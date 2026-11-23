namespace Sushi.Core.Diagnostics;

/// <summary>
/// Used to generate new diagnostic reporters.
/// </summary>
public interface IDiagnosticReporterFactory
{
    /// <summary>
    /// Creates a new <see cref="IDiagnosticReporter"/> and returns it.
    /// </summary>
    /// <returns>
    /// The new <see cref="IDiagnosticReporter"/>.
    /// </returns>
    public IDiagnosticReporter GetNewDiagnosticReporter();
}
