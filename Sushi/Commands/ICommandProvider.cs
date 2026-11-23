namespace Sushi.Commands;

/// <summary>
/// Provider used to discover and register commands.
/// </summary>
public interface ICommandProvider
{
    /// <summary>
    /// The list of registered commands.
    /// </summary>
    public IReadOnlyList<ISushiCommand> Commands { get; }
}
