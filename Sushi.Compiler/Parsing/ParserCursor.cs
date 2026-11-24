using Sushi.Core.Lexing;
using Sushi.Core.Lexing.Tokenization;
using Sushi.Core.Source;

namespace Sushi.Compiler.Parsing;

public sealed class ParserCursor(LexResult result, CancellationToken cancellationToken)
{
    /// <summary>
    /// Gets the authoritative source snapshot associated with this cursor.
    /// </summary>
    public SourceSnapshot Snapshot => result.Snapshot;

    /// <summary>
    /// Gets the current index in the complete lexical stream, including trivia.
    /// </summary>
    public int Index { get; private set; }

    /// <summary>
    /// Gets the byte position of the next significant lexical element, or the end of the source if no significant elements remain.
    /// </summary>
    public int Position => this.TryPeek(out LexToken token) ? token.Span.Start : this.Snapshot.SourceLength;

    
    /// <summary>
    /// Inspects a significant lexical element without advancing the cursor. Trivia is ignored,
    /// but Unknown recovery elements remain significant.
    /// </summary>
    /// <param name="token">
    /// The matching lexical element, if present.
    /// </param>
    /// <param name="lookahead">
    /// The number of significant elements to look ahead.
    /// </param>
    /// <returns>
    /// Whether the requested element exists.
    /// </returns>
    public bool TryPeek(out LexToken token, int lookahead = 0) => this.TryFind(lookahead, skipUnknown: false, out token, out _);

    /// <summary>
    /// Consumes the next significant lexical element, including Unknown elements.
    /// </summary>
    /// <param name="token">
    /// The consumed lexical element, if present.
    /// </param>
    /// <returns>
    /// Whether an element was consumed.
    /// </returns>
    public bool TryAdvance(out LexToken token)
    {
        if (!this.TryFind(0, skipUnknown: false, out token, out int tokenIndex))
        {
            return false;
        }

        this.Index = tokenIndex + 1;
        return true;
    }

    /// <summary>
    /// Tries to find a token at the specified position ahead.
    /// </summary>
    /// <param name="lookahead">
    /// The number of tokens to look ahead of the current cursor position.
    /// </param>
    /// <param name="skipUnknown">
    /// Whether to skip unknown tokens as trivia or count them for recovery.
    /// </param>
    /// <param name="token">
    /// The token, if found.
    /// </param>
    /// <param name="tokenIndex">
    /// The index of the token, if found.
    /// </param>
    /// <returns>
    /// True if the token was found. False otherwise.
    /// </returns>
    private bool TryFind(int lookahead, bool skipUnknown, out LexToken token, out int tokenIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lookahead);

        for (int i = this.Index; i < result.Tokens.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LexToken candidate = result.Tokens[i];

            if (IsTrivia(candidate.Type) || (skipUnknown && candidate.Type is LexTokenType.Unknown))
            {
                continue;
            }

            if (lookahead-- > 0)
            {
                continue;
            }

            token = candidate;
            tokenIndex = i;
            return true;
        }

        token = default;
        tokenIndex = -1;
        return false;
    }

    /// <summary>
    /// Consumes a keyword with the exact specified source spelling.
    /// </summary>
    /// <param name="keyword">
    /// The UTF-8 spelling of the expected keyword.
    /// </param>
    /// <param name="token">
    /// The consumed keyword, if matched.
    /// </param>
    /// <returns>
    /// Whether the keyword was consumed.
    /// </returns>
    public bool TryConsumeKeyword(ReadOnlySpan<byte> keyword, out LexToken token)
    {
        if (this.TryFind(0, skipUnknown: false, out LexToken candidate, out int tokenIndex)
            && candidate.Type is LexTokenType.Keyword
            && this.TokenTextEquals(candidate, keyword))
        {
            this.Index = tokenIndex + 1;
            token = candidate;
            return true;
        }

        token = default;
        return false;
    }

    /// <summary>
    /// Compares the exact source spelling of a lexical element with UTF-8 text.
    /// </summary>
    /// <param name="token">
    /// A lexical element from this cursor's source.
    /// </param>
    /// <param name="expected">
    /// The expected UTF-8 spelling.
    /// </param>
    /// <returns>
    /// Whether the spellings match exactly.
    /// </returns>
    public bool TokenTextEquals(LexToken token, ReadOnlySpan<byte> expected)
        => token.Span.Length == expected.Length
        && this.Snapshot.Bytes.Span[token.Span.Start..token.Span.End].SequenceEqual(expected);

    /// <summary>
    /// Returns whether the token type is considered lexical trivia.
    /// </summary>
    /// <param name="type">
    /// The type to check.
    /// </param>
    /// <returns>
    /// True if the token type is trivia. False otherwise.
    /// </returns>
    private static bool IsTrivia(LexTokenType type)
        => type is LexTokenType.Whitespace
        or LexTokenType.LineTerminator
        or LexTokenType.LineComment
        or LexTokenType.BlockComment;
}
