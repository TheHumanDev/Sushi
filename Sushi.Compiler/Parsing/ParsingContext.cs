using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Lexing;
using Sushi.Core.Parsing;

namespace Sushi.Compiler.Parsing;

/// <inheritdoc cref="IParsingContext" />
public sealed class ParsingContext : IParsingContext
{
    /// <summary>
    /// The top-level parser that handles source-file parsing.
    /// </summary>
    public SourceParser SourceParser { get; }

    /// <summary>
    /// Parses activity declarations.
    /// </summary>
    public ActivityParser ActivityParser { get; }

    /// <summary>
    /// Represents the current position of the parsing pass.
    /// From the cursor, the parser can advance or peek ahead.
    /// </summary>
    public ParserCursor Cursor { get; }

    /// <summary>
    /// The lex result from the lexing pass.
    /// </summary>
    public LexResult LexResult { get; }

    /// <summary>
    /// Creates a new instance of <see cref="ParsingContext" />.
    /// </summary>
    /// <param name="result">
    /// The lex result of the lexing pass.
    /// </param>
    /// <param name="cursor">
    /// The cursor used to traverse the token stream.
    /// </param>
    /// <param name="keywordProvider">
    /// The keyword provider used to discover registered keywords
    /// and compare tokens to them.
    /// </param>
    public ParsingContext([NotNull] LexResult result, [NotNull] ParserCursor cursor, [NotNull] IKeywordProvider keywordProvider)
    {
        ArgumentNullException.ThrowIfNull(cursor);
        ArgumentNullException.ThrowIfNull(keywordProvider);

        this.LexResult = result;
        this.Cursor = cursor;
        this.ActivityParser = new(keywordProvider);
        this.SourceParser = new();
    }
}
