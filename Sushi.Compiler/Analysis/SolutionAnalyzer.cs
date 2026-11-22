using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Configuration;
using Sushi.Compiler.Diagnostics;
using Sushi.Core.Analysis;
using Sushi.Core.Diagnostics;
using Sushi.Core.Source;

namespace Sushi.Compiler.Analysis;

/// <summary>
/// Performs analysis of Sushi Solution snapshots.
/// </summary>
public sealed class SolutionAnalyzer : Analyzer
{
    /// <inheritdoc />
    public override Task<AnalysisResult> Analyze([NotNull] SourceSnapshot snapshot, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(diagnosticReporter);

        cancellationToken.ThrowIfCancellationRequested();

        IDiagnosticReporter localDiagnostics = new DiagnosticReporter();

        if (!snapshot.IsValidUTF8)
        {
            AccumulateEncodingDiagnostics(localDiagnostics, snapshot);
            diagnosticReporter.CommitReporter(localDiagnostics);
            return Task.FromResult<AnalysisResult>(new SolutionAnalysisResult(snapshot, null));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // TODO: This result has a use later
        TomlConfigurationParseResult _ = TomlConfigurationParser.Parse(snapshot, localDiagnostics, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        diagnosticReporter.CommitReporter(localDiagnostics);

        return Task.FromResult<AnalysisResult>(new SolutionAnalysisResult(snapshot, new SolutionDefinition()));
    }
}
