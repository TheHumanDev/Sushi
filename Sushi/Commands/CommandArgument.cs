namespace Sushi.Commands;

/// <summary>
/// Represents an argumetn for a specific command.
/// </summary>
/// <param name="Name">
/// The name of the command argument.
/// </param>
/// <param name="isFlag">
/// True if the argument is a flag. False if it is a parameter.
/// </param>
/// <param name="Description">
/// The description of the command argument. This is used for help messages.
/// </param>
public readonly record struct CommandArgument(string Name, bool IsFlag, string Description);
