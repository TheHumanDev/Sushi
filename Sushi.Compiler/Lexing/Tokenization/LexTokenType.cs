namespace Sushi.Compiler.Lexing.Tokenization;

/// <summary>
/// Represents the type of a <see cref="LexToken"/>, which determines its meaning.
/// </summary>
public enum LexTokenType
{
    Unknown,
    Whitespace,
    LineComment,
    BlockComment
}
