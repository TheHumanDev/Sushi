using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Intermediate;
using Sushi.Compiler.Intermediate.Lowering;

namespace Sushi.Compiler.Backend.Native;

/// <summary>
/// Builds native executables from lowered Sushi functions.
/// </summary>
public sealed class ExecutableBuilder : IArtifactBuilder
{
    private readonly IModuleEmitter moduleEmitter;
    private readonly IObjectEmitter objectEmitter;
    private readonly IEntryShim entryShim;
    private readonly ILinker linker;

    /// <inheritdoc />
    public ExecutableBuilder([NotNull] IModuleEmitter moduleEmitter, [NotNull] IObjectEmitter objectEmitter, [NotNull] IEntryShim entryShim, [NotNull] ILinker linker)
    {
        ArgumentNullException.ThrowIfNull(moduleEmitter);
        ArgumentNullException.ThrowIfNull(objectEmitter);
        ArgumentNullException.ThrowIfNull(entryShim);
        ArgumentNullException.ThrowIfNull(linker);
        this.moduleEmitter = moduleEmitter;
        this.objectEmitter = objectEmitter;
        this.entryShim = entryShim;
        this.linker = linker;
    }

    /// <inheritdoc />
    public Task<CompiledArtifact> BuildAsync(LoweredFunction? entry, LoweredFunction? stop, [NotNull] IReadOnlyList<LoweredFunction> packageFunctions, [NotNull] string outputPath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(packageFunctions);
        ArgumentNullException.ThrowIfNull(outputPath);

        ValidateRootLifecycleMethod(entry);

        if (stop is not null)
        {
            ValidateRootLifecycleMethod(stop);

            if (entry.Name == stop.Name)
            {
                throw new ArgumentException("Entry and stop methods must have distinct native symbols.", nameof(stop));
            }
        }

        List<LoweredFunction> functions = [.. packageFunctions, entry];

        if (stop is not null)
        {
            functions.Add(stop);
        }

        return this.BuildCoreAsync(
            functions,
            outputPath,
            artifact => this.entryShim.Add(artifact, entry.Name, stop?.Name),
            cancellationToken);
    }

    /// <summary>
    /// Builds an artifact using the supplied functions and output path.
    /// </summary>
    /// <param name="functions">
    /// The functions to include in the module.
    /// </param>
    /// <param name="outputPath">
    /// The output path of the compiled artifact.
    /// </param>
    /// <param name="addNativeEntry">
    /// The action used to inject the native entry with a supplied module artifact.
    /// </param>
    /// <param name="cancellationToken">
    /// Used to cancel compilation.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task" /> that returns the <see cref="CompiledArtifact" />.
    /// </returns>
    private async Task<CompiledArtifact> BuildCoreAsync(IReadOnlyList<LoweredFunction> functions, string outputPath, Action<IModuleArtifact> addNativeEntry, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(outputPath);
        ArgumentNullException.ThrowIfNull(functions);
        ArgumentNullException.ThrowIfNull(addNativeEntry);
        cancellationToken.ThrowIfCancellationRequested();

        string temporaryDirectory = Path.Combine(Path.GetTempPath(), "Sushi", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);

        string objectPath = Path.Combine(temporaryDirectory, OperatingSystem.IsWindows() ? "program.obj" : "program.o");

        try
        {
            using IModuleArtifact moduleArtifact = this.moduleEmitter.Emit("Sushi", functions, cancellationToken);

            addNativeEntry(moduleArtifact);

            CompiledArtifact objectArtifact = this.objectEmitter.EmitForHost(moduleArtifact, objectPath, cancellationToken);

            return await this.linker.LinkAsync(objectArtifact, outputPath, cancellationToken);
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
        }
    }

    /// <summary>
    /// Validates that the root lifecycle method is valid, such as being void and having no parameters.
    /// This may be changed in the future to fit command-line arguments.
    /// </summary>
    /// <param name="method">
    /// The method that represents the root lifecycle method.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if the return type is not void or there are parameters.
    /// </exception>
    private static void ValidateRootLifecycleMethod(LoweredFunction method)
    {
        ArgumentNullException.ThrowIfNull(method);

        if (method.ReturnType is not IRType.Void || method.Parameters.Count != 0)
        {
            throw new ArgumentException("Root lifecycle methods must be parameterless and return void.", nameof(method));
        }
    }
}
