namespace Sushi.Core.Source;

/// <summary>
/// Represents a type of source document, and defines information such
/// as what file extension it expects.
/// </summary>
public interface ISourceDocumentType
{
    /// <summary>
    /// The extension associated with this document type.
    /// </summary>
    public string Extension { get; }
}
