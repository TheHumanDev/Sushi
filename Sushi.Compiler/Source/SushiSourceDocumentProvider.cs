using System.Collections.ObjectModel;
using Sushi.Core.Source;

namespace Sushi.Compiler.Source;

/// <summary>
/// The document provider for all sushi source files.
/// </summary>
public sealed class SushiSourceDocumentProvider : ISourceDocumentProvider
{
    /// <inheritdoc />
    public IReadOnlyList<ISourceDocumentType> DocumentTypes { get; } = new ReadOnlyCollection<ISourceDocumentType>([
       new SushiSourceDocumentType()
    ]);
}
