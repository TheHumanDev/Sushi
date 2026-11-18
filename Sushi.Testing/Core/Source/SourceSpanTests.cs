using AwesomeAssertions;
using NUnit.Framework;
using Sushi.Core.Source;

namespace Sushi.Testing.Core.Source;

[TestFixture]
public sealed class SourceSpanTests
{
    private static readonly Uri testUri = new("file:///TestProject/Test.sus");

    [TestCase(-1, 1, TestName = "SourceSpan Should Throw If Start Is Less Than 0")]
    [TestCase(1, -1, TestName = "SourceSpan Should Throw If End Is Less Than 0")]
    [TestCase(5, 3, TestName = "SourceSpan Should Throw If End Is Less Than Start")]
    [TestCase(2000, 2030, TestName = "SourceSpan Should Throw If Start Is Out Of SourceSpan Bounds")]
    [TestCase(5, 2000, TestName = "SourceSpan Should Throw If End Is Out Of SourceSpan Bounds")]
    public void SourceSpanShouldThrowIfRangeIsOutOfBounds(int start, int end)
    {
        SourceSnapshot snapshot = SourceSnapshot.FromText(testUri, null, "Test code");

        Action action = () => _ = new SourceSpan(snapshot, start, end);

        action
            .Should()
            .Throw<ArgumentOutOfRangeException>();
    }
}
