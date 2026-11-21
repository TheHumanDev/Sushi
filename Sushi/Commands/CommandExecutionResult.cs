using System.Diagnostics.CodeAnalysis;

namespace Sushi.Commands;

/// <summary>
/// The result of an attempt to execute an <see cref="ISushiCommand"/>.
/// </summary>
public sealed class CommandExecutionResult
{
    /// <summary>
    /// Creates a new instance of <see cref="CommandExecutionResult" />.
    /// </summary>
    private CommandExecutionResult() { }

    /// <summary>
    /// True if the command execution was successful. False otherwise.
    /// </summary>
    [MemberNotNullWhen(false, nameof(FailureReason))]
    public bool Success { get; init; }

    /// <summary>
    /// The reason the command execution failed.
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// Creates a successful <see cref="CommandExecutionResult" />. 
    /// </summary>
    /// <returns>
    /// The new <see cref="CommandExecutionResult" />.
    /// </returns>
    public static CommandExecutionResult FromSuccess() => new() { Success = true };

    /// <summary>
    /// Creates a failed <see cref="CommandExecutionResult" /> with the specified failure reason. 
    /// </summary>
    /// <param name="failureReason">
    /// The reason the command execution failed.
    /// </param>
    /// <returns>
    /// The new <see cref="CommandExecutionResult" />.
    /// </returns>
    public static CommandExecutionResult FromFailure(string failureReason) => new() { Success = false, FailureReason = failureReason };
}
