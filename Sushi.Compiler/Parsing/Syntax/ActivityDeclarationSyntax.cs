using Sushi.Core.Parsing.Syntax;
using Sushi.Core.Source;

namespace Sushi.Compiler.Parsing.Syntax;

/// <summary>
/// Represents a declared activity with an explicit lifetime classification and ordered members.
/// </summary>
public sealed class ActivityDeclarationSyntax : SyntaxNode
{
    /// <inheritdoc />
    public override SyntaxType Type => SyntaxType.ActivityDeclaration;

    /// <inheritdoc />
    public override SourceSpan Span => new(this.AccessModifier.Span.Snapshot, this.AccessModifier.Span.Start, this.AccessModifier.Span.End);

    /// <summary>
    /// Gets the activity's declared accessibility.
    /// </summary>
    public SyntaxToken AccessModifier { get; }
}
