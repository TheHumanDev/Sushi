using System.Diagnostics.CodeAnalysis;

namespace Sushi.Compiler;

/// <summary>
/// Orchestrates the compilation process through the steps necessary
/// to create a final output.
/// </summary>
public interface ISushiCompiler
{
    /// <summary>
    /// Runs a compile job.
    /// </summary>
    /// <param name="path">
    /// The path to the project, solution, or folder containing the project or solution.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task"/> that returns the result.
    /// </returns>
    public Task<CompilationResult> CompileJob([NotNull] string path);
}
