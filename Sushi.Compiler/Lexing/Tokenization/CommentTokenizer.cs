using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Source;

namespace Sushi.Compiler.Lexing.Tokenization;

/// <summary>
/// Recognizes line and block comments. 
/// </summary>
public sealed class CommentTokenizer : ILexTokenizer
{
    /// <inheritdoc />
    public bool CanStart(byte firstByte) => firstByte == (byte)'/';

    /// <inheritdoc />
    public bool TryRecognize([NotNull] SourceSnapshot snapshot, int position, CancellationToken cancellationToken, out LexTokenMatch match)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        cancellationToken.ThrowIfCancellationRequested();

        ReadOnlySpan<byte> bytes = snapshot.Bytes.Span;

        if (position < 0 || position >= bytes.Length || bytes[position] != (byte)'/' || position + 1 >= bytes.Length)
        {
            match = default;
            return false;
        }

        if (bytes[position + 1] == (byte)'/')
        {
            match = RecognizeLineComment(bytes, position, cancellationToken);
            return true;
        }

        if (bytes[position + 1] == (byte)'*')
        {
            match = RecognizeBlockComment(bytes, position, cancellationToken);
            return true;
        }

        match = default;
        return false;
    }

    /// <summary>
    /// Recognizes a line comment through the byte immediately preceding
    /// its line terminator or the end of the source.
    /// </summary>
    /// <param name="bytes">
    /// The canonical source bytes being tokenized.
    /// </param>
    /// <param name="position">
    /// The byte position of the opening <c>//</c>.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel lexical analysis.
    /// </param>
    /// <returns>
    /// The recognized line-comment match.
    /// </returns>
    private static LexTokenMatch RecognizeLineComment(ReadOnlySpan<byte> bytes, int position, CancellationToken cancellationToken)
    {
        int end = position + 2;

        while (end < bytes.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (bytes[end] is (byte)'\r' or (byte)'\n')
            {
                break;
            }

            end++;
        }

        return new LexTokenMatch(LexTokenType.LineComment, end - position);
    }

    /// <summary>
    /// Recognizes a block comment.
    /// </summary>
    /// <param name="bytes">
    /// The canonical source bytes being tokenized.
    /// </param>
    /// <param name="position">
    /// The byte position of the opening <c>/*</c>.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel lexical analysis.
    /// </param>
    /// <returns>
    /// The recognized block-comment match.
    /// </returns>
    private static LexTokenMatch RecognizeBlockComment(ReadOnlySpan<byte> bytes, int position, CancellationToken cancellationToken)
    {
        int end = position + 2;
        int depth = 1;

        while (end < bytes.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsBlockCommentStart(bytes, end))
            {
                depth++;
                end += 2;
                continue;
            }

            if (IsBlockCommentEnd(bytes, end))
            {
                depth--;
                end += 2;

                if (depth == 0)
                {
                    return new LexTokenMatch(LexTokenType.BlockComment, end - position);
                }

                continue;
            }

            end++;
        }

        return new LexTokenMatch(LexTokenType.BlockComment, end - position);
    }

    /// <summary>
    /// Determines whether a block comment begins at the specified byte position.
    /// </summary>
    /// <param name="bytes">
    /// The canonical source bytes being inspected.
    /// </param>
    /// <param name="position">
    /// The byte position to inspect.
    /// </param>
    /// <returns>
    /// True when <c>/*</c> begins at the specified position.
    /// </returns>
    private static bool IsBlockCommentStart(ReadOnlySpan<byte> bytes, int position)
        => position + 1 < bytes.Length && bytes[position] == (byte)'/' && bytes[position + 1] == (byte)'*';

    /// <summary>
    /// Determines whether a block comment ends at the specified byte position.
    /// </summary>
    /// <param name="bytes">
    /// The canonical source bytes being inspected.
    /// </param>
    /// <param name="position">
    /// The byte position to inspect.
    /// </param>
    /// <returns>
    /// True when <c>*/</c> begins at the specified position.
    /// </returns>
    private static bool IsBlockCommentEnd(ReadOnlySpan<byte> bytes, int position)
        => position + 1 < bytes.Length && bytes[position] == (byte)'*' && bytes[position + 1] == (byte)'/';
}
