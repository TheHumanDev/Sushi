using System.Diagnostics.CodeAnalysis;

namespace Sushi.Core.Source;

/// <summary>
/// Represents a half-open byte span within a source snapshot.
/// Coordinates refer to the canonical source after BOM removal.
/// </summary>
public readonly record struct SourceSpan
{
    /// <summary>
    /// Gets the <see cref="SourceSnapshot" /> that this span originated from.
    /// </summary>
    public SourceSnapshot Snapshot { get; }

    /// <summary>
    /// Gets the inclusive starting byte offset.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// Gets the exclusive ending byte offset.
    /// </summary>
    public int End { get; }

    /// <summary>
    /// Creates a new instance of <see cref="SourceSpan"/>.
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
    public SourceSpan([NotNull] SourceSnapshot snapshot, int start, int end)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (start > end)
        {
            throw new ArgumentOutOfRangeException(nameof(end), "End offset cannot be lower than start offset.");
        }

        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "Start offset cannot be lower than 0.");
        }

        if (end < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "End offset cannot be lower than 0.");
        }

        this.Snapshot = snapshot;
        this.Start = start;
        this.End = end;
    }

    /// <summary>
    /// The length in bytes covered by the source span.
    /// </summary>
    public int Length => this.End - this.Start;
}
