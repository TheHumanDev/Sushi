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

    /// <summary>
    /// Returns whether the specified path belongs to a source document, such as a .sus file.
    /// </summary>
    /// <param name="path">
    /// The path of the document.
    /// </param>
    /// <returns>
    /// True if the path points to a valid source document. False otherwise. Note that this
    /// does not determine whether the contents of the document are expected of a valid source
    /// file.
    /// </returns>
    public bool IsSourceDocument(string path)
    {
        string extension = Path.GetExtension(path).Replace(".", string.Empty, StringComparison.OrdinalIgnoreCase);

        ISourceDocumentType? expectedType = this.DocumentTypes.FirstOrDefault(doc => doc.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase));

        if (expectedType is null)
        {
            return false;
        }

        return expectedType.IsSourceDocument;
    }
}
