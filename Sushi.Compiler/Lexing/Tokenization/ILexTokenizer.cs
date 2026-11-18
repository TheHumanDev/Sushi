using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Source;

namespace Sushi.Compiler.Lexing.Tokenization;

/// <summary>
/// Represents a token handler that knows how to recognize a specific token type or set of token types.
/// </summary>
public interface ILexTokenizer
{
    /// <summary>
    /// Returns whether the tokenizer recognizes the first byte of a stream of bytes
    /// as a valid candidate for full recognition.
    /// </summary>
    /// <param name="firstByte">
    /// The first byte to check for candidacy.
    /// </param>
    /// <returns>
    /// True if the tokenizer can start a tokenization attempt from the specified byte. False otherwise.
    /// </returns>
    public bool CanStart(byte firstByte);

    /// <summary>
    /// Tries to recognize a token at the specified position in the given <see cref="SourceSnapshot"/>.
    /// </summary>
    /// <param name="snapshot">
    /// The <see cref="SourceSnapshot"/> to analyze.
    /// </param>
    /// <param name="position">
    /// The position of the first byte.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel tokenization.
    /// </param>
    /// <param name="match">
    /// The resultant match if one is found. If the return value is false, then this will be the default value.
    /// </param>
    /// <returns>
    /// True if a valid token match was recognized. False otherwise.
    /// </returns>
    public bool TryRecognize([NotNull] SourceSnapshot snapshot, int position, CancellationToken cancellationToken, out LexTokenMatch match);
}
