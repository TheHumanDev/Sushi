using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Serilog;
using Sushi.Commands.Options;
using Sushi.Compiler;
using Sushi.Core.Diagnostics;
using Sushi.Diagnostics;
using Sushi.Diagnostics.Exceptions;

namespace Sushi.Commands;

/// <summary>
/// The compile command is used to compile a project, solution, or folder.
/// </summary>
public sealed class SushiCompileCommand : ISushiCommand
{
    /// <inheritdoc />
    public string Name => "compile";

    /// <inheritdoc />
    public string Description => "Compiles a project, solution, or folder.";

    private const string PathDescription = "The path to the project, solution, or folder. If this is a folder, then it must contain a project or solution file in the root directory.";

    private const string DebugDescription = "Runs the compiler with debug logging enabled.";

    ///  <inheritdoc />
    public IReadOnlyList<CommandArgument> Arguments { get; } = new ReadOnlyCollection<CommandArgument>
    ([
        new("path", false, PathDescription),
        new("debug", true, DebugDescription)
    ]);

    /// <inheritdoc />
    public bool AllowsUnnamedParameter => false;

    /// <inheritdoc />
    public async Task<CommandExecutionResult> Execute([NotNull] ICommandContext context)
    {
        CompilationResult result;
        try
        {
            CompilerOptions options = await CompilerOptions.FromContext(context);

            CancellationToken cancellationToken = new();

            result = await Diag.Monitor("Compilation", () => context.Compiler.CompileJob(options.Path, cancellationToken));

            foreach (SushiDiagnostic diagnostic in result.Diagnostics)
            {
                LogDiagnostic(diagnostic);
            }
        }
        catch (SushiProgramException exception)
        {
            return CommandExecutionResult.FromFailure(exception.Error, exception.Message);
        }

        if (result.Succeeded)
        {
            return CommandExecutionResult.FromSuccess();
        }
        else
        {
            return CommandExecutionResult.FromFailure(SushiProgramError.CompilationFailed, "Compilation failed with errors.");
        }
    }

    /// <summary>
    /// Logs a diagnostic message.
    /// </summary>
    /// <param name="diagnostic">
    /// The message to log.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if diagnostic is null.
    /// </exception>
    private static void LogDiagnostic([NotNull] SushiDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        (int line, int character) = diagnostic.Span.Snapshot.GetUTF16Position(diagnostic.Span.Start);

        string path = diagnostic.Span.Snapshot.Uri.LocalPath;

        switch (diagnostic.Severity)
        {
            case DiagnosticSeverity.Error:
                Log.Error("{Path}({Line},{Column}): {Code}: {Message}", path, line + 1, character + 1, diagnostic.DisplayName, diagnostic.Message);
                break;
            case DiagnosticSeverity.Warning:
                Log.Warning("{Path}({Line},{Column}): {Code}: {Message}", path, line + 1, character + 1, diagnostic.DisplayName, diagnostic.Message);
                break;
            case DiagnosticSeverity.Information:
                Log.Information("{Path}({Line},{Column}): {Code}: {Message}", path, line + 1, character + 1, diagnostic.DisplayName, diagnostic.Message);
                break;
            case DiagnosticSeverity.Hint:
                Log.Debug("{Path}({Line},{Column}): {Code}: {Message}", path, line + 1, character + 1, diagnostic.DisplayName, diagnostic.Message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(diagnostic));
        }
    }
}
