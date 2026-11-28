using Sushi.Core.Parsing.Syntax;
using Sushi.Core.Source;

namespace Sushi.Compiler.Parsing.Syntax;

/// <summary>
/// Preserves source belonging to an activity member that the current parser cannot recognize.
/// </summary>
public sealed class UnsupportedActivityMemberSyntax(SourceSpan span) : ActivityMemberSyntax
{
    /// <inheritdoc />
    public override SyntaxType Type => SyntaxType.UnsupportedActivityMember;

    /// <inheritdoc />
    public override SourceSpan Span { get; } = span;
}
