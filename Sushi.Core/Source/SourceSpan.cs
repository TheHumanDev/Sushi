namespace Sushi.Core.Source;

/// <summary>
/// Represents a half-open byte span within a source snapshot.
/// Coordinates refer to the canonical source after BOM removal.
/// </summary>
/// <param name="Snapshot">
/// The <see cref="SourceSnapshot" /> that this span originated from.
/// </param>
/// <param name="Start">
/// The inclusive starting byte offset.
/// </param>
/// <param name="End">
/// The exclusive ending byte offset.
/// </param>
public readonly record struct SourceSpan(SourceSnapshot Snapshot, int Start, int End)
{
    /// <summary>
    /// The length in bytes covered by the source span.
    /// </summary>
    public int Length => this.End - this.Start;
}
