using Sushi.Core.Semantics;

namespace Sushi.Compiler.Semantics;

/// <summary>
/// Represents semantic information produced by binding one Sushi source file.
/// </summary>
public sealed record SourceFileBindResult
{
    /// <summary>
    /// Gets the source file's successfully bound activity declarations.
    /// Invalid activity declarations remain available through the syntax tree.
    /// </summary>
    public IReadOnlyList<BoundActivity> Activities { get; init; } = [];
}
