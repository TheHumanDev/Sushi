namespace Sushi.Compiler.Lexing.Tokenization;

/// <summary>
/// Describes a lexical element recognized by a tokenizer.
/// </summary>
/// <param name="Type">
/// The type of the recognized element.
/// </param>
/// <param name="Length">
/// The number of source bytes consumed by the recognized element.
/// </param>
public readonly record struct LexTokenMatch(LexTokenType Type, int Length);
