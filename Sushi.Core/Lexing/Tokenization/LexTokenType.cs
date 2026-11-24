namespace Sushi.Core.Lexing.Tokenization;

/// <summary>
/// Represents the type of a <see cref="LexToken"/>, which determines its meaning.
/// </summary>
public enum LexTokenType
{
    Unknown,
    Whitespace,
    LineComment,
    BlockComment,
    LineTerminator,
    Keyword
}
