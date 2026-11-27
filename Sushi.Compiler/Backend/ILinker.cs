using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Backend.Native;

namespace Sushi.Compiler.Backend;

/// <summary>
/// Links native Sushi object files into host executables.
/// </summary>
public interface ILinker
{
    /// <summary>
    /// Links one native object artifact into an executable.
    /// </summary>
    /// <param name="objectArtifact">
    /// The native object file to link.
    /// </param>
    /// <param name="outputPath">
    /// The requested executable path.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel linking.
    /// </param>
    /// <returns>
    /// The linked executable artifact.
    /// </returns>
    public Task<CompiledArtifact> LinkAsync([NotNull] CompiledArtifact objectArtifact, [NotNull] string outputPath, CancellationToken cancellationToken);
}
