using System.Collections.ObjectModel;
using Sushi.Core.Source;

namespace Sushi.Compiler.Source;

/// <summary>
/// Represents a sushi source document, or .sus file.
/// </summary>
public sealed class SushiSourceDocumentType : ISourceDocumentType
{
    /// <inheritdoc />
    public string Extension { get; } = "sus";
}
