using System.Diagnostics.CodeAnalysis;
using System.Net;
using Serilog.Events;
using Sushi.Diagnostics.Exceptions;
using Sushi.LanguageServer;

namespace Sushi.Commands.Options;

/// <summary>
/// Contains the options passed to the language server through command line arguments.
/// </summary>
public sealed class LanguageServerOptions
{
    /// <summary>
    /// The IP Address to listen on when in LSP mode.
    /// </summary>
    internal IPEndPoint? ListenEndpoint { get; private set; }

    /// <summary>
    /// Determines by which method the Language Server Protocol is transported between server and client.
    /// </summary>
    internal LSPTransportMethod LSPTransportMethod { get; private set; }

    /// <summary>
    /// Creates a new <see cref="LanguageServerOptions" /> from an <see cref="ICommandContext"/>.
    /// </summary>
    /// <param name="context">
    /// The context used to create the options.
    /// </param>
    /// <returns></returns>
    public static Task<LanguageServerOptions> FromContext([NotNull] ICommandContext context)
    {
        LanguageServerOptions options = new();

        if (context.Flags.Contains("debug"))
        {
            context.LevelSwitch.MinimumLevel = LogEventLevel.Debug;
        }

        if (context.Arguments.TryGetValue("tcp", out string? endpointString))
        {
            if (!IPEndPoint.TryParse(endpointString, out IPEndPoint? endpoint))
            {
                throw new CompilerOptionsException(CompilerOptionsError.InvalidParameterSyntax, $"Invalid TCP endpoint {endpointString}.");
            }

            options.LSPTransportMethod = LSPTransportMethod.TCP;
            options.ListenEndpoint = endpoint;
        }

        if (context.Arguments.ContainsKey("tcp") && context.Flags.Contains("stdio"))
        {
            throw new CompilerOptionsException(CompilerOptionsError.InvalidParameterSyntax, $"Cannot use LSP with both --tcp and -stdio parameters. Choose one or the other.");
        }

        if (context.Flags.Contains("stdio"))
        {
            options.LSPTransportMethod = LSPTransportMethod.Stdio;
        }

        return Task.FromResult(options);
    }
}
