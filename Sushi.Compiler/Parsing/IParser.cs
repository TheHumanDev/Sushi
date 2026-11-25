using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Diagnostics;

namespace Sushi.Compiler.Parsing;

/// <summary>
/// Represents a parser that handles a specific kind of syntax structure.
/// </summary>
public interface IParser<T>
{
    /// <summary>
    /// Parses a token stream, using a cursor to keep its current position.
    /// </summary>
    /// <param name="context">
    /// The parsing context which contains the available parsers and the cursor.
    /// </param>
    /// <param name="diagnosticReporter">
    /// The reporter used to accumulate diagnostics.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel parsing.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task" />.
    /// </returns>
    public Task<T> Parse([NotNull] ParsingContext context, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken);

    /// <summary>
    /// Returns whether the parser can recognize the next stream of tokens by looking ahead.
    /// </summary>
    /// <param name="context">
    /// The parsing context which contains the available parsers and the cursor.
    /// </param>
    /// <returns>
    /// True if the parser can start a parsing attempt. False otherwise.
    /// </returns>
    public bool CanStart([NotNull] ParsingContext context);
}
