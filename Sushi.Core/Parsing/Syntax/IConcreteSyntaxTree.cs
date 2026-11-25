using Sushi.Core.Lexing;
using Sushi.Core.Source;

namespace Sushi.Core.Parsing.Syntax;

/// <summary>
/// Represents parsed Sushi syntax together with the complete
/// lexical source model from which it was produced.
/// </summary>
public interface IConcreteSyntaxTree
{
    /// <summary>
    /// Gets the complete lexical result underlying this syntax tree.
    /// </summary>
    public LexResult LexResult { get; }

    /// <summary>
    /// Gets the structured root of the syntax tree.
    /// </summary>
    public SyntaxNode Root { get; }

    /// <summary>
    /// Gets the authoritative source snapshot represented by the tree.
    /// </summary>
    public SourceSnapshot Snapshot { get; }
}
