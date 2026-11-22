using Serilog.Core;
using Sushi.Compiler;
using Sushi.Core.Source;
using Sushi.LanguageServer;

namespace Sushi.Commands;

/// <summary>
/// Represents a context of a specific command being invoked. This contains parameters and initial state.
/// </summary>
public interface ICommandContext
{
    /// <summary>
    /// The arguments passed in. These are key-value pairs.
    /// </summary>
    public IReadOnlyDictionary<string, string> Arguments { get; }

    /// <summary>
    /// The flags passed in. These are just boolean values and do not have a parameter.
    /// </summary>
    public IReadOnlySet<string> Flags { get; }

    /// <summary>
    /// The raw input. Used for commands that take in an unnamed parameter.
    /// </summary>
    public string? Input { get; }

    /// <summary>
    /// The command provider used to discover registered commands.
    /// </summary>
    public ICommandProvider Provider { get; }

    /// <summary>
    /// The compiler used to compile Sushi code.
    /// </summary>
    public ISushiCompiler Compiler { get; }

    /// <summary>
    /// The language server used to communicate with an LSP client, such as VS Code.
    /// </summary>
    public ISushiLanguageServer LanguageServer { get; }

    /// <summary>
    /// The source document provider used to discover registered document types.
    /// </summary>
    public ISourceDocumentProvider DocumentProvider { get; }

    /// <summary>
    /// The level at which the logger should log events for.
    /// </summary>
    public LoggingLevelSwitch LevelSwitch { get; }
}
