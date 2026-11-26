using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Diagnostics;
using Sushi.Core.Diagnostics;

namespace Sushi.Compiler;

/// <inheritdoc cref="ISushiCompiler"/>
public sealed class SushiCompiler : ISushiCompiler
{
    /// <inheritdoc />
    public async Task<CompilationResult> CompileJob([NotNull] string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        IDiagnosticReporter diagnosticReporter = new DiagnosticReporter();

        return await this.CompileAsync(path, diagnosticReporter);
    }

    /// <summary>
    /// Compiles the project at the specified path.
    /// </summary>
    /// <param name="path">
    /// The path where the project or solution lives, or its direct file path.
    /// </param>
    /// <param name="diagnosticReporter">
    /// The reporter used to accumulate diagnostics.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task" /> that returns the result.
    /// </returns>
    private async Task<CompilationResult> CompileAsync([NotNull] string path, [NotNull] IDiagnosticReporter diagnosticReporter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(diagnosticReporter);

        return new CompilationResult(true);
    }
}
