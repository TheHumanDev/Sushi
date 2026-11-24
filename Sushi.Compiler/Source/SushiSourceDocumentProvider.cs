using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Lexing;
using Sushi.Core.Source;

namespace Sushi.Compiler.Source;

/// <summary>
/// The document provider for all sushi source files.
/// </summary>
/// <param name="keywordProvider">
/// The keyword provider used to discover registered keywords.
/// </param>
public sealed class SushiSourceDocumentProvider([NotNull] IKeywordProvider keywordProvider) : ISourceDocumentProvider
{
    /// <inheritdoc />
    public IReadOnlyList<ISourceDocumentType> DocumentTypes { get; } = new ReadOnlyCollection<ISourceDocumentType>([
       new SushiSourceDocumentType(keywordProvider),
       new SushiProjectDocumentType(),
       new SushiSolutionDocumentType()
    ]);
}
