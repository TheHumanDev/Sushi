using Sushi.Core.Source;

namespace Sushi.Core.Parsing.Syntax;

/// <summary>
/// Represents a declaration or recoverable unsupported construct inside an activity body.
/// </summary>
public abstract class ActivityMemberSyntax : SyntaxNode
{
    /// <inheritdoc />
    public abstract override SyntaxType Type { get; }

    /// <inheritdoc />
    public abstract override SourceSpan Span { get; }
}
