namespace Sushi.Compiler.Backend.Native;

/// <summary>
/// Identifies a linked artifact produced by the compiler.
/// </summary>
public sealed record CompiledArtifact
{
    /// <summary>
    /// Gets the absolute artifact path.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the LLVM target triple for which the artifact was produced.
    /// </summary>
    public string TargetTriple { get; }

    /// <summary>
    /// Creates a new instance of <see cref="CompiledArtifact" />.
    /// </summary>
    /// <param name="filePath">
    /// The absolute or relative artifact path.
    /// </param>
    /// <param name="targetTriple">
    /// The target triple represented by the artifact.
    /// </param>
    public CompiledArtifact(string filePath, string targetTriple)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentException.ThrowIfNullOrEmpty(targetTriple);

        this.FilePath = Path.GetFullPath(filePath);
        this.TargetTriple = targetTriple;
    }
}
