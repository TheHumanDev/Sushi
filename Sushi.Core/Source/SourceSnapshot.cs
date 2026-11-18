using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Sushi.Core.Source;

/// <summary>
/// Represents a canonical snapshot of a specific source document.
/// </summary>
public sealed class SourceSnapshot
{
    private readonly byte[] bytes;

    private readonly ReadOnlyCollection<int> lineStarts;

    /// <summary>
    /// The document <see cref="System.Uri"/>. This is not always
    /// a path to a local file.
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// The document version number. This is used for concurrency.
    /// </summary>
    public int? Version { get; }

    /// <summary>
    /// A decoded text view of the source.
    ///
    /// For malformed UTF-8, replacement characters may appear here.
    /// <see cref="Bytes" /> remains the authoritative source representation.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// The canonical post-BOM source bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Bytes => this.bytes;

    /// <summary>
    /// The length of the canonical source in bytes.
    /// </summary>
    public int SourceLength => this.bytes.Length;

    /// <summary>
    /// The byte offsets at which logical source lines begin.
    /// </summary>
    public IReadOnlyList<int> LineStarts => this.lineStarts;

    private SourceSnapshot([NotNull] Uri uri, int? version, [NotNull] byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(bytes);
        this.Uri = uri;
        this.Version = version;
        this.bytes = bytes;

        // This is intentionally a lossy convenience view when malformed
        // UTF-8 exists. The byte array above remains authoritative.
        this.Text = Encoding.UTF8.GetString(this.bytes);

        this.lineStarts = BuildLineStarts(this.bytes);
    }

    /// <summary>
    /// Creates a snapshot from raw UTF-8 source bytes.
    /// </summary>
    /// <param name="uri">
    /// The document <see cref="System.Uri"/>. This is not always
    /// a path to a local file.
    /// </param>
    /// <param name="version">
    /// The document version number. This is used for concurrency.
    /// </param>
    /// <param name="source">
    /// The source bytes.
    /// </param>
    /// <returns>
    /// The <see cref="SourceSnapshot"/> created from the source.
    /// </returns>
    public static SourceSnapshot FromUTF8([NotNull] Uri uri, int? version, ReadOnlySpan<byte> source)
    {
        ArgumentNullException.ThrowIfNull(uri);

        source = RemoveBOM(source);

        return new SourceSnapshot(uri, version, source.ToArray());
    }

    /// <summary>
    /// Creates a snapshot from source text supplied by an editor or
    /// another already-decoded text source.
    /// </summary>
    /// <param name="uri">
    /// The document <see cref="System.Uri"/>. This is not always
    /// a path to a local file.
    /// </param>
    /// <param name="version">
    /// The document version number. This is used for concurrency.
    /// </param>
    /// <param name="text">
    /// The source text.
    /// </param>
    /// <returns>
    /// The <see cref="SourceSnapshot"/> created from the source.
    /// </returns>
    public static SourceSnapshot FromText([NotNull] Uri uri, int? version, [NotNull] string text)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(text);

        // If the decoded source surfaced the BOM as U+FEFF, it has the same
        // language-level treatment as the original UTF-8 BOM.
        if (text.Length > 0 && text[0] == '\uFEFF')
        {
            text = text[1..];
        }

        byte[] bytes = Encoding.UTF8.GetBytes(text);

        return new SourceSnapshot(uri, version, bytes);
    }

    /// <summary>
    /// Removes the Byte-Order Mark (BOM) from the source bytes in order to normalize the source.
    /// </summary>
    /// <param name="source">
    /// The source bytes.
    /// </param>
    /// <returns>
    /// The normalized source bytes as a <see cref="ReadOnlySpan{T}"/>.
    /// </returns>
    private static ReadOnlySpan<byte> RemoveBOM(ReadOnlySpan<byte> source)
    {
        if (source.Length >= 3 &&
            source[0] == 0xEF &&
            source[1] == 0xBB &&
            source[2] == 0xBF)
        {
            return source[3..];
        }

        return source;
    }

    /// <summary>
    /// Builds the line starts from the source bytes.
    /// </summary>
    /// <param name="bytes">
    /// The source span of the bytes.
    /// </param>
    /// <returns>
    /// An <see cref="int[]"/> containing the line starts.
    /// </returns>
    private static ReadOnlyCollection<int> BuildLineStarts(ReadOnlySpan<byte> bytes)
    {
        List<int> starts = [0];

        for (int i = 0; i < bytes.Length; i++)
        {
            switch (bytes[i])
            {
                case (byte)'\r':
                    if (i + 1 < bytes.Length && bytes[i + 1] == (byte)'\n')
                    {
                        i++;
                    }

                    starts.Add(i + 1);
                    break;
                case (byte)'\n':
                    starts.Add(i + 1);
                    break;
            }
        }

        return new ReadOnlyCollection<int>(starts);
    }
}
