using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Lexing;
using Sushi.Core.Parsing.Syntax;
using Sushi.Core.Source;

namespace Sushi.Testing.Mocks;

public sealed class MockConcreteSyntaxTree : IConcreteSyntaxTree
{
    public LexResult LexResult { get; }

    public SyntaxNode Root { get; }

    public SourceSnapshot Snapshot { get; }

    public MockConcreteSyntaxTree(LexResult lexResult, SyntaxNode root, SourceSnapshot snapshot)
    {
        this.LexResult = lexResult;
        this.Root = root;
        this.Snapshot = snapshot;
    }

    public static MockConcreteSyntaxTree FromEmpty()
    {
        Uri uri =  new("file:///TestProject/Test.sus");
        SourceSnapshot snapshot = SourceSnapshot.FromText(uri, null, "");
        LexResult result = new(snapshot, []);
        SyntaxNode root = new MockSyntaxNode();
        return new(result, root, snapshot);
    }
}
