using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Diagnostics;
using Sushi.Core.Lexing.Tokenization;
using Sushi.Core.Parsing.Syntax;

namespace Sushi.Compiler.Parsing;

/// <summary>
/// Parses blocks such as method bodies.
/// </summary>
public sealed class BlockParser : IParser<BlockSyntax>
{
    public bool CanStart([NotNull] ParsingContext context)
        => context.Cursor.TryPeek(out LexToken openingBrace) && context.Cursor.IsPunctuation(openingBrace, (byte)'{');
    ///<inheritdoc />
    public Task<BlockSyntax> Parse([NotNull] ParsingContext context, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(diagnosticReporter);

        SyntaxToken openBrace = context.Cursor.ParsePunctuation((byte)'{', SyntaxType.OpenBraceToken, "Expected \"{\" to begin statement block.", diagnosticReporter);
        List<StatementSyntax> statements = [];

        SyntaxToken closeBrace = context.Cursor.ParsePunctuation((byte)'}', SyntaxType.CloseBraceToken, "Expected \"}\" to end statement block.", diagnosticReporter);
        return Task.FromResult(new BlockSyntax(openBrace, statements, closeBrace));
    }
}
