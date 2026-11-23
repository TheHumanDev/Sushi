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
    /// The factory used to create diagnostic reporters.
    /// </summary>
    private readonly IDiagnosticReporterFactory diagnosticReporterFactory;

    /// <summary>
    /// Associates source analyses with the projects that explicitly include them.
    /// </summary>
    private readonly WorkspaceProjectMembership projectMembership;

    /// <summary>
    /// Synchronizes access to scheduled analysis.
    /// </summary>
    private readonly Lock scheduleSyncRoot = new();

    /// <summary>
    /// The cancellation source owned by the pending analysis attempt.
    /// </summary>
#pragma warning disable CA2213 // Disposable fields should be disposed
    private CancellationTokenSource? scheduledWorkspaceAnalysis;
#pragma warning restore CA2213 // Disposable fields should be disposed

    /// <summary>
    /// The affected document URIs accumulated for scheduled analysis.
    /// </summary>
    private HashSet<Uri>? scheduledAnalysisUris;

    /// <summary>
    /// Whether the coordinator has been disposed.
    /// </summary>
    private bool disposed;

    /// <summary>
    /// Creates a new instance of <see cref="WorkspaceAnalysisCoordinator" />.
    /// </summary>
    /// <param name="orchestrator">
    /// Owns workspace mutations, snapshot capture, and result commitment.
    /// </param>
    /// <param name="documentProvider">
    /// Resolves the analyzer registered for each document extension.
    /// </param>
    /// <param name="diagnosticReporterFactory">
    /// Creates independent diagnostic reporters for document analyses.
    /// </param>
    /// <param name="projectMembership">
    /// Associates source analyses with the projects that explicitly include them.
    /// </param>
    internal WorkspaceAnalysisCoordinator([NotNull] WorkspaceOrchestrator orchestrator, [NotNull] ISourceDocumentProvider documentProvider, [NotNull] IDiagnosticReporterFactory diagnosticReporterFactory, WorkspaceProjectMembership projectMembership)
    {
        ArgumentNullException.ThrowIfNull(orchestrator);
        ArgumentNullException.ThrowIfNull(documentProvider);
        ArgumentNullException.ThrowIfNull(diagnosticReporterFactory);
        ArgumentNullException.ThrowIfNull(projectMembership);

        this.orchestrator = orchestrator;
        this.documentProvider = documentProvider;
        this.diagnosticReporterFactory = diagnosticReporterFactory;
        this.projectMembership = projectMembership;
    }

    /// <summary>
    /// Analyzes the registered documents in a captured workspace generation,
    /// associates sources with their projects, and attempts an atomic
    /// generation-checked commitment.
    /// </summary>
    /// <param name="cancellationToken">
    /// Cancels analysis and prevents publication of incomplete results.
    /// </param>
    /// <param name="affectedUris">
    /// The changed documents, or null for an unconditional workspace analysis.
    /// Incremental selection is deferred; nonempty changes currently require
    /// a complete pass to preserve project membership consistency.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task" /> representing the analysis attempt.
    /// </returns>
    internal async Task AnalyzeWorkspace(CancellationToken cancellationToken, IReadOnlyList<Uri>? affectedUris = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (affectedUris is { Count: 0 })
        {
            return;
        }

        WorkspaceAnalysisInput input = await this.orchestrator.CaptureAnalysisInput(cancellationToken);

        List<(AnalysisResult Analysis, IDiagnosticReporter Reporter)> analyses = [];

        foreach (DocumentSnapshot item in input.Documents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string extension = Path.GetExtension(item.Snapshot.Uri.LocalPath).TrimStart('.');

            ISourceDocumentType? documentType = this.documentProvider.DocumentTypes
                .FirstOrDefault(type => type.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase));

            if (documentType is null)
            {
                continue;
            }

            IDiagnosticReporter reporter = this.diagnosticReporterFactory.GetNewDiagnosticReporter();

            AnalysisResult analysis = await documentType.Analyze(item.Snapshot, reporter, cancellationToken);

            analyses.Add((analysis, reporter));
        }

        IReadOnlyList<AnalysisResult> associated = this.projectMembership.AssociateSources(
            [.. analyses.Select(item => item.Analysis)], cancellationToken);

        for (int index = 0; index < analyses.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            input.AddAnalysisCandidate(associated[index], analyses[index].Reporter);
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
    /// Executes scheduled analysis and releases resources belonging to
    /// that attempt. Superseded attempts cannot clear newer scheduling state.
    /// </summary>
    /// <param name="delay">
    /// The debounce period.
    /// </param>
    /// <param name="cancellation">
    /// The cancellation source owned by this attempt.
    /// </param>
    /// <param name="affectedUris">
    /// The captured affected-document scope.
    /// </param>
    /// <returns>
    /// An awaitable task representing the scheduled attempt.
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
            // A newer workspace generation superseded the scheduled attempt.
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

    /// <summary>
    /// Prevents further scheduling and cancels the currently scheduled
    /// attempt. Running attempts retain ownership of their cancellation
    /// resources until they exit.
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
