using Sushi.Core.Diagnostics;

namespace Sushi.Compiler.Diagnostics;

/// <inheritdoc />
public sealed class DiagnosticReporterFactory : IDiagnosticReporterFactory
{
    /// <inheritdoc />
    public IDiagnosticReporter GetNewDiagnosticReporter() => new DiagnosticReporter();
}
