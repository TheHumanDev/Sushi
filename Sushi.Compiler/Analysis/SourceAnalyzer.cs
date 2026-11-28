using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Diagnostics;
using Sushi.Compiler.Parsing;
using Sushi.Compiler.Parsing.Syntax;
using Sushi.Compiler.Semantics;
using Sushi.Core;
using Sushi.Core.Analysis;
using Sushi.Core.Diagnostics;
using Sushi.Core.Lexing;
using Sushi.Core.Parsing;
using Sushi.Core.Source;

namespace Sushi.Compiler.Analysis;

/// <summary>
/// Performs analysis of Sushi source snapshots.
/// </summary>
public sealed class SourceAnalyzer : Analyzer
{
    /// <summary>
    /// The context used to link parts of the analysis process together.
    /// </summary>
    private readonly ISushiContext<ParsingContext> context;

    /// <summary>
    /// Creates a new instance of <see cref="Analyzer" />.
    /// </summary>
    /// <param name="context">
    /// The context used to link parts of the analysis process together.
    /// </param>
    public SourceAnalyzer([NotNull] ISushiContext<ParsingContext> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        this.context = context;
    }

    /// <inheritdoc />
    public override async Task<AnalysisResult> Analyze([NotNull] SourceSnapshot snapshot, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(diagnosticReporter);
        cancellationToken.ThrowIfCancellationRequested();

        IDiagnosticReporter localDiagnostics = new DiagnosticReporter();

        AccumulateEncodingDiagnostics(localDiagnostics, snapshot);
        cancellationToken.ThrowIfCancellationRequested();

        LexResult lexerResult = await this.context.Lexer.Lex(snapshot, localDiagnostics, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        ParserCursor cursor = new(lexerResult, this.context.KeywordProvider, cancellationToken);

        ParsingContext context = new(lexerResult, cursor, this.context.KeywordProvider);

        ParserResult parserResult = await this.context.Parser.Parse(context, localDiagnostics, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        SourceFileSyntax sourceFile = (SourceFileSyntax)parserResult.Tree.Root;

        SourceFileBindResult bindResult = SourceFileBinder.Bind(sourceFile, localDiagnostics, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        diagnosticReporter.CommitReporter(localDiagnostics);

        SourceAnalysisResult result = new(snapshot, parserResult.Tree)
        {
            Activities = bindResult.Activities
        };

        return result;
    }
}
