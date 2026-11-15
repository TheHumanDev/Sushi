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
    /// <returns>
    /// An awaitable <see cref="Task"/>.
    /// </returns>
    public Task CompileJob();
}
