
using System.Diagnostics.CodeAnalysis;
using Serilog;
using Sushi.Core.Analysis;
using Sushi.Core.Diagnostics;
using Sushi.Core.Source;

namespace Sushi.LanguageServer.Workspace;

/// <summary>
/// Coordinates workspace analysis, including scheduling, cancellation,
/// and execution against captured workspace snapshots.
/// </summary>
internal sealed class WorkspaceAnalysisCoordinator : IDisposable
{
    /// <summary>
    /// The debounce time for file system analysis.
    /// </summary>
    private static readonly TimeSpan fileSystemAnalysisDebounce = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// The workspace orchestrator that owns this coordinator.
    /// </summary>
    private readonly WorkspaceOrchestrator orchestrator;

    /// <summary>
    /// The provider used to discover registered document types.
    /// </summary>
    private readonly ISourceDocumentProvider documentProvider;

    /// <summary>
    /// The factory used to create new <see cref="IDiagnosticReporter" /> objects.
    /// </summary>
    private readonly IDiagnosticReporterFactory diagnosticReporterFactory;

    /// <summary>
    /// Synchronizes access of scheduled analysis.
    /// </summary>
    private readonly Lock scheduleSyncRoot = new();

    /// <summary>
    /// The cancellation token used for cooperative cancellation of scheduled analysis.
    /// </summary>
#pragma warning disable CA2213 // Disposable fields should be disposed
    private CancellationTokenSource? scheduledWorkspaceAnalysis;
#pragma warning restore CA2213 // Disposable fields should be disposed

    /// <summary>
    /// The Uris currently participating in scheduled analysis.
    /// </summary>
    private HashSet<Uri>? scheduledAnalysisUris;

    /// <summary>
    /// Whether the coordinator has been disposed.
    /// </summary>
    private bool disposed;

    /// <summary>
    /// Creates a new instance of <see cref="WorkspaceAnalysisCoordinator"/>.
    /// </summary>
    /// <param name="orchestrator">
    /// The owner of workspace mutations, snapshot capture, and result commitment.
    /// </param>
    /// <param name="documentProvider">
    /// The provider used to discover registered document types.
    /// </param>
    /// <param name="diagnosticReporterFactory">
    /// The factory used to create new <see cref="IDiagnosticReporter" /> objects.
    /// </param>
    internal WorkspaceAnalysisCoordinator([NotNull] WorkspaceOrchestrator orchestrator, [NotNull] ISourceDocumentProvider documentProvider, [NotNull] IDiagnosticReporterFactory diagnosticReporterFactory)
    {
        ArgumentNullException.ThrowIfNull(orchestrator);
        ArgumentNullException.ThrowIfNull(documentProvider);
        ArgumentNullException.ThrowIfNull(diagnosticReporterFactory);
        this.orchestrator = orchestrator;
        this.documentProvider = documentProvider;
        this.diagnosticReporterFactory = diagnosticReporterFactory;
    }

    /// <summary>
    /// Analyzes a captured workspace generation.
    /// </summary>
    /// <param name="cancellationToken">
    /// Used to cancel analysis before a result can be committed.
    /// </param>
    /// <param name="affectedUris">
    /// The documents affected by the change, or null for full workspace analysis.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task" /> representing the analysis attempt.
    /// </returns>
    internal async Task AnalyzeWorkspace(CancellationToken cancellationToken, IReadOnlyList<Uri>? affectedUris = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        WorkspaceAnalysisInput input = await this.orchestrator.CaptureAnalysisInput(cancellationToken);

        IReadOnlyList<SourceSnapshot> snapshots = [.. input.Documents.Select(item => item.Snapshot)];

        bool analyzeEntireWorkspace = affectedUris is null
            || affectedUris
                .Any(uri => this.DoesDocumentInvalidateEntireCache(uri) || !File.Exists(uri.LocalPath));

        IReadOnlyList<Uri>? effectiveAffectedUris = analyzeEntireWorkspace ? null : affectedUris;

        foreach (DocumentSnapshot item in input.Documents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string extension = Path.GetExtension(item.Snapshot.Uri.LocalPath).Replace(".", string.Empty, StringComparison.InvariantCultureIgnoreCase);

            ISourceDocumentType? documentType = this.documentProvider.DocumentTypes.FirstOrDefault(doc => doc.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase));

            if (documentType is null)
            {
                continue;
            }

            IDiagnosticReporter reporter = this.diagnosticReporterFactory.GetNewDiagnosticReporter();
            AnalysisResult analysis = await documentType.Analyze(item.Snapshot, reporter, cancellationToken);

        }

