using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Diagnostics;
using Sushi.Compiler.Lexing;
using Sushi.Core.Analysis;
using Sushi.Core.Diagnostics;
using Sushi.Core.Source;

namespace Sushi.Compiler.Analysis;

/// <summary>
/// Performs analysis of Sushi source snapshots.
/// </summary>
public sealed class SourceAnalyzer : Analyzer
{
    /// <summary>
    /// The lexer used to lex the source snapshot.
    /// </summary>
    private readonly SourceLexer lexer = new();

    /// <inheritdoc />
    public override async Task<AnalysisResult> Analyze([NotNull] SourceSnapshot snapshot, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(diagnosticReporter);
        cancellationToken.ThrowIfCancellationRequested();

        IDiagnosticReporter localDiagnostics = new DiagnosticReporter();

        AccumulateEncodingDiagnostics(localDiagnostics, snapshot);
        cancellationToken.ThrowIfCancellationRequested();

        LexResult lexerResult = await this.lexer.Lex(snapshot, localDiagnostics, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        diagnosticReporter.CommitReporter(localDiagnostics);

        SourceAnalysisResult result = new(snapshot);

        return result;
    }
}
