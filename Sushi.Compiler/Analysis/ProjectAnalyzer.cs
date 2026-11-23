using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Configuration;
using Sushi.Compiler.Diagnostics;
using Sushi.Core.Analysis;
using Sushi.Core.Diagnostics;
using Sushi.Core.Source;

namespace Sushi.Compiler.Analysis;

/// <summary>
/// Performs analysis of Sushi Project snapshots.
/// </summary>
public sealed class ProjectAnalyzer : Analyzer
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
            return Task.FromResult<AnalysisResult>(new ProjectAnalysisResult(snapshot, null));
        }

        cancellationToken.ThrowIfCancellationRequested();

        TomlConfigurationParseResult parseResult = TomlConfigurationParser.Parse(snapshot, localDiagnostics, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        bool hasSyntaxErrors = localDiagnostics.HasErrors();

        ProjectConfigurationBindResult bindResult = ProjectConfigurationBinder.Bind(snapshot, localDiagnostics, parseResult.Document, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        ProjectDefinition? project = hasSyntaxErrors ? null : bindResult.Project;

        diagnosticReporter.CommitReporter(localDiagnostics);

        return Task.FromResult<AnalysisResult>(new ProjectAnalysisResult(snapshot, project));
    }
}
