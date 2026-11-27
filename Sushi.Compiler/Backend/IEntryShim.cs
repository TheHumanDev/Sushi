using System.Diagnostics.CodeAnalysis;

namespace Sushi.Compiler.Backend;

/// <summary>
/// Generates the native entry point for the top-level activity.
/// </summary>
public interface IEntryShim
{
    /// <summary>
    /// Adds the native startup sequence for a compiled task root.
    /// </summary>
    /// <param name="artifact">
    /// The module containing both lifecycle methods.
    /// </param>
    /// <param name="entrySymbol">
    /// The internal symbol for the root entry method.
    /// </param>
    /// <param name="stopSymbol">
    /// The optional internal symbol for the stop method.
    /// </param>
    public void Add([NotNull] IModuleArtifact artifact, [NotNull] string entrySymbol, string? stopSymbol);
}
