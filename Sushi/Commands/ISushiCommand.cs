using System.Diagnostics.CodeAnalysis;

namespace Sushi.Commands;

/// <summary>
/// Represents a command that can be passed to the program.
/// </summary>
public interface ISushiCommand
{
    /// <summary>
    /// The name of the command. I.e. Sushi help where "help" is the command.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The description of the command used for help messages.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The list of command arguments available for this command.
    /// </summary>
    public IReadOnlyList<CommandArgument> Arguments { get; }
    
    /// <summary>
    /// True if the command allows an unnamed parameter as its primary input. False otherwise.
    /// </summary>
    public bool AllowsUnnamedParameter { get; }

    /// <summary>
    /// Attempts to execute the command with the given context.
    /// </summary>
    /// <param name="context">
    /// The command context that contains parameters, flags, and the provider.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task" /> that returns the result.
    /// </returns>
    public Task<CommandExecutionResult> Execute([NotNull] ICommandContext context);
}
