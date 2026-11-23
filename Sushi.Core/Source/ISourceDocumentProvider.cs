using System.Collections.ObjectModel;

namespace Sushi.Core.Source;

/// <summary>
/// Represents a handler that provides source document information such as registered
/// source document types.
/// </summary>
public interface ISourceDocumentProvider
{
    /// <summary>
    /// The available document types registered with the provider.
    /// </summary>
    public IReadOnlyList<ISourceDocumentType> DocumentTypes { get; }

    /// <summary>
    /// The extensions registered in this document provider.
    /// </summary>
    public IReadOnlyList<string> RegisteredExtensions => new ReadOnlyCollection<string>([.. this.DocumentTypes.Select(x => x.Extension)]);
}
