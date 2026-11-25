using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Lexing;
using Sushi.Core.Lexing.Tokenization;
using Sushi.Core.Source;

namespace Sushi.Compiler.Lexing;

/// <summary>
/// Used as an authoritative source of legal keywords, and aids
/// in matching raw source bytes to their representative forms.
/// </summary>
public sealed class KeywordProvider : IKeywordProvider
{
    /// <inheritdoc />
    public IReadOnlyList<Keyword> Keywords { get; } = new ReadOnlyCollection<Keyword>
    ([
        new("activity", KeywordType.Keyword),
        new("public", KeywordType.AccessModifier),
        new("internal", KeywordType.AccessModifier),
        new("package", KeywordType.AccessModifier),
        new("protected", KeywordType.AccessModifier),
        new("private", KeywordType.AccessModifier)
    ]);

    /// <inheritdoc />
    public bool IsAccessModifier(LexToken token, [NotNull] SourceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (token.Type is LexTokenType.Keyword && this.Keywords
            .Where(x => x.Type is KeywordType.AccessModifier)
            .Any(keyword => TokenEqualsKeyword(token, snapshot, keyword)))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool IsBuiltInType(LexToken token, [NotNull] SourceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (token.Type is LexTokenType.Keyword && this.Keywords
            .Where(x => x.Type is KeywordType.BuiltInType)
            .Any(keyword => TokenEqualsKeyword(token, snapshot, keyword)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets whether the specified token equals the specified keyword given the source snapshot.
    /// </summary>
    /// <param name="token">
    /// The token to check.
    /// </param>
    /// <param name="snapshot">
    /// The snapshot to match against.
    /// </param>
    /// <param name="keyword">
    /// The authoritative keyword.
    /// </param>
    /// <returns>
    /// True if the token equals the keyword semantically.
    /// </returns>
    private static bool TokenEqualsKeyword(LexToken token, [NotNull] SourceSnapshot snapshot, Keyword keyword)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        ReadOnlySpan<byte> keywordSpan = snapshot.Bytes.Span[token.Span.Start..token.Span.End];

        if (keyword.Bytes.SequenceEqual(keywordSpan))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool IsReservedKeyword(ReadOnlySpan<byte> spelling)
    {
        foreach (Keyword candidate in this.Keywords)
        {
            if (candidate.Bytes.SequenceEqual(spelling))
            {
                return true;
            }
        }

        return false;
    }
}
