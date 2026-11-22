namespace Sushi.Diagnostics;

/// <summary>
/// Represents a specific reason why the program has exited.
/// </summary>
internal enum ExitCode
{
    /// <summary>
    /// Successful program termination.
    /// </summary>
    Success,

    /// <summary>
    /// An exception was not handled.
    /// </summary>
    UnhandledException,

    /// <summary>
    /// A command line parameter was invalid.
    /// </summary>
    InvalidParameterSyntax,

    /// <summary>
    /// A project file or folder path was invalid.
    /// </summary>
    InvalidProjectFileOrFolder
}