        cancellationToken.ThrowIfCancellationRequested();
        await this.orchestrator.TryCommitAnalysis(input, cancellationToken);
    }

    /// <summary>
    /// Schedules a debounced analysis after watched filesystem changes.
    /// </summary>
    /// <param name="affectedUris">
    /// The documents affected by the filesystem notification.
    /// </param>
    internal void ScheduleFileSystemAnalysis(IEnumerable<Uri> affectedUris)
        => this.ScheduleWorkspaceAnalysis(fileSystemAnalysisDebounce, affectedUris);

    /// <summary>
    /// Replaces pending analysis with a new scheduled attempt while retaining
    /// the accumulated affected-document scope.
    /// </summary>
    /// <param name="delay">
    /// The debounce period before analysis begins.
    /// </param>
    /// <param name="affectedUris">
    /// The documents to include in the pending analysis scope.
    /// </param>
    private void ScheduleWorkspaceAnalysis(TimeSpan delay, IEnumerable<Uri> affectedUris)
    {
        ArgumentNullException.ThrowIfNull(affectedUris);

        CancellationTokenSource cancellation = new();
        Uri[] scope;

        using (this.scheduleSyncRoot.EnterScope())
        {
            if (this.disposed)
            {
                cancellation.Dispose();
                return;
            }

            this.scheduledWorkspaceAnalysis?.Cancel();

            this.scheduledAnalysisUris ??= [];

            foreach (Uri uri in affectedUris)
            {
                this.scheduledAnalysisUris.Add(uri);
            }

            scope = [.. this.scheduledAnalysisUris];
            this.scheduledWorkspaceAnalysis = cancellation;
        }

#pragma warning disable CA2025 // Do not pass 'IDisposable' instances into unawaited tasks
        _ = this.RunScheduledWorkspaceAnalysis(delay, cancellation, scope);
#pragma warning restore CA2025 // Do not pass 'IDisposable' instances into unawaited tasks
    }

    /// <summary>
    /// Executes a scheduled analysis and releases its cancellation resources.
    /// Superseded attempts cannot clear the state of newer attempts.
    /// </summary>
    /// <param name="delay">
    /// The debounce period.
    /// </param>
    /// <param name="cancellation">
    /// The cancellation source owned by this particular attempt.
    /// </param>
    /// <param name="affectedUris">
    /// The captured affected-document scope.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task" /> representing the scheduled attempt.
    /// </returns>
    private async Task RunScheduledWorkspaceAnalysis(TimeSpan delay, CancellationTokenSource cancellation, IReadOnlyList<Uri> affectedUris)
    {
        try
        {
            await Task.Delay(delay, cancellation.Token);
            await this.AnalyzeWorkspace(cancellation.Token, affectedUris);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Expected when a newer workspace snapshot supersedes this analysis.
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception exception)
        {
            Log.Error(exception, "Unhandled exception while performing scheduled workspace analysis.");
        }
#pragma warning restore CA1031 // Do not catch general exception types
        finally
        {
            using (this.scheduleSyncRoot.EnterScope())
            {
                if (ReferenceEquals(this.scheduledWorkspaceAnalysis, cancellation))
                {
                    this.scheduledWorkspaceAnalysis = null;
                    this.scheduledAnalysisUris = null;
                }
            }

            cancellation.Dispose();
        }
    }

    /// <summary>
    /// Cancels pending analysis and discards the pending affected-document scope.
    /// </summary>
    internal void CancelScheduledAnalysis()
    {
        using (this.scheduleSyncRoot.EnterScope())
        {
            if (this.scheduledWorkspaceAnalysis is CancellationTokenSource cancellation)
            {
                this.scheduledWorkspaceAnalysis = null;
                this.scheduledAnalysisUris = null;
                cancellation.Cancel();
            }
        }
    }

    private bool IsWorkspaceDocument(string filePath)
    {
        string extension = Path.GetExtension(filePath).Replace(".", string.Empty, StringComparison.OrdinalIgnoreCase);

        return this.documentProvider.RegisteredExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private bool DoesDocumentInvalidateEntireCache(Uri uri)
    {
        string extension = Path.GetExtension(uri.LocalPath).Replace(".", string.Empty, StringComparison.OrdinalIgnoreCase);

        ISourceDocumentType? documentType = this.documentProvider.DocumentTypes.FirstOrDefault(doc => doc.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase));

        if (documentType is null)
        {
            return false;
        }

        return documentType.ChangesInvalidateEntireCache;
    }

    /// <summary>
    /// Prevents further scheduling and cancels the currently scheduled attempt.
    /// Running attempts retain ownership of their cancellation resources until exit.
    /// </summary>
    public void Dispose()
    {
        using (this.scheduleSyncRoot.EnterScope())
        {
            if (this.disposed)
            {
                return;
            }
            
            this.disposed = true;

            this.scheduledWorkspaceAnalysis?.Cancel();
            this.scheduledWorkspaceAnalysis = null;
            this.scheduledAnalysisUris = null;
        }
    }
}
