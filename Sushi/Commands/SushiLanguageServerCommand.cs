using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Sushi.Commands.Options;
using Sushi.Diagnostics;

namespace Sushi.Commands;

/// <summary>
/// The language server command is used to run the language server.
/// </summary>
public sealed class SushiLanguageServerCommand : ISushiCommand
{
    /// <inheritdoc />
    public string Name => "lsp";

    /// <inheritdoc />
    public string Description => "Runs a language server that stays open until closed.";

    private const string TCPDescription = "The TCP ip and port to listen on.";

    private const string DebugDescription = "Runs the compiler with debug logging enabled.";

    private const string StdioDescription = "Runs the language server in standard input/output mode.";

    ///  <inheritdoc />
    public IReadOnlyList<CommandArgument> Arguments { get; } = new ReadOnlyCollection<CommandArgument>
    ([
        new("tcp", false, TCPDescription),
        new("stdio", true, StdioDescription),
        new("debug", true, DebugDescription)
    ]);

    /// <inheritdoc />
    public bool AllowsUnnamedParameter => false;

    /// <inheritdoc />
    public async Task<CommandExecutionResult> Execute([NotNull] ICommandContext context)
    {
        LanguageServerOptions options = await LanguageServerOptions.FromContext(context);

        await Diag.Monitor("LanguageServer", () => context.LanguageServer.Run(options.LSPTransportMethod, options.ListenEndpoint, AppMeta.GetVersion()));

        return CommandExecutionResult.FromSuccess();
    }
}
