using Sushi.Core.Parsing.Syntax;
using Sushi.Core.Source;

namespace Sushi.Testing.Mocks;

public sealed class MockSyntaxNode : SyntaxNode
{
    public override SyntaxType Type { get; }

    public override SourceSpan Span { get; }
}
