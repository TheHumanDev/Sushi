namespace Sushi.Diagnostics.Exceptions;

/// <summary>
/// Thrown when the sushi program encounters an options validation or syntax error.
/// </summary>
#pragma warning disable CA1032 // Implement standard exception constructors
internal sealed class SushiProgramException(SushiProgramError error, string message) : Exception(message)
#pragma warning restore CA1032 // Implement standard exception constructors
{
    /// <summary>
    /// The type of the error that was thrown.
    /// </summary>
    internal SushiProgramError Error { get; } = error;
}
