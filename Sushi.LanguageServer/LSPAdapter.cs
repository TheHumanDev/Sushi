using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Sushi.Core.Diagnostics;
using Sushi.Core.Source;
using LSPRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using DiagnosticSeverity = Sushi.Core.Diagnostics.DiagnosticSeverity;
using OmniSharpDiagnosticSeverity = OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity;

namespace Sushi.LanguageServer;

/// <summary>
/// Handles conversion from Sushi classes to the native
/// classes that the LSP abstraction understands.
/// </summary>
internal static class LSPAdapter
{
    /// <summary>
    /// Converts a <see cref="SushiDiagnostic"/> to an LSP <see cref="Diagnostic"/>.
    /// </summary>
    /// <param name="diagnostic">
    /// The diagnostic to convert.
    /// </param>
    /// <param name="expectedSnapshot">
    /// The snapshot that this diagnostic is expected to originate from.
    /// </param>
    /// <returns>
    /// The new diagnostic.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the diagnostic belongs to a different source snapshot than the expected snapshot.
    /// </exception>
    internal static Diagnostic ToLSPDiagnostic(SushiDiagnostic diagnostic, SourceSnapshot expectedSnapshot)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        ArgumentNullException.ThrowIfNull(expectedSnapshot);

        SourceSpan span = diagnostic.Span;

        if (!ReferenceEquals(span.Snapshot, expectedSnapshot))
        {
            throw new InvalidOperationException("Diagnostic belongs to a different source snapshot.");
        }

        (int startLine, int startCharacter) = span.Snapshot.GetUtf16Position(span.Start);
        (int endLine, int endCharacter) = span.Snapshot.GetUtf16Position(span.End);

        return new Diagnostic
        {
            Code = diagnostic.DisplayName,
            Message = diagnostic.Message,
            Severity = ToLspSeverity(diagnostic.Severity),
            Range = new LSPRange(startLine, startCharacter, endLine, endCharacter),
            Source = "Sushi Compiler"
        };
    }

    /// <summary>
    /// Converts a <see cref="DiagnosticSeverity"/> to an LSP <see cref="OmniSharpDiagnosticSeverity"/>.
    /// </summary>
    /// <param name="severity">
    /// The diagnostic severity to convert.
    /// </param>
    /// <returns>
    /// The new diagnostic severity.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if a non-normative enum value was supplied.
    /// </exception>
    internal static OmniSharpDiagnosticSeverity ToLspSeverity(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Error => OmniSharpDiagnosticSeverity.Error,
            DiagnosticSeverity.Warning => OmniSharpDiagnosticSeverity.Warning,
            DiagnosticSeverity.Information => OmniSharpDiagnosticSeverity.Information,
            DiagnosticSeverity.Hint => OmniSharpDiagnosticSeverity.Hint,
            _ => throw new ArgumentOutOfRangeException(nameof(severity))
        };
    }
}
