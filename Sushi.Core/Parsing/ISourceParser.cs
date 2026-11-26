using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Diagnostics;

namespace Sushi.Core.Parsing;

/// <summary>
/// Represents the source parser, which is the top-level parser of a file.
/// </summary>
public interface ISourceParser<TContext> where TContext : IParsingContext
{
    /// <summary>
    /// Parses a stream of tokens into a syntax tree.
    /// </summary>
    /// <param name="context">
    /// The context to use for the parsing pass.
    /// </param>
    /// <param name="diagnosticReporter">
    /// The reporter used to accumulate diagnostics.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel parsing.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task" /> that returns the result.
    /// </returns>
    public Task<ParserResult> Parse([NotNull] TContext context, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken);
}
