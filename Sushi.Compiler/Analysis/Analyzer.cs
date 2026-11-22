using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Analysis;
using Sushi.Core.Diagnostics;
using Sushi.Core.Source;

namespace Sushi.Compiler.Analysis;

/// <summary>
/// Coordinates compiler analysis for a specific type of source snapshot.
/// </summary>
public abstract class Analyzer
{
    /// <summary>
    /// Accumulates encoding diagnostics into the specified diagnostic reporter.
    /// </summary>
    /// <param name="diagnosticReporter">
    /// The diagnostic reporter to accumulate the diagnostics into.
    /// </param>
    /// <param name="snapshot">
    /// The source snapshot containing the diagnostic encoding issues.
    /// </param>
    protected static void AccumulateEncodingDiagnostics([NotNull] IDiagnosticReporter diagnosticReporter, [NotNull] SourceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(diagnosticReporter);
        ArgumentNullException.ThrowIfNull(snapshot);

        foreach (SourceEncodingIssue issue in snapshot.EncodingIssues)
        {
            diagnosticReporter.GenerateError(ErrorType.InvalidUtf8, new SourceSpan(snapshot, issue.Start, issue.End));
        }
    }

    /// <summary>
    /// Analyzes the specified source snapshot.
    /// </summary>
    /// <param name="snapshot">
    /// The snapshot to analyze.
    /// </param>
    /// <param name="diagnosticReporter">
    /// The diagnostic reporter used to accumulate diagnostics.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel analysis.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task" /> that returns the result of the analysis.
    /// </returns>
    public abstract Task<AnalysisResult> Analyze([NotNull] SourceSnapshot snapshot, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken);
}
