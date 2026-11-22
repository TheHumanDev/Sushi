using System.Diagnostics.CodeAnalysis;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Sushi.Core.Analysis;
using Sushi.Core.Diagnostics;
using Sushi.Core.Source;

namespace Sushi.LanguageServer.Workspace;

/// <summary>
/// Handles the orchestration and plumbing of source document tracking.
/// </summary>
public sealed class WorkspaceOrchestrator : IDisposable
{
    /// <summary>
    /// Represents the currently tracked workspace.
    /// </summary>
    private readonly SushiWorkspace workspace = new();

    /// <summary>
    /// Synchronizes access of workspace folders.
    /// </summary>
    private readonly Lock foldersSyncRoot = new();

    /// <summary>
    /// Serializes authoritative workspace mutations and analysis commits.
    /// </summary>
    private readonly SemaphoreSlim mutationGate = new(1, 1);

    /// <summary>
    /// The coordinator that handles analysis.
    /// </summary>
    private readonly WorkspaceAnalysisCoordinator coordinator;

    /// <summary>
    /// The workspace folders from the client. Contains the sushi project files when running in LSP mode.
    /// </summary>
    private readonly List<WorkspaceFolder> folders = [];

    /// <summary>
    /// The current workspace generation.
    /// </summary>
    private long workspaceGeneration;

    /// <summary>
    /// The most recently committed analysis generation.
    /// </summary>
    private long analyzedWorkspaceGeneration = -1;

    /// <summary>
    /// The language server facade used to publish diagnostics.
    /// </summary>
    private ILanguageServerFacade? languageServer;

    /// <summary>
    /// The provider used to discover registered document types.
    /// </summary>
    private readonly ISourceDocumentProvider documentProvider;

    /// <summary>
    /// Whether the orchestrator has been disposed.
    /// </summary>
    private bool disposed;

    /// <summary>
    /// Creates a new instance of <see cref="WorkspaceOrchestrator" />.
    /// </summary>
    /// <param name="documentProvider">
    /// The provider used to discover registered document types.
    /// </param>
    /// <param name="diagnosticReporterFactory">
    /// The factory used to create new <see cref="IDiagnosticReporter" /> objects.
    /// </param>
    /// <param name="projectMembership">
    /// Associates source analyses with the projects that explicitly include them.
    /// </param>
    public WorkspaceOrchestrator([NotNull] ISourceDocumentProvider documentProvider, [NotNull] IDiagnosticReporterFactory diagnosticReporterFactory, [NotNull] WorkspaceProjectMembership projectMembership)
    {
        ArgumentNullException.ThrowIfNull(documentProvider);
        ArgumentNullException.ThrowIfNull(diagnosticReporterFactory);
        ArgumentNullException.ThrowIfNull(projectMembership);

        this.coordinator = new WorkspaceAnalysisCoordinator(this, documentProvider, diagnosticReporterFactory, projectMembership);
        this.documentProvider = documentProvider;
    }

    /// <summary>
    /// Initializes the language service with the information about the currently open workspace.
    /// </summary>
    /// <param name="workspaceFolders">
    /// The folders that are a part of the workspace.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token used to cancel initialization.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task"/>.
    /// </returns>
    public async Task Initialize([NotNull] IEnumerable<WorkspaceFolder> workspaceFolders, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspaceFolders);

        await this.mutationGate.WaitAsync(cancellationToken);

        try
        {
            this.ReplaceWorkspaceFolders(workspaceFolders);

            await this.UpdateSourceDocuments(cancellationToken);
            this.AdvanceWorkspaceGeneration();
        }
        finally
        {
            this.mutationGate.Release();
        }

