using Sushi.Core.Lexing;
using Sushi.Core.Parsing;

namespace Sushi.Core;

/// <summary>
/// Handles registration and discovery between different compilation steps of the compiler.
/// This is mainly used to inject dependencies that may reference each other.
/// </summary>
public interface ISushiContext<TContext> where TContext : IParsingContext
{
    /// <summary>
    /// Handles registration and discovery of keywords and their classifications.
    /// </summary>
    public IKeywordProvider KeywordProvider { get; }

    /// <summary>
    /// The lexer used to convert a source file into a stream of tokens.
    /// </summary>
    public ILexer Lexer { get; }

    /// <summary>
    /// The source parser used to convert a stream of tokens into a syntax tree.
    /// </summary>
    public ISourceParser<TContext> Parser { get; }
}
