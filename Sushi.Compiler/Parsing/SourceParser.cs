using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Diagnostics;
using Sushi.Compiler.Parsing.Syntax;
using Sushi.Core.Diagnostics;
using Sushi.Core.Lexing.Tokenization;
using Sushi.Core.Source;

namespace Sushi.Compiler.Parsing;

/// <summary>
/// Parses a source file at the top-level and coordinates parsing delegation.
/// </summary>
public sealed class SourceParser : IParser<ParserResult>
{
    /// <inheritdoc />
    public bool CanStart([NotNull] ParsingContext context) => true;

    /// <inheritdoc />
    public async Task<ParserResult> Parse([NotNull] ParsingContext context, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(diagnosticReporter);
        cancellationToken.ThrowIfCancellationRequested();

        IDiagnosticReporter localDiagnostics = new DiagnosticReporter();

        List<ActivityDeclarationSyntax> activityDeclarations = [];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (context.ActivityParser.CanStart(context))
            {
                activityDeclarations.Add(await context.ActivityParser.Parse(context, localDiagnostics, cancellationToken));
            }
            else
            {
                break;
            }
        }

        SourceSpan? unparsedContentSpan = context.Cursor.TryPeek(out LexToken next)
            ? new SourceSpan(context.Cursor.Snapshot, next.Span.Start, context.Cursor.Snapshot.SourceLength)
            : null;

        SourceFileSyntax sourceFile = new(
            context.Cursor.Snapshot,
            activityDeclarations,
            unparsedContentSpan);

        diagnosticReporter.CommitReporter(localDiagnostics);
        return new ParserResult(new ConcreteSyntaxTree(context.LexResult, sourceFile));
    }
}
