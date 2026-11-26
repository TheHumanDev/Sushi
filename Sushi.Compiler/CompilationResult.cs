namespace Sushi.Compiler;

/// <summary>
/// Represents the result of a compilation job.
/// </summary>
/// <param name="Succeeded">
/// Whether the job succeeded or ran into an issue.
/// </param>
public sealed record CompilationResult(bool Succeeded);
