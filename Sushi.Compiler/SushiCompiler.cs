using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Analysis;
using Sushi.Compiler.Backend.Native;
using Sushi.Compiler.Diagnostics;
using Sushi.Compiler.Intermediate.Lowering;
using Sushi.Compiler.Parsing;
using Sushi.Core;
using Sushi.Core.Analysis;
using Sushi.Core.Configuration;
using Sushi.Core.Diagnostics;
using Sushi.Core.Source;

namespace Sushi.Compiler;

/// <inheritdoc cref="ISushiCompiler"/>
public sealed class SushiCompiler([NotNull] ISushiContext<ParsingContext> context, [NotNull] ExecutableBuilder executableBuilder, [NotNull] IProjectSourceLoader sourceLoader) : ISushiCompiler
{
    /// <inheritdoc />
    public async Task<CompilationResult> CompileJob([NotNull] string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string projectPath = await sourceLoader.ResolveProjectPath(path, cancellationToken);

            string? projectDirectory = Path.GetDirectoryName(projectPath);

            if (string.IsNullOrWhiteSpace(projectDirectory))
            {
                IDiagnosticReporter diagnosticReporter = new DiagnosticReporter();
                diagnosticReporter.GenerateErrorWithCustomMessage(ErrorType.ExpectedSyntax, $"Project \"{path}\" has no containing directory.", new SourceSpan());
                return CompilationResult.FromFailure(diagnosticReporter.ReportDiagnostics());
            }

            return await this.CompileAsync(projectPath, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            IDiagnosticReporter diagnosticReporter = new DiagnosticReporter();
            diagnosticReporter.GenerateErrorWithCustomMessage(ErrorType.ExpectedSyntax, exception.Message, new SourceSpan());
            return CompilationResult.FromFailure(diagnosticReporter.ReportDiagnostics());
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    /// <summary>
    /// Compiles the project at the specified path.
    /// </summary>
    /// <param name="path">
    /// The path where the project or solution lives, or its direct file path.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel compilation.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task" /> that returns the result.
    /// </returns>
    private async Task<CompilationResult> CompileAsync([NotNull] string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        cancellationToken.ThrowIfCancellationRequested();

        IDiagnosticReporter diagnosticReporter = new DiagnosticReporter();

        SourceSnapshot projectSnapshot = await sourceLoader.LoadSnapshot(path, cancellationToken);

        ProjectAnalysisResult projectResult = (ProjectAnalysisResult)await new ProjectAnalyzer().Analyze(projectSnapshot, diagnosticReporter, cancellationToken);

        if (diagnosticReporter.HasErrors())
        {
            return CompilationResult.FromFailure(diagnosticReporter.ReportDiagnostics());
        }

        ProjectDefinition project = projectResult.Project ?? throw new InvalidOperationException("Project analysis succeeded without producing a project definition.");

        IReadOnlyList<string> sourcePaths = sourceLoader.DiscoverSourcePaths(path, project, cancellationToken);

        if (sourcePaths.Count == 0)
        {
            throw new InvalidOperationException($"Project \"{project.Name}\" did not select any Sushi source files.");
        }

        List<SourceAnalysisResult> sources = [];

        foreach (string sourcePath in sourcePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SourceSnapshot snapshot = await sourceLoader.LoadSnapshot(sourcePath, cancellationToken);
            SourceAnalysisResult source = (SourceAnalysisResult)await new SourceAnalyzer(context).Analyze(snapshot, diagnosticReporter, cancellationToken);

            sources.Add(source);
        }

        LoweredBasicBlock entryBlock = new("__sushi_root_entry", [], new LoweredReturnTerminator());

        LoweredBasicBlock stopBlock = new("__sushi_root_stop", [], new LoweredReturnTerminator());

        LoweredFunction? entry = new(Intermediate.IRAccessibility.Public, "__sushi_root_entry", Intermediate.IRType.Void, new ReadOnlyCollection<LoweredBasicBlock>([entryBlock]));

        LoweredFunction? stop = new(Intermediate.IRAccessibility.Public, "__sushi_root_stop", Intermediate.IRType.Void, new ReadOnlyCollection<LoweredBasicBlock>([stopBlock]));

        if (diagnosticReporter.HasErrors() || entry is null)
        {
            return CompilationResult.FromFailure(diagnosticReporter.ReportDiagnostics());
        }

        string outputPath = GetOutputPath(path, project);

        // TODO: Eventually project configuration will determine what builder to use.
        CompiledArtifact artifact = await executableBuilder.BuildAsync(entry, stop, [], outputPath, cancellationToken);

        return CompilationResult.FromSuccess(diagnosticReporter.ReportDiagnostics(), artifact);
    }

    private static string GetOutputPath(string projectDirectory, ProjectDefinition project)
    {
        string target = project.Build.DefaultTarget
            ?? throw new InvalidOperationException("The analyzed project does not have a default build target.");

        return Path.Combine(projectDirectory, "bin", target, project.Assembly);
    }
}
