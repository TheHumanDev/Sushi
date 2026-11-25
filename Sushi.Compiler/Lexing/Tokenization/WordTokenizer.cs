using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Lexing;
using Sushi.Core.Lexing.Tokenization;
using Sushi.Core.Source;

namespace Sushi.Compiler.Lexing.Tokenization;

/// <summary>
/// Recognizes identifiers, escaped identifiers, keywords, and boolean literals.
/// </summary>
/// <param name="keywordProvider">
/// The keyword provider used to discover registered keywords.
/// </param>
public sealed class WordTokenizer([NotNull] IKeywordProvider keywordProvider) : ILexTokenizer
{
    /// <inheritdoc />
    public bool CanStart(byte firstByte) => firstByte == (byte)'@' || IsIdentifierStart(firstByte);

    /// <inheritdoc />
    public bool TryRecognize([NotNull] SourceSnapshot snapshot, int position, CancellationToken cancellationToken, out LexTokenMatch match)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        cancellationToken.ThrowIfCancellationRequested();

        ReadOnlySpan<byte> bytes = snapshot.Bytes.Span;

        if (position < 0 || position >= bytes.Length)
        {
            match = default;
            return false;
        }

        if (bytes[position] == (byte)'@')
        {
            return this.TryRecognizeEscapedIdentifier(bytes, position, cancellationToken, out match);
        }

        if (!IsIdentifierStart(bytes[position]))
        {
            match = default;
            return false;
        }

        int end = FindIdentifierEnd(bytes, position, cancellationToken);

        ReadOnlySpan<byte> spelling = bytes[position..end];

        if (IsAllUnderscores(spelling))
        {
            match = default;
            return false;
        }

        match = new LexTokenMatch(keywordProvider.IsReservedKeyword(spelling) ? LexTokenType.Keyword : LexTokenType.Identifier, end - position);

        return true;
    }

    /// <summary>
    /// Recognizes an escaped identifier whose underlying spelling is reserved by the language.
    /// </summary>
    /// <param name="bytes">
    /// The canonical source bytes being tokenized.
    /// </param>
    /// <param name="position">
    /// The byte position of the <c>@</c> escape prefix.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel lexical analysis.
    /// </param>
    /// <param name="match">
    /// The escaped-identifier match when recognition succeeds.
    /// </param>
    /// <returns>
    /// True when the source contains a valid escaped identifier at the specified position. False otherwise.
    /// </returns>
    private bool TryRecognizeEscapedIdentifier(ReadOnlySpan<byte> bytes, int position, CancellationToken cancellationToken, out LexTokenMatch match)
    {
        int identifierStart = position + 1;

        if (identifierStart >= bytes.Length || !IsIdentifierStart(bytes[identifierStart]))
        {
            match = default;
            return false;
        }

        int end = FindIdentifierEnd(bytes, identifierStart, cancellationToken);

        ReadOnlySpan<byte> spelling = bytes[identifierStart..end];

        if (!keywordProvider.IsReservedKeyword(spelling))
        {
            match = default;
            return false;
        }

        match = new LexTokenMatch(LexTokenType.EscapedIdentifier, end - position);

        return true;
    }

    /// <summary>
    /// Finds the end of the maximal identifier-shaped source sequence.
    /// </summary>
    /// <param name="bytes">
    /// The canonical source bytes being tokenized.
    /// </param>
    /// <param name="position">
    /// The byte position of the first identifier character.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel lexical analysis.
    /// </param>
    /// <returns>
    /// The exclusive byte position following the identifier-shaped sequence.
    /// </returns>
    private static int FindIdentifierEnd(ReadOnlySpan<byte> bytes, int position, CancellationToken cancellationToken)
    {
        int end = position + 1;

        while (end < bytes.Length && IsIdentifierPart(bytes[end]))
        {
            cancellationToken.ThrowIfCancellationRequested();
            end++;
        }

        return end;
    }

    /// <summary>
    /// Determines whether a source byte may begin an ordinary identifier.
    /// </summary>
    /// <param name="value">
    /// The source byte to check.
    /// </param>
    /// <returns>
    /// True when the source byte is a valid beginning for an ordinary identifier. False otherwise.
    /// </returns>
    private static bool IsIdentifierStart(byte value) => IsAsciiLetter(value) || value == (byte)'_';

    /// <summary>
    /// Determines whether a source byte may continue an ordinary identifier.
    /// </summary>
    /// <param name="value">
    /// The source byte to check.
    /// </param>
    /// <returns>
    /// True when the source byte is a valid part for an ordinary identifier. False otherwise.
    /// </returns>
    private static bool IsIdentifierPart(byte value) => IsIdentifierStart(value) || value is >= (byte)'0' and <= (byte)'9';

    /// <summary>
    /// Determines whether a source byte is an ASCII letter.
    /// </summary>
    /// <param name="value">
    /// The source byte to check.
    /// </param>
    /// <returns>
    /// True when the source byte is an ASCII letter. False otherwise.
    /// </returns>
    private static bool IsAsciiLetter(byte value) => value is (>= (byte)'A' and <= (byte)'Z') or (>= (byte)'a' and <= (byte)'z');

    /// <summary>
    /// Determines whether an identifier-shaped spelling consists entirely of underscores.
    /// </summary>
    /// <param name="spelling">
    /// The identifier-shaped spelling without the escape prefix.
    /// </param>
    /// <returns>
    /// True if the identifier is comprised entirely of underscore characters. False otherwise.
    /// </returns>
    private static bool IsAllUnderscores(ReadOnlySpan<byte> spelling)
    {
        foreach (byte value in spelling)
        {
            if (value != (byte)'_')
            {
                return false;
            }
        }

        return true;
    }
}
