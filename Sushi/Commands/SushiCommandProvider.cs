using System.Collections.ObjectModel;

namespace Sushi.Commands;

/// <inheritdoc />
public sealed class SushiCommandProvider : ICommandProvider
{
    /// <inheritdoc />
    public IReadOnlyList<ISushiCommand> Commands { get; } = new ReadOnlyCollection<ISushiCommand>
    ([
       new SushiHelpCommand(),
       new SushiLanguageServerCommand(),
       new SushiCompileCommand()
    ]);
}
