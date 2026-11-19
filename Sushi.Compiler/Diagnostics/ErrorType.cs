namespace Sushi.Compiler.Diagnostics;

/// <summary>
/// Represents a type of error in Sushi.
/// </summary>
#pragma warning disable CA1008 // Enums should have zero value
public enum ErrorType
#pragma warning restore CA1008 // Enums should have zero value
{
    #region Core Errors
    InvalidUtf8 = 1,
    #endregion Core Errors

    #region Syntax Errors
    UnterminatedBlockComment = 201,
    #endregion Syntax Errors
}
