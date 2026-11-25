using System.Diagnostics.CodeAnalysis;
using Serilog;
using Sushi.Diagnostics.Exceptions;

namespace Sushi.Commands;

/// <summary>
/// The help command is used to describe other commands.
/// </summary>
public sealed class SushiHelpCommand : ISushiCommand
{
    /// <inheritdoc />
    public string Name => "help";

    /// <inheritdoc />
    public string Description => "Provides help information for commands.";

    /// <inheritdoc />
    public IReadOnlyList<CommandArgument> Arguments { get; } = [];

    /// <inheritdoc />
    public bool AllowsUnnamedParameter => true;

    /// <inheritdoc />
    public async Task<CommandExecutionResult> Execute([NotNull] ICommandContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Input))
        {
            Log.Information("Usage: help [CommandName]");
            Log.Information("Available commands:");

            foreach (ISushiCommand cmd in context.Provider.Commands)
            {
                Log.Information(cmd.Name);
            }

            return CommandExecutionResult.FromSuccess();
        }

        string commandName = context.Input.Trim();

        ISushiCommand? command = context.Provider.Commands.FirstOrDefault(cmd => cmd.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));

        if (command is null)
        {
            return CommandExecutionResult.FromFailure(SushiProgramError.InvalidParameterSyntax, $"\"{commandName}\" is not a recognized command name.");
        }

        Log.Information(command.Name);
        Log.Information(command.Description);

        foreach (CommandArgument argument in command.Arguments.OrderBy(arg => arg.IsFlag))
        {
            Log.Information($"{(argument.IsFlag ? "-" : "--")}{argument.Name}");
        }

        return CommandExecutionResult.FromSuccess();
    }
}
