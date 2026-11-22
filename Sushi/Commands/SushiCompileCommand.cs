using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Sushi.Commands.Options;
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
        try
        {
            CompilerOptions options = await CompilerOptions.FromContext(context);

            await Diag.Monitor("Compilation", context.Compiler.CompileJob);
        }
        catch (SushiProgramException exception)
        {
            return CommandExecutionResult.FromFailure(exception.Error, exception.Message);
        }

        return CommandExecutionResult.FromSuccess();
    }
}
