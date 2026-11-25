using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Diagnostics;
using Sushi.Compiler.Parsing.Syntax;
using Sushi.Core.Diagnostics;
using Sushi.Core.Lexing;
using Sushi.Core.Lexing.Tokenization;
using Sushi.Core.Parsing.Syntax;

namespace Sushi.Compiler.Parsing;

/// <summary>
/// Handles activity declaration parsing.
/// </summary>
public sealed class ActivityParser(IKeywordProvider keywordProvider) : IParser<ActivityDeclarationSyntax>
{
    /// <inheritdoc />
    public bool CanStart([NotNull] ParsingContext context)
    {
        if (!context.Cursor.TryPeek(out LexToken accessModifier) || !keywordProvider.IsAccessModifier(accessModifier, context.Cursor.Snapshot))
        {
            return false;
        }

        return (context.Cursor.TryPeek(out LexToken next, lookahead: 1)
            && next.Type is LexTokenType.Keyword
            && context.Cursor.TokenTextEquals(next, "root"u8)) || context.Cursor.TokenTextEquals(next, "activity"u8);
    }

    /// <inheritdoc />
    public async Task<ActivityDeclarationSyntax> Parse([NotNull] ParsingContext context, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(diagnosticReporter);

        cancellationToken.ThrowIfCancellationRequested();

        IDiagnosticReporter localDiagnostics = new DiagnosticReporter();

        SyntaxToken accessModifier = context.Cursor.ParseAccessModifier(localDiagnostics);
        SyntaxToken? rootKeyword = context.Cursor.TryConsumeKeyword("root"u8, out LexToken root) ? new SyntaxToken(SyntaxType.RootKeyword, root) : null;
        SyntaxToken activityKeyword = context.Cursor.ParseKeyword("activity"u8, SyntaxType.ActivityKeyword, "Expected \"activity\" keyword.", localDiagnostics);
        SyntaxToken name = ParseNameComponent(context, localDiagnostics, "Expected activity name.", skipUnknown: false);
        SyntaxToken openBraceToken = context.Cursor.ParsePunctuation((byte)'{', SyntaxType.OpenBraceToken, "Expected \"{\" to begin activity body.", localDiagnostics);
        SyntaxToken closeBraceToken = context.Cursor.ParsePunctuation((byte)'}', SyntaxType.CloseBraceToken, "Expected \"}\" to end activity body.", localDiagnostics);

        diagnosticReporter.CommitReporter(localDiagnostics);

        return new(
            accessModifier,
            rootKeyword,
            activityKeyword,
            name,
            openBraceToken,
            closeBraceToken
        );
    }

    private static SyntaxToken ParseNameComponent([NotNull] ParsingContext context, [NotNull] IDiagnosticReporter diagnosticReporter, string diagnosticMessage, bool skipUnknown)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(diagnosticReporter);

        if (context.Cursor.TryConsumeIdentifier(out LexToken token, skipUnknown))
        {
            SyntaxType type = token.Type is LexTokenType.EscapedIdentifier
                ? SyntaxType.EscapedIdentifierToken
                : SyntaxType.IdentifierToken;

            return new SyntaxToken(type, token);
        }

        int position = context.Cursor.Position;
        context.Cursor.AccumulateExpectedDiagnostic(diagnosticMessage, position, diagnosticReporter);
        return SyntaxToken.Missing(SyntaxType.IdentifierToken, context.Cursor.Snapshot, position);
    }
}
