using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Serilog.Core;
using Sushi.Compiler;
using Sushi.Diagnostics.Exceptions;
using Sushi.LanguageServer;

namespace Sushi.Commands;

/// <inheritdoc />
public sealed class SushiCommandContext : ICommandContext
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Arguments { get; private set; }

    /// <inheritdoc />
    public IReadOnlySet<string> Flags { get; private set; }

    /// <inheritdoc />
    public string? Input { get; private set; }

    /// <inheritdoc />
    public ICommandProvider Provider { get; }

    /// <inheritdoc />
    public ISushiCompiler Compiler { get; }

    /// <inheritdoc />
    public ISushiLanguageServer LanguageServer { get; }

    /// <inheritdoc />
    public LoggingLevelSwitch LevelSwitch { get; }

    /// <summary>
    /// Creates a new instance of <see cref="SushiCommandContext" />.
    /// </summary>
    /// <param name="command">
    /// The command.
    /// </param>
    /// <param name="args">
    /// The raw command line arguments.
    /// </param>
    /// <param name="provider">
    /// The command provider used to discover registered commands.
    /// </param>
    /// <param name="languageServer">
    /// The language server.
    /// </param>
    /// <param name="compiler">
    /// The compiler.
    /// </param>
    /// <param name="levelSwitch">
    /// The level switch used to switch logging level.
    /// </param>
    public SushiCommandContext([NotNull] string command, [NotNull] string[] args, [NotNull] ICommandProvider provider, [NotNull] ISushiLanguageServer languageServer, [NotNull] ISushiCompiler compiler, [NotNull] LoggingLevelSwitch levelSwitch)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(languageServer);
        ArgumentNullException.ThrowIfNull(compiler);
        ArgumentNullException.ThrowIfNull(levelSwitch);
        this.Provider = provider;
        this.Compiler = compiler;
        this.LanguageServer = languageServer;
        this.LevelSwitch = levelSwitch;
        this.ConvertCommandLineArguments(command, args);
    }

    /// <summary>
    /// Converts command line arguments to their respective arguments and flags.
    /// </summary>
    /// <param name="command">
    /// The command.
    /// </param> 
    /// <param name="args">
    /// The raw command line arguments.
    /// </param>
    /// <exception cref="CompilerOptionsException">
    /// Thrown when the syntax of the command line arguments is invalid.
    /// </exception>
    [MemberNotNull(nameof(Arguments), nameof(Flags))]
    private void ConvertCommandLineArguments([NotNull] string command, [NotNull] string[] args)
    {
        Dictionary<string, string> arguments = [with(StringComparer.OrdinalIgnoreCase)];

        HashSet<string> flags = [with(StringComparer.OrdinalIgnoreCase)];

        string? key = null;

        List<CommandArgument> allowedArguments = [.. this.Provider.Commands.SelectMany(command => command.Arguments)];

        bool isFirstArg = true;

        string? rawInput = null;

        foreach (string arg in args)
        {
            if (key is not null)
            {
                arguments[key] = arg;
                key = null;
                isFirstArg = false;
                continue;
            }

            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                key = arg[2..];

                if (!allowedArguments.Any(argument => argument.Name.Equals(key, StringComparison.OrdinalIgnoreCase) && !argument.IsFlag))
                {
                    throw new CompilerOptionsException(CompilerOptionsError.InvalidParameterSyntax, $"Invalid key \"{arg}\".");
                }

                isFirstArg = false;
                continue;
            }
            else if (arg.StartsWith('-', StringComparison.Ordinal))
            {
                string flag = arg[1..];

                flags.Add(flag);

                if (!allowedArguments.Any(argument => argument.Name.Equals(key, StringComparison.OrdinalIgnoreCase) && argument.IsFlag))
                {
                    throw new CompilerOptionsException(CompilerOptionsError.InvalidParameterSyntax, $"Invalid flag \"{arg}\".");
                }

                isFirstArg = false;
                continue;
            }
            else
            {
                ISushiCommand? expectedCommand = this.Provider.Commands.FirstOrDefault(cmd => cmd.Name.Equals(command, StringComparison.OrdinalIgnoreCase));

                rawInput = isFirstArg && expectedCommand is not null && expectedCommand.AllowsUnnamedParameter
                    ? arg
                    : throw new CompilerOptionsException(CompilerOptionsError.InvalidParameterSyntax, $"Invalid parameter {arg} with no key specified. Please use key value pairs (--project \"C:\\Path\\To\\Folder\") or flags (-debug).");
            }
        }

        if (key is not null)
        {
            throw new CompilerOptionsException(CompilerOptionsError.InvalidParameterSyntax, $"Parameter --{key} requires a value.");
        }

        this.Arguments = new ReadOnlyDictionary<string, string>(arguments);
        this.Flags = flags.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        this.Input = rawInput;
    }
}
