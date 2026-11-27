namespace Sushi.Diagnostics.Exceptions;

/// <summary>
/// Represents the type of error that the Sushi program threw.
/// </summary>
public enum SushiProgramError
{
    /// <summary>
    /// The syntax for the command line arguments is invalid.
    /// </summary>
    InvalidParameterSyntax,

    /// <summary>
    /// The project, solution, or directory path for a compile command is invalid.
    /// </summary>
    InvalidPath,

    /// <summary>
    /// The cause of the error is unknown.
    /// </summary>
    UnknownError,
    CompilationFailed
}
