using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Analysis;
using Sushi.Core.Diagnostics;

namespace Sushi.LanguageServer.Workspace;

/// <summary>
/// Represents a versioned input for workspace analysis.
/// </summary>
internal sealed record WorkspaceAnalysisInput
{
    private readonly List<(AnalysisResult analysis, IDiagnosticReporter diagnosticReporter)> analysisCandidates = [];

    /// <summary>
    /// The generation number of the workspace analysis.
    /// </summary>
    public long Generation { get; }

    /// <summary>
    /// The document snapshots participating in workspace analysis.
    /// </summary>
    public IReadOnlyList<DocumentSnapshot> Documents { get; }

    /// <summary>
    /// Gets the candidates for analysis commitment.
    /// </summary>
    public IReadOnlyList<(AnalysisResult analysis, IDiagnosticReporter diagnosticReporter)> AnalysisCandidates
    => new ReadOnlyCollection<(AnalysisResult result, IDiagnosticReporter diagnosticReporter)>([..this.analysisCandidates]);

    /// <summary>
    /// Adds an anlysis candidate to this workspace analysis input.
    /// </summary>
    /// <param name="analysis">
    /// The analysis result.
    /// </param>
    /// <param name="diagnosticReporter">
    /// The reporter used to accumulate diagnostics.
    /// </param>
    public void AddAnalysisCandidate([NotNull] AnalysisResult analysis, [NotNull] IDiagnosticReporter diagnosticReporter)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(diagnosticReporter);
        this.analysisCandidates.Add((analysis, diagnosticReporter));
    }

    /// <summary>
    /// Creates a new instance of <see cref="WorkspaceAnalysisInput" />.
    /// </summary>
    /// <param name="generation">
    /// The generation number of the workspace analysis.
    /// </param>
    /// <param name="documents">
    /// The document snapshots participating in workspace analysis.
    /// </param>
    internal WorkspaceAnalysisInput(long generation, [NotNull] IReadOnlyList<DocumentSnapshot> documents)
    {
        this.Generation = generation;

        this.Documents = documents;
    }
}
