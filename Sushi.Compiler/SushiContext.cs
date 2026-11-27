using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Parsing;
using Sushi.Core;
using Sushi.Core.Configuration;
using Sushi.Core.Lexing;
using Sushi.Core.Parsing;

namespace Sushi.Compiler;

/// <inheritdoc cref="ISushiContext{T}" />
public sealed class SushiContext : ISushiContext<ParsingContext>
{
    /// <inheritdoc />
    public IKeywordProvider KeywordProvider { get; }

    /// <inheritdoc />
    public ILexer Lexer { get; }

    /// <inheritdoc />
    public ISourceParser<ParsingContext> Parser { get; }

    /// <inheritdoc />
    public IProjectSourceLoader ProjectSourceLoader { get; }

    /// <summary>
    /// Creates a new instance of <see cref="SushiContext" />.
    /// </summary>
    /// <param name="keywordProvider">
    /// The keyword provider used to discover registered keywords.
    /// </param>
    /// <param name="lexer">
    /// The lexer used to convert source to a stream of tokens.
    /// </param>
    /// <param name="sourceParser">
    /// The parser used to convert a stream of tokens into
    /// a syntax tree.
    /// </param>
    public SushiContext(
        [NotNull] IKeywordProvider keywordProvider,
        [NotNull] ILexer lexer,
        [NotNull] ISourceParser<ParsingContext> sourceParser,
        [NotNull] IProjectSourceLoader sourceLoader)
    {
        ArgumentNullException.ThrowIfNull(keywordProvider);
        ArgumentNullException.ThrowIfNull(lexer);
        ArgumentNullException.ThrowIfNull(sourceParser);
        ArgumentNullException.ThrowIfNull(sourceLoader);
        this.KeywordProvider = keywordProvider;
        this.Lexer = lexer;
        this.Parser = sourceParser;
        this.ProjectSourceLoader = sourceLoader;
    }
}
