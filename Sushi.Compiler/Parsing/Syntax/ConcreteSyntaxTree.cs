using Sushi.Core.Lexing;
using Sushi.Core.Parsing.Syntax;
using Sushi.Core.Source;

namespace Sushi.Compiler.Parsing.Syntax;

/// <inheritdoc cref="IConcreteSyntaxTree" />
public sealed class ConcreteSyntaxTree : IConcreteSyntaxTree
{
    /// <inheritdoc />
    public LexResult LexResult { get; }

    /// <inheritdoc />
    public SyntaxNode Root { get; }

    /// <inheritdoc />
    public SourceSnapshot Snapshot => this.LexResult.Snapshot;

    /// <summary>
    /// Creates a concrete syntax tree over an existing lexical result.
    /// </summary>
    /// <param name="lexResult">
    /// The complete lexical result, including source-backed trivia and recovery elements.
    /// </param>
    /// <param name="root">
    /// The structured syntax root produced by the parser.
    /// </param>
    public ConcreteSyntaxTree(LexResult lexResult, SyntaxNode root)
    {
        ArgumentNullException.ThrowIfNull(lexResult);
        ArgumentNullException.ThrowIfNull(root);

        if (!ReferenceEquals(lexResult.Snapshot, root.Span.Snapshot))
        {
            throw new ArgumentException("The syntax root must belong to the lexical result's source snapshot.", nameof(root));
        }

        this.LexResult = lexResult;
        this.Root = root;
    }
}
