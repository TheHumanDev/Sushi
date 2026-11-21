using System.Diagnostics.CodeAnalysis;
using System.Net;
using Serilog.Events;
using Sushi.Diagnostics.Exceptions;
using Sushi.LanguageServer;

namespace Sushi.Commands.Options;

/// <summary>
/// Contains the options passed to the compiler through command line arguments.
/// </summary>
internal sealed class CompilerOptions
{
    /// <summary>
    /// The path of the project, solution, or folder containing the project.
    /// </summary>
    public string Path { get; private set; } = string.Empty;

    /// <summary>
    /// Creates a new <see cref="LanguageServerOptions" /> from an <see cref="ICommandContext"/>.
    /// </summary>
    /// <param name="context">
    /// The context used to create the options.
    /// </param>
    /// <returns></returns>
    public static Task<CompilerOptions> FromContext([NotNull] ICommandContext context)
    {
        CompilerOptions options = new();

        if (context.Flags.Contains("debug"))
        {
            context.LevelSwitch.MinimumLevel = LogEventLevel.Debug;
        }

        if (!context.Arguments.TryGetValue("path", out string? value))
        {
            throw new CompilerOptionsException(CompilerOptionsError.InvalidParameterSyntax, "A path must be specified when compiling. Use the --path \"Path\\To\\File\\OrFolder.suspoj\" parameter.");
        }

        options.Path = value;

        return Task.FromResult(options);
    }
}