        await this.coordinator.AnalyzeWorkspace(cancellationToken);
    }

    /// <summary>
    /// Connects the initialized workspace to the LSP server and publishes
    /// any existing current diagnostics.
    /// </summary>
    /// <param name="languageServer">
    /// The language server facade used to interface with the protocol.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel startup.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task"/>.
    /// </returns>
    public Task Start([NotNull] ILanguageServerFacade languageServer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(languageServer);

        this.languageServer = languageServer;
        this.PublishDiagnosticsForAllDocuments(cancellationToken);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates the workspace folders with the current changesset from the changed workspace folders.
    /// </summary>
    /// <param name="addedFolders">
    /// A list of folders that were added.
    /// </param>
    /// <param name="removedFolders">
    /// A list of folders that were removed.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task" />.
    /// </returns>
    public async Task UpdateWorkspaceFolders([NotNull] IEnumerable<WorkspaceFolder> addedFolders, [NotNull] IEnumerable<WorkspaceFolder> removedFolders, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(addedFolders);
        ArgumentNullException.ThrowIfNull(removedFolders);

        await this.mutationGate.WaitAsync(cancellationToken);

        try
        {
            using (this.foldersSyncRoot.EnterScope())
            {
                foreach (WorkspaceFolder removedFolder in removedFolders)
                {
                    Uri removedUri = removedFolder.Uri.ToUri();
                    this.folders.RemoveAll(folder => folder.Uri.ToUri().IsSamePath(removedUri));
                }

                foreach (WorkspaceFolder addedFolder in addedFolders)
                {
                    Uri addedUri = addedFolder.Uri.ToUri();
                    bool alreadyExists = this.folders.Any(folder => folder.Uri.ToUri().IsSamePath(addedUri));

                    if (!alreadyExists)
                    {
                        this.folders.Add(addedFolder);
                    }
                }
            }

            await this.UpdateSourceDocuments(cancellationToken);
            this.AdvanceWorkspaceGeneration();
        }
        finally
        {
            this.mutationGate.Release();
        }

        await this.coordinator.AnalyzeWorkspace(cancellationToken);
    }

    /// <summary>
    /// Updates the source documents by refreshing the workspace folders
    /// and replacing the contents of each source document with its current
    /// up-to-date version.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token used to cancel the update.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task" />.
    /// </returns>
    private async Task UpdateSourceDocuments(CancellationToken cancellationToken)
    {
        WorkspaceFolder[] folders;

        using (this.foldersSyncRoot.EnterScope())
        {
            folders = [.. this.folders];
        }

        List<Uri> discoveredDocuments = [];

        EnumerationOptions enumerationOptions = new()
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        foreach (WorkspaceFolder folder in folders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Uri folderUri = folder.Uri.ToUri();

            if (!folderUri.IsFile)
            {
                continue;
            }

            string folderPath = folderUri.LocalPath;

            if (!Directory.Exists(folderPath))
            {
                continue;
            }

            foreach (string filePath in Directory.EnumerateFiles(folderPath, "*", enumerationOptions))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!this.IsWorkspaceDocument(filePath))
                {
                    continue;
                }

                Uri documentUri = new(Path.GetFullPath(filePath));
                discoveredDocuments.Add(documentUri);

                SourceSnapshot diskSnapshot = await LoadDiskSnapshot(documentUri, cancellationToken);
                SourceDocument document = this.workspace.GetOrAddDocument(documentUri, diskSnapshot);

                document.UpdateDisk(diskSnapshot);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        foreach (SourceDocument document in this.workspace.Documents)
        {
            bool stillExists = discoveredDocuments.Any(uri => uri.IsSamePath(document.Uri));

            if (!stillExists && !document.IsOpen)
            {
                this.coordinator.CancelScheduledAnalysis();
                this.workspace.RemoveDocument(document.Uri);
                this.ClearDiagnostics(document.Uri);
            }
        }
    }

    /// <summary>
    /// Captures workspace membership, source snapshots, and the current
    /// generation under the authoritative workspace mutation gate.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token used to cancel snapshot capture.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task"/> that returns a consistent, versioned input for workspace analysis.
    /// </returns>
    internal async Task<WorkspaceAnalysisInput> CaptureAnalysisInput(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await this.mutationGate.WaitAsync(cancellationToken);

        try
        {
            return new WorkspaceAnalysisInput(this.workspaceGeneration,
                [.. this.workspace.Documents.Select(document => new DocumentSnapshot(document, document.CurrentSnapshot))]);
        }
        finally
        {
            this.mutationGate.Release();
        }
    }

    /// <summary>
    /// Conditionally commits an analysis generation if the workspace and
    /// every captured source snapshot are still current.
    /// </summary>
    /// <param name="input">
    /// The input used by the analysis attempt.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancels commitment.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task" /> that returns true if the
    /// generation was accepted; otherwise false.
    /// </returns>
    internal async Task<bool> TryCommitAnalysis(WorkspaceAnalysisInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        cancellationToken.ThrowIfCancellationRequested();
        await this.mutationGate.WaitAsync(cancellationToken);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (this.workspaceGeneration != input.Generation
                || input.Documents.Any(item => !item.Document.IsCurrent(item.Snapshot)))
            {
                return false;
            }

            foreach ((AnalysisResult analysis, IDiagnosticReporter diagnosticReporter) in input.AnalysisCandidates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                DocumentSnapshot? item = input.Documents.FirstOrDefault(candidate => ReferenceEquals(candidate.Snapshot, analysis.Snapshot));

                if (item is null || !item.Document.TryUpdateAnalysis(analysis, diagnosticReporter))
                {
                    continue;  
                }

                this.PublishDiagnostics(analysis, diagnosticReporter.ReportDiagnostics());
            }

            this.analyzedWorkspaceGeneration = input.Generation;

            return true;
        }
        finally
        {
            this.mutationGate.Release();
        }
    }

    /// <summary>
    /// Advances the workspace generation after an authoritative state change.
    /// </summary>
    private void AdvanceWorkspaceGeneration() => this.workspaceGeneration++;

    /// <summary>
    /// Loads the disk snapshot for the specified document Uri.
    /// </summary>
    /// <param name="documentUri">
    /// The Uri of the document to load the disk snapshot for.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel loading.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task"/> that returns the resulting <see cref="SourceSnapshot"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the Uri is not a file Uri. Only file Uris can be loaded from disk.
    /// </exception>
    private static async Task<SourceSnapshot> LoadDiskSnapshot(Uri documentUri, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(documentUri);

        if (!documentUri.IsFile)
        {
            throw new ArgumentException("Only file URIs can be loaded from disk.", nameof(documentUri));
        }

        cancellationToken.ThrowIfCancellationRequested();

        byte[] bytes = await File.ReadAllBytesAsync(documentUri.LocalPath, cancellationToken);

        return SourceSnapshot.FromUTF8(documentUri, version: null, bytes);
    }

    /// <summary>
    /// Replaces the current workspace folders with an entirely new set.
    /// </summary>
    /// <param name="workspaceFolders">
    /// The workspace folders to replace the old ones with.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task"/>.
    /// </returns>
    private void ReplaceWorkspaceFolders([NotNull] IEnumerable<WorkspaceFolder> workspaceFolders)
    {
        ArgumentNullException.ThrowIfNull(workspaceFolders);

        using (this.foldersSyncRoot.EnterScope())
        {
            this.folders.Clear();
            this.folders.AddRange(workspaceFolders);
        }
    }
    
    /// <summary>
    /// Applies watched-file notifications and schedules analysis when
    /// an authoritative workspace change is detected.
    /// </summary>
    /// <param name="changes">
    /// The changes to apply.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel file change updates.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the file change type is unsupported.
    /// </exception>
    public async Task ApplyFileChanges([NotNull] IEnumerable<FileEvent> changes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(changes);

        FileEvent[] fileEvents = [.. changes];
        bool changed = false;
        List<Uri> affectedUris = [];

        await this.mutationGate.WaitAsync(cancellationToken);

        try
        {
            foreach (FileEvent fileEvent in fileEvents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Uri documentUri = fileEvent.Uri.ToUri();

                if (!documentUri.IsFile || !this.IsWorkspaceDocument(documentUri.LocalPath))
                {
                    continue;
                }

                switch (fileEvent.Type)
                {
                    case FileChangeType.Created:
                    case FileChangeType.Changed:
                        if (await this.RefreshDiskDocument(documentUri, cancellationToken))
                        {
                            changed = true;
                            affectedUris.Add(documentUri);
                        }

                        break;

                    case FileChangeType.Deleted:
                        if (this.HandleDeletedDiskDocument(documentUri))
                        {
                            changed = true;
                            affectedUris.Add(documentUri);
                        }

                        break;

                    default:
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                        throw new ArgumentOutOfRangeException(nameof(fileEvent), fileEvent.Type, "Unsupported file change type.");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
                }
            }

            if (changed)
            {
                this.AdvanceWorkspaceGeneration();
            }
        }
        finally
        {
            this.mutationGate.Release();
        }

        if (changed)
        {
            this.coordinator.ScheduleFileSystemAnalysis(affectedUris);
        }
    }

    /// <summary>
    /// Refreshes an existing disk document or adds a newly discovered document.
    /// </summary>
    /// <param name="documentUri">
    /// The <see cref="Uri"/> of the document to refresh.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel document refresh.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task"/> that returns a bool. True if the change affects the current document, or it was closed. False if the document was still open.
    /// </returns>
    private async Task<bool> RefreshDiskDocument(Uri documentUri, CancellationToken cancellationToken)
    {
        if (!File.Exists(documentUri.LocalPath))
        {
            return this.HandleDeletedDiskDocument(documentUri);
        }

        SourceSnapshot diskSnapshot;

        try
        {
            diskSnapshot = await LoadDiskSnapshot(documentUri, cancellationToken);
        }
        catch (Exception _) when (_ is FileNotFoundException or DirectoryNotFoundException)
        {
            return this.HandleDeletedDiskDocument(documentUri);
        }

        SourceDocument document = this.workspace.GetOrAddDocument(documentUri, diskSnapshot);
        document.UpdateDisk(diskSnapshot);

        if (document.IsOpen)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Removes a deleted disk-backed document while preserving an open
    /// editor snapshot, if one exists.
    /// </summary>
    /// <param name="documentUri">
    /// The deleted document <see cref="Uri"/>.
    /// </param>
    /// <returns>
    /// True if the document was closed. False if it was still open.
    /// </returns>
    private bool HandleDeletedDiskDocument(Uri documentUri)
    {
        if (!this.workspace.TryGetDocument(documentUri, out SourceDocument? document))
        {
            this.ClearDiagnostics(documentUri);

            return false;
        }

        if (document.IsOpen)
        {
            // The in-memory editor snapshot remains authoritative.
            // The stale disk snapshot will never become current while
            // the editor snapshot exists.
            return false;
        }

        this.coordinator.CancelScheduledAnalysis();
        this.workspace.RemoveDocument(documentUri);
        this.ClearDiagnostics(documentUri);
        return true;
    }

    /// <summary>
    /// Clears the diagnostics for the given document <see cref="Uri" />.
    /// </summary>
    /// <param name="documentUri">
    /// The document <see cref="Uri"/> to clear diagnostics for.
    /// </param>
    private void ClearDiagnostics([NotNull] Uri documentUri)
    {
        ArgumentNullException.ThrowIfNull(documentUri);

        if (this.languageServer is null)
        {
            return;
        }

        this.languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = DocumentUri.FromFileSystemPath(documentUri.LocalPath),
            Version = null,
            Diagnostics = new Container<Diagnostic>([])
        });
    }

    /// <summary>
    /// Publishes diagnostics for all tracked documents in the workspace.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token used to cancel publishing.
    /// </param>
    private void PublishDiagnosticsForAllDocuments(CancellationToken cancellationToken)
    {
        foreach (SourceDocument document in this.workspace.Documents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!document.TryGetCurrentAnalysis(out AnalysisResult? analysis, out IReadOnlyList<SushiDiagnostic>? diagnostics))
            {
                continue;
            }

            this.PublishDiagnostics(analysis, diagnostics);
        }
    }

    /// <summary>
    /// Publishes diagnostics for the specified analysis result.
    /// </summary>
    /// <param name="result">
    /// The analysis result to publish the diagnostics for.
    /// </param>
    /// <param name="sushiDiagnostics">
    /// The diagnostics to publish.
    /// </param>
    private void PublishDiagnostics([NotNull] AnalysisResult result, [NotNull] IReadOnlyList<SushiDiagnostic> sushiDiagnostics)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(sushiDiagnostics);

        if (this.languageServer is null)
        {
            return;
        }

        SourceSnapshot snapshot = result.Snapshot;

        Diagnostic[] diagnostics = [.. sushiDiagnostics.Select(diagnostic => LSPAdapter.ToLSPDiagnostic(diagnostic, snapshot))];

        this.languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = DocumentUri.FromFileSystemPath(snapshot.Uri.LocalPath),
            Version = snapshot.Version,
            Diagnostics = new Container<Diagnostic>(diagnostics)
        });
    }

    /// <summary>
    /// Returns whether the specified path is considered a sushi workspace document.
    /// </summary>
    /// <param name="filePath">
    /// The path to check.
    /// </param>
    /// <returns>
    /// True if the file is a sushi workspace document. False otherwise.
    /// </returns>
    private bool IsWorkspaceDocument([NotNull] string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        string extension = Path.GetExtension(filePath).Replace(".", string.Empty, StringComparison.OrdinalIgnoreCase);

        return this.documentProvider.RegisteredExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.coordinator.Dispose();
        this.mutationGate.Dispose();

        this.disposed = true;
    }
}
