namespace Sushi.Core.Source;

/// <summary>
/// Represents an issue with source encoding, such as malformed UTF-8.
/// </summary>
/// <param name="Start">
/// The start of the issue span.
/// </param>
/// <param name="Length">
/// The length of the issue span.
/// </param>
public readonly record struct SourceEncodingIssue(int Start, int Length)
{
    /// <summary>
    /// The end of the issue span.
    /// </summary>
    public int End => this.Start + this.Length;
}
