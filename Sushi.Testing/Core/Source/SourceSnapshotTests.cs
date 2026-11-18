using System.Text;
using AwesomeAssertions;
using NUnit.Framework;
using Sushi.Core.Source;

namespace Sushi.Testing.Core.Source;

[TestFixture]
public sealed class SourceSnapshotTests
{
    private static readonly Uri testUri = new("file:///TestProject/Test.sus");

    [TestCase(TestName = "FromUTF8 Should Remove Leading UTF-8 BOM")]
    public void FromUTF8ShouldRemoveLeadingUTF8BOM()
    {
        byte[] source =
        [
            0xEF,
            0xBB,
            0xBF,
            (byte)'a',
            (byte)'b',
            (byte)'c'
        ];
        
        SourceSnapshot snapshot = SourceSnapshot.FromUTF8(testUri, version: null, source);

        snapshot.Bytes
            .ToArray()
            .Should()
            .Equal("abc"u8.ToArray());

        snapshot.Text
            .Should()
            .Be("abc");

        snapshot.SourceLength
            .Should()
            .Be(3);
    }
    
    [TestCase(TestName = "FromText Should Remove Leading BOM Character")]
    public void FromTextShouldRemoveLeadingBOMCharacter()
    {
        SourceSnapshot snapshot = SourceSnapshot.FromText(testUri, version: null, "\uFEFFabc");

        snapshot.Text
            .Should()
            .Be("abc");

        snapshot.Bytes
            .ToArray()
            .Should()
            .Equal(Encoding.UTF8.GetBytes("abc"));

        snapshot.SourceLength
            .Should()
            .Be(3);
    }

    [TestCase(TestName = "FromUTF8 Should Preserve Physical Line Terminators")]
    public void FromUTF8ShouldPreservePhysicalLineTerminators()
    {
        const string text = "a\r\nb\rc\nd";

        byte[] source = Encoding.UTF8.GetBytes(text);

        SourceSnapshot snapshot = SourceSnapshot.FromUTF8(testUri, version: null, source);

        snapshot.Bytes
            .ToArray()
            .Should()
            .Equal(source);

        snapshot.Text
            .Should()
            .Be(text);

        snapshot.LineStarts
            .Should()
            .Equal(0, 3, 5, 7);
    }

    [TestCase(TestName = "FromUTF8 Should Copy Input Bytes")]
    public void FromUTF8ShouldCopyInputBytes()
    {
        byte[] source = Encoding.UTF8.GetBytes("abc");

        SourceSnapshot snapshot = SourceSnapshot.FromUTF8(testUri, version: null, source);

        source[0] = (byte)'z';

        snapshot.Bytes
            .ToArray()
            .Should()
            .Equal("abc"u8.ToArray());

        snapshot.Text
            .Should()
            .Be("abc");
    }
}
