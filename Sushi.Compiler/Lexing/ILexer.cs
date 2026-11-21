using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Diagnostics;
using Sushi.Core.Source;

namespace Sushi.Compiler.Lexing;

/// <summary>
/// Performs lexical analysis of text for a specific kind of document, such as a source file or project file.
/// </summary>
public interface ILexer
{
    /// <summary>
    /// Lexes the specified source document and returns the result.
    /// </summary>
    /// <param name="snapshot">
    /// The snapshot of the source document.
    /// </param>
    /// <param name="DiagnosticReporter">
    /// The root diagnostic reporter used to accumulate diagnostics
    /// produced during the pass.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel lexical analysis.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task" /> that returns the <see cref="LexResult"/>.
    /// </returns>
    public Task<LexResult> Lex([NotNull] SourceSnapshot snapshot, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken);
}
