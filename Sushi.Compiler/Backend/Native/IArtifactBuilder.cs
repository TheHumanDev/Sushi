using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Intermediate.Lowering;

namespace Sushi.Compiler.Backend.Native;

public interface IArtifactBuilder
{
    /// <summary>
    /// Builds an artifact using the supplied functions with the specified output path.
    /// </summary>
    /// <param name="entry">
    /// The optional compiled root entry method. Note that for executables, this should
    /// be non-null as executables always need an entry point.
    /// </param>
    /// <param name="stop">
    /// The optional compiled stop method.
    /// </param>
    /// <param name="packageFunctions">
    /// Additional functions included in the same native module.
    /// </param>
    /// <param name="outputPath">
    /// The requested artifact path.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancels compilation and linking.
    /// </param>
    /// <returns>
    /// The linked native artifact.
    /// </returns>
    public Task<CompiledArtifact> BuildAsync(LoweredFunction? entry, LoweredFunction? stop, [NotNull] IReadOnlyList<LoweredFunction> packageFunctions, [NotNull] string outputPath, CancellationToken cancellationToken);
}
