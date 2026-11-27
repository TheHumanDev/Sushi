using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Backend.Native;
using Sushi.Core.Diagnostics;

namespace Sushi.Compiler;

/// <summary>
/// Represents the result of a compilation job.
/// </summary>

public sealed record CompilationResult
{
    /// <summary>
    /// Whether the job succeeded or ran into an issue.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Artifact))]
    public bool Succeeded { get; }

    /// <summary>
    /// Any accumulated diagnostics during compilation.
    /// </summary>
    public IReadOnlyList<SushiDiagnostic> Diagnostics { get; init; }

    /// <summary>
    /// The compiled artifact. Null if the compilation result failed.
    /// </summary>
    CompiledArtifact? Artifact { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="CompilationResult" />.
    /// </summary>
    /// <param name="succeeded">
    /// Whether the job succeeded or ran into an issue.
    /// </param>
    public CompilationResult(bool succeeded)
    {
        this.Succeeded = succeeded;
        this.Diagnostics = [];
    }

    /// <summary>
    /// Creates a failed <see cref="CompilationResult" /> with a list of diagnostics.
    /// </summary>
    /// <param name="diagnostics">
    /// Any accumulated diagnostics during compilation.
    /// </param>
    /// <returns>
    /// The compilation result.
    /// </returns>
    internal static CompilationResult FromFailure([NotNull] IReadOnlyList<SushiDiagnostic> diagnostics) => new(false) { Diagnostics = diagnostics };

    /// <summary>
    /// Creates a successful <see cref="CompilationResult" />.
    /// </summary>
    /// <param name="diagnostics">
    /// The accumulated diagnostics during compilation. Note that this should only be filled with
    /// non-error diagnostics when returning success.
    /// </param>
    /// <param name="artifact">
    /// The artifact created during compilation.
    /// </param>
    /// <returns>
    /// The compilation result.
    /// </returns>
    internal static CompilationResult FromSuccess([NotNull] IReadOnlyList<SushiDiagnostic> diagnostics, [NotNull] CompiledArtifact artifact)
        => new(true) { Diagnostics = diagnostics, Artifact = artifact };
}
