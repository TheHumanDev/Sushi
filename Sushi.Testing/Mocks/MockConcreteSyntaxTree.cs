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

    public MockConcreteSyntaxTree() : this(null!, null!, null!) { }

    public MockConcreteSyntaxTree([NotNull] LexResult lexResult, [NotNull] SyntaxNode root, [NotNull] SourceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(lexResult);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(snapshot);
        this.LexResult = lexResult;
        this.Root = root;
        this.Snapshot = snapshot;
    }
}
