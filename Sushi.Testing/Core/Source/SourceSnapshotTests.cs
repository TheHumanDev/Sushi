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

    [TestCase(TestName = "FromUTF8 Should Preserve Source Without BOM")]
    public void FromUTF8ShouldPreserveSourceWithoutBOM()
    {
        byte[] source = Encoding.UTF8.GetBytes("abc");

        SourceSnapshot snapshot = SourceSnapshot.FromUTF8(testUri, version: null, source);

        snapshot.Bytes
            .ToArray()
            .Should()
            .Equal(source);

        snapshot.Text
            .Should()
            .Be("abc");

        snapshot.SourceLength
            .Should()
            .Be(source.Length);
    }

    [TestCase(TestName = "FromUTF8 Should Preserve Valid Multibyte UTF-8")]
    public void FromUTF8ShouldPreserveValidMultibyteUTF8()
    {
        const string text = "寿司";

        byte[] source = Encoding.UTF8.GetBytes(text);

        SourceSnapshot snapshot = SourceSnapshot.FromUTF8(testUri, version: null, source);

        snapshot.Bytes
            .ToArray()
            .Should()
            .Equal(source);

        snapshot.Text
            .Should()
            .Be(text);

        snapshot.SourceLength
            .Should()
            .Be(source.Length);

        snapshot.IsValidUTF8
            .Should()
            .BeTrue();

        snapshot.EncodingIssues
            .Should()
            .BeEmpty();
    }

    [TestCase(TestName = "FromUtf8 Should Preserve Malformed UTF-8 Bytes")]
    public void FromUTF8ShouldPreserveMalformedUTF8Bytes()
    {
        byte[] source =
        [
            (byte)'a',
            0xFF,
            (byte)'b'
        ];

        SourceSnapshot snapshot = SourceSnapshot.FromUTF8(testUri, version: null, source);

        snapshot.Bytes
            .ToArray()
            .Should()
            .Equal(source);

        snapshot.SourceLength
            .Should()
            .Be(source.Length);

        snapshot.IsValidUTF8
            .Should()
            .BeFalse();

        snapshot.EncodingIssues
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .Be(new SourceEncodingIssue(1, 1));
    }

    [TestCase(TestName = "FromUtf8 Should Preserve Truncated UTF-8 Sequence As One Encoding Issue")]
    public void FromUTF8ShouldPreserveTruncatedUTF8SequenceAsOneEncodingIssue()
    {
        byte[] source =
        [
            (byte)'a',

            // Beginning of the three-byte sequence for U+20AC EURO SIGN,
            // with the final continuation byte missing.
            0xE2,
            0x82
        ];

        SourceSnapshot snapshot = SourceSnapshot.FromUTF8(testUri, version: null, source);

        snapshot.Bytes
            .ToArray()
            .Should()
            .Equal(source);

        snapshot.IsValidUTF8
            .Should()
            .BeFalse();

        snapshot.EncodingIssues
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .Be(new SourceEncodingIssue(1, 2));
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

    [TestCase(TestName = "FromUtf8 And FromText Should Produce Equivalent Canonical Source")]
    public void FromUtf8AndFromTextShouldProduceEquivalentCanonicalSource()
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

        SourceSnapshot byteSnapshot = SourceSnapshot.FromUTF8(testUri, version: null, source);

        SourceSnapshot textSnapshot = SourceSnapshot.FromText(testUri, version: null, "\uFEFFabc");

        byteSnapshot.Bytes
            .ToArray()
            .Should()
            .Equal(textSnapshot.Bytes.ToArray());

        byteSnapshot.Text
            .Should()
            .Be(textSnapshot.Text);

        byteSnapshot.SourceLength
            .Should()
            .Be(textSnapshot.SourceLength);

        byteSnapshot.EncodingIssues
            .Should()
            .Equal(textSnapshot.EncodingIssues);
    }

    [TestCase(TestName = "GetUtf16Position Should Handle Malformed UTF-8")]
    public void GetUtf16PositionShouldHandleMalformedUTF8()
    {
        byte[] source =
        [
            (byte)'a',
            0xFF,
            (byte)'b'
        ];

        SourceSnapshot snapshot = SourceSnapshot.FromUTF8(testUri, version: null, source);

        snapshot.GetUTF16Position(0)
            .Should()
            .Be((0, 0));

        snapshot.GetUTF16Position(1)
            .Should()
            .Be((0, 1));

        snapshot.GetUTF16Position(2)
            .Should()
            .Be((0, 2));

        snapshot.GetUTF16Position(3)
            .Should()
            .Be((0, 3));
    }

    [TestCase("a\r\nb", 0, 0, 0)]
    [TestCase("a\r\nb", 1, 0, 1)]
    [TestCase("a\r\nb", 2, 0, 1)]
    [TestCase("a\r\nb", 3, 1, 0)]
    [TestCase("a\r\nb", 4, 1, 1)]
    [TestCase("a\nb", 1, 0, 1)]
    [TestCase("a\nb", 2, 1, 0)]
    [TestCase("a\rb", 1, 0, 1)]
    [TestCase("a\rb", 2, 1, 0)]
    [TestCase("a\r\n", 2, 0, 1)]
    [TestCase("a\r\n", 3, 1, 0)]
    [TestCase("🍣\r\nX", 4, 0, 2)]
    [TestCase("🍣\r\nX", 5, 0, 2)]
    [TestCase("🍣\r\nX", 6, 1, 0)]
    [TestCase("🍣\r\nX", 7, 1, 1)]
    public void GetUTF16PositionShouldMapLineTerminatorsToLineContentEnd(string text, int byteOffset, int expectedLine, int expectedCharacter)
    {
        SourceSnapshot snapshot = SourceSnapshot.FromText(testUri, null, text);

        (int line, int character) = snapshot.GetUTF16Position(byteOffset);

        line
            .Should()
            .Be(expectedLine);
        
        character
            .Should()
            .Be(expectedCharacter);
    }

    [TestCase("a\r\nb", 0, 1, 1)]
    [TestCase("a\r\nb", 1, 0, 3)]
    [TestCase("a\nb", 1, 0, 2)]
    [TestCase("a\rb", 1, 0, 2)]
    [TestCase("🍣\r\nX", 0, 2, 4)]
    [TestCase("🍣\r\nX", 1, 0, 6)]
    public void GetByteOffsetAndGetUTF16PositionShouldRoundTripValidEditorPositions(string text, int line, int character, int expectedByteOffset)
    {
        SourceSnapshot snapshot = SourceSnapshot.FromText(testUri, null, text);

        int byteOffset = snapshot.GetByteOffset(line, character);
        (int actualLine, int actualCharacter) = snapshot.GetUTF16Position(byteOffset);

        byteOffset
            .Should()
            .Be(expectedByteOffset);
        
        actualLine
            .Should()
            .Be(line);
        
        actualCharacter
            .Should()
            .Be(character);
    }
}
