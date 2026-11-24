using System.Diagnostics.CodeAnalysis;
using System.Text;
using Sushi.Core.Lexing.Tokenization;

namespace Sushi.Core.Lexing;

/// <summary>
/// Represents a valid keyword with metadata.
/// </summary>
public readonly record struct Keyword
{
    private readonly byte[] rawBytes;

    /// <summary>
    /// The string representation of the keyword.
    /// </summary>
    public string Spelling => Encoding.UTF8.GetString(this.rawBytes);

    /// <summary>
    /// The canonical UTF-8 bytes of the keyword.
    /// </summary>
    public ReadOnlySpan<byte> Bytes => new(this.rawBytes);

    /// <summary>
    /// The type of the keyword. Most keywords are <see cref="KeywordType.Keyword" />,
    /// but some are special like access modifiers.
    /// </summary>
    public KeywordType Type { get; }

    /// <summary>
    /// Creates a new instance of <see cref="Keyword" />.
    /// </summary>
    /// <param name="spelling">
    /// The string representation of the keyword.
    /// </param>
    /// <param name="keyword">
    /// The type of the keyword.
    /// </param>
    public Keyword([NotNull] string spelling, KeywordType keyword)
    {
        ArgumentNullException.ThrowIfNull(spelling);
        this.rawBytes = [.. Encoding.UTF8.GetBytes(spelling)];
        this.Type = keyword;
    }
}
