namespace Sushi.Diagnostics;

/// <summary>
/// Represents a specific reason why the program has exited.
/// </summary>
internal enum ExitCode
{
    Success,
    UnhandledException,
    InvalidParameterSyntax
}
