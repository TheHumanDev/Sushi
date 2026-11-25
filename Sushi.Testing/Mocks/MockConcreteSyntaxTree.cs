using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Parsing.Syntax;
using Sushi.Core.Lexing;
using Sushi.Core.Parsing.Syntax;
using Sushi.Core.Source;

namespace Sushi.Testing.Mocks;

public sealed class MockConcreteSyntaxTree(LexResult lexResult, SyntaxNode root, SourceSnapshot snapshot) : IConcreteSyntaxTree
{
    public LexResult LexResult { get; } = lexResult;

    public SyntaxNode Root { get; } = root;

    public SourceSnapshot Snapshot { get; } = snapshot;

    public static MockConcreteSyntaxTree FromSnapshot(SourceSnapshot snapshot)
    {
        LexResult result = new(snapshot, []);
        SourceFileSyntax root = new(snapshot, [], null);
        return new(result, root, snapshot);
    }
}
