using System.Text;
using Sushi.Core.Source;
using SushiSourceSpan = Sushi.Core.Source.SourceSpan;
using TomlSourceSpan = Tomlyn.Syntax.SourceSpan;

namespace Sushi.Compiler.Configuration;

/// <summary>
/// Contains extensions for toml source spans.
/// </summary>
public static class TomlSourceSpanExtensions
{
    /// <summary>
    /// Converts this source span to a sushi source span.
    /// </summary>
    /// <param name="span">
    /// This <see cref="TomlSourceSpan" /> to convert.
    /// </param>
    /// <param name="snapshot">
    /// The <see cref="SourceSnapshot" />.
    /// </param>
    /// <returns>
    /// The new <see cref="SushiSourceSpan"/>.
    /// </returns>
    public static SushiSourceSpan ToSushiSpan(this TomlSourceSpan span, SourceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        int startUtf16 = NormalizeOffset(span.Start.Offset, snapshot.Text.Length);

        int endUtf16 = span.End.Offset < 0
            ? startUtf16
            : Math.Min(span.End.Offset + 1, snapshot.Text.Length);

        if (endUtf16 < startUtf16)
        {
            endUtf16 = startUtf16;
        }

        int startByte = Encoding.UTF8.GetByteCount(snapshot.Text.AsSpan(0, startUtf16));

        int endByte = Encoding.UTF8.GetByteCount(snapshot.Text.AsSpan(0, endUtf16));

        return new SushiSourceSpan(snapshot, startByte, endByte);
    }

    /// <summary>
    /// Converts this source span to a sushi source span.
    /// </summary>
    /// <param name="span">
    /// This <see cref="Tomlyn.Text.TomlSourceSpan" /> to convert.
    /// </param>
    /// <param name="snapshot">
    /// The <see cref="SourceSnapshot" />.
    /// </param>
    /// <returns>
    /// The new <see cref="SushiSourceSpan"/>.
    /// </returns>
    public static SushiSourceSpan ToSushiSpan(this Tomlyn.Text.TomlSourceSpan span, SourceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        int startUtf16 = NormalizeOffset(span.Start.Offset, snapshot.Text.Length);

        int endUtf16 = span.End.Offset < 0
            ? startUtf16
            : Math.Min(span.End.Offset + 1, snapshot.Text.Length);

        if (endUtf16 < startUtf16)
        {
            endUtf16 = startUtf16;
        }

        int startByte = Encoding.UTF8.GetByteCount(snapshot.Text.AsSpan(0, startUtf16));

        int endByte = Encoding.UTF8.GetByteCount(snapshot.Text.AsSpan(0, endUtf16));

        return new SushiSourceSpan(snapshot, startByte, endByte);
    }

    /// <summary>
    /// Normalizes an offset.
    /// </summary>
    /// <param name="offset">
    /// The offset to normalize.
    /// </param>
    /// <param name="textLength">
    /// The length of the text.
    /// </param>
    /// <returns>
    /// The normalized offset.
    /// </returns>
    private static int NormalizeOffset(int offset, int textLength)
    {
        if (offset < 0)
        {
            return textLength;
        }

        return Math.Min(offset, textLength);
    }
}
