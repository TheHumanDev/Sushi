using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Diagnostics;
using Sushi.Compiler.Parsing.Syntax;
using Sushi.Core.Diagnostics;
using Sushi.Core.Lexing.Tokenization;
using Sushi.Core.Parsing;
using Sushi.Core.Source;

namespace Sushi.Compiler.Parsing;

/// <summary>
/// Parses a source file at the top-level and coordinates parsing delegation.
/// </summary>
public sealed class SourceParser : IParser<ParserResult>, ISourceParser<ParsingContext>
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
                int previousIndex = context.Cursor.Index;
                ActivityDeclarationSyntax declaration = await context.ActivityParser.Parse(context, localDiagnostics, cancellationToken);

                if (context.Cursor.Index <= previousIndex)
                {
                    throw new InvalidOperationException("Activity parsing did not advance the token cursor.");
                }

                activityDeclarations.Add(declaration);
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
