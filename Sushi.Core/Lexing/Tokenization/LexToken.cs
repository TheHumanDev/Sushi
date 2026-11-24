using Sushi.Core.Source;

namespace Sushi.Core.Lexing.Tokenization;

/// <summary>
/// Represents a lexical token emitted by a lexer. Contains its type
/// and its originating <see cref="SourceSpan"/>.
/// </summary>
/// <param name="Type">
/// The <see cref="LexTokenType"/> of the token.
/// </param>
/// <param name="Span">
/// The <see cref="SourceSpan"/> that represents the originating source
/// text that this token represents.
/// </param>
public readonly record struct LexToken(LexTokenType Type, SourceSpan Span);
