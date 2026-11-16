using System.Net;
using Sushi.Diagnostics.Exceptions;
using Sushi.LanguageServer;

namespace Sushi;

/// <summary>
/// Contains the options passed to the compiler through command line arguments.
/// </summary>
internal sealed class CompilerOptions
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
    /// Whether the compiler is running in LSP mode.
    /// </summary>
    internal bool IsInLanguageServerMode { get; private set; }

    /// <summary>
    /// Whether debug logging is enabled.
    /// </summary>
    internal bool IsDebugLoggingEnabled { get; private set; }

    /// <summary>
    /// Creates a new instance of <see cref="CompilerOptions"/>.
    /// </summary>
    private CompilerOptions() { }

    /// <summary>
    /// The list of allowed flags.
    /// </summary>
    private static readonly HashSet<string> allowedFlags =
    [
      with(StringComparer.OrdinalIgnoreCase),
      "lsp",
      "stdio",
      "debug"  
    ];

    /// <summary>
    /// The list of allowed keys.
    /// </summary>
    private static readonly HashSet<string> allowedKeys =
    [
      with(StringComparer.OrdinalIgnoreCase),
      "tcp"
    ];

    /// <summary>
    /// Processes the command line arguments into a <see cref="CompilerOptions"/> object.
    /// </summary>
    /// <param name="args">
    /// The command line arguments.
    /// </param>
    /// <returns>
    /// A <see cref="CompilerOptions"/> object.
    /// </returns>
    internal static CompilerOptions FromCommandLineArguments(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        CompilerOptions options = new();

        Dictionary<string, string> arguments = [with(StringComparer.OrdinalIgnoreCase)];

        HashSet<string> flags = [with(StringComparer.OrdinalIgnoreCase)];

        string? key = null;

        foreach (string arg in args)
        {
            if (key is not null)
            {
                arguments[key] = arg;
                key = null;
                continue;
            }

            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                key = arg[2..];

                if (!allowedKeys.Contains(key))
                {
                    throw new CompilerOptionsException(CompilerOptionsError.InvalidParameterSyntax, $"Invalid parameter \"{arg}\".");
                }

                continue;
            }
            else if (arg.StartsWith('-', StringComparison.Ordinal))
            {
                string flag = arg[1..];

                flags.Add(flag);

                if (!allowedFlags.Contains(flag))
                {
                    throw new CompilerOptionsException(CompilerOptionsError.InvalidParameterSyntax, $"Invalid parameter \"{arg}\".");
                }

                continue;
            }
            else
            {
                throw new CompilerOptionsException(CompilerOptionsError.InvalidParameterSyntax, $"Invalid parameter {arg} with no key specified. Please use key value pairs (--project \"C:\\Path\\To\\Folder\") or flags (-debug).");
            }
        }

        if (key is not null)
        {
            throw new CompilerOptionsException(CompilerOptionsError.InvalidParameterSyntax, $"Parameter --{key} requires a value.");
        }

        if (arguments.TryGetValue("tcp", out string? endpointString))
        {
            if (!IPEndPoint.TryParse(endpointString, out IPEndPoint? endpoint))
            {
                throw new CompilerOptionsException(CompilerOptionsError.InvalidParameterSyntax, $"Invalid TCP endpoint {endpointString}.");
            }

            options.LSPTransportMethod = LSPTransportMethod.TCP;
            options.ListenEndpoint = endpoint;
        }

        if (arguments.ContainsKey("tcp") && flags.Contains("stdio"))
        {
            throw new CompilerOptionsException(CompilerOptionsError.InvalidParameterSyntax, $"Cannot use LSP with both --tcp and -stdio parameters. Choose one or the other.");
        }

        if (flags.Contains("lsp"))
        {
            options.IsInLanguageServerMode = true;
        }

        if (flags.Contains("stdio"))
        {
            options.LSPTransportMethod = LSPTransportMethod.Stdio;
        }

        if (flags.Contains("debug"))
        {
            options.IsDebugLoggingEnabled = true;
        }

        return options;
    }
}
