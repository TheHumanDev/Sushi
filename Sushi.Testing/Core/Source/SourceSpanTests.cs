using System.Text;
using AwesomeAssertions;
using NUnit.Framework;
using Sushi.Core.Source;

namespace Sushi.Testing.Core.Source;

[TestFixture]
public sealed class SourceSpanTests
{
    private static readonly Uri testUri = new("file:///TestProject/Test.sus");

    [TestCase(-1, 1, "start", TestName = "SourceSpan Should Throw If Start Is Less Than 0")]
    [TestCase(1, -1, "end", TestName = "SourceSpan Should Throw If End Is Less Than 0")]
    [TestCase(5, 3, "end", TestName = "SourceSpan Should Throw If End Is Less Than Start")]
    [TestCase(2000, 2030, "start", TestName = "SourceSpan Should Throw If Start Is Out Of SourceSpan Bounds")]
    [TestCase(5, 2000, "end", TestName = "SourceSpan Should Throw If End Is Out Of SourceSpan Bounds")]
    public void SourceSpanShouldThrowIfRangeIsOutOfBounds(int start, int end, string parameterName)
    {
        SourceSnapshot snapshot = SourceSnapshot.FromText(testUri, null, "Test code");

        Action action = () => _ = new SourceSpan(snapshot, start, end);

        action
            .Should()
            .Throw<ArgumentOutOfRangeException>()
            .Which.ParamName
            .Should()
            .Be(parameterName);
    }

    [TestCase("ABCDE", 0, 0, 0, TestName = "SourceSpan Should Allow Empty Span At Position Zero")]
    [TestCase("ABCDE", 5, 5, 0, TestName = "SourceSpan Should Allow Empty Span At End Of Source")]
    [TestCase("ABCDE", 0, 5, 5, TestName = "SourceSpan Should Allow Span Covering Entire Source")]
    [TestCase("ABCDE", 2, 3, 1, TestName = "SourceSpan Should Allow Span Covering Single Byte")]
    [TestCase("", 0, 0, 0, TestName = "SourceSpan Should Allow Empty Span In Empty Source")]
    public void SourceSpanShouldAcceptValidRanges(string text, int start, int end, int expectedLength)
    {
        SourceSnapshot snapshot = SourceSnapshot.FromText(testUri, null, text);

        SourceSpan span = new(snapshot, start, end);

        span.Snapshot
            .Should()
            .BeSameAs(snapshot);
        
        span.Start
            .Should()
            .Be(start);
            
        span.End
            .Should()
            .Be(end);

        span.Length
            .Should()
            .Be(expectedLength);
    }

    [TestCase(TestName = "SourceSpan Should Measure UTF-8 Bytes Rather Than Characters")]
    public void SourceSpanShouldMeasureUTF8BytesRatherThanCharacters()
    {
        SourceSnapshot snapshot = SourceSnapshot.FromText(testUri, null, "Aé🍣B");

        SourceSpan span = new(snapshot, 1, 7);

        snapshot.SourceLength
            .Should()
            .Be(8);

        span.Start
            .Should()
            .Be(1);

        span.End
            .Should()
            .Be(7);

        span.Length
            .Should()
            .Be(6);

        string selectedText = Encoding.UTF8.GetString(snapshot.Bytes.Span[span.Start..span.End]);
        selectedText
            .Should()
            .Be("é🍣");
    }

    [TestCase(TestName = "SourceSpan Should Throw If SourceSnapshot Is Null")]
    public void SourceSpanShouldThrowIfSourceSnapshotIsNull()
    {
        Action action = () => _ = new SourceSpan(null!, 0, 0);

        action
            .Should()
            .Throw<ArgumentNullException>()
            .Which.ParamName
            .Should()
            .Be("snapshot");
    }
}
