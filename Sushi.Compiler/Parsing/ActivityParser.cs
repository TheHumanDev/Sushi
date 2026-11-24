using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Parsing.Syntax;
using Sushi.Core.Diagnostics;
using Sushi.Core.Lexing;
using Sushi.Core.Lexing.Tokenization;

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
    public Task<ActivityDeclarationSyntax> Parse([NotNull] ParsingContext context, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
    {
        ActivityDeclarationSyntax activityDeclaration = new();

        return Task.FromResult(activityDeclaration);
    }
}
