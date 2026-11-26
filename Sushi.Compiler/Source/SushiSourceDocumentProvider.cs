using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Parsing;
using Sushi.Core;
using Sushi.Core.Lexing;
using Sushi.Core.Source;

namespace Sushi.Compiler.Source;

/// <summary>
/// The document provider for all sushi source files.
/// </summary>
/// <param name="context">
/// The context used to link parts of the analysis process together.
/// </param>
public sealed class SushiSourceDocumentProvider([NotNull] ISushiContext<ParsingContext> context) : ISourceDocumentProvider
{
    /// <inheritdoc />
    public IReadOnlyList<ISourceDocumentType> DocumentTypes { get; } = new ReadOnlyCollection<ISourceDocumentType>([
       new SushiSourceDocumentType(context),
       new SushiProjectDocumentType(),
       new SushiSolutionDocumentType()
    ]);

    /// <inheritdoc />
    public string? GetLanguageIDFromExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        string normalizedExtension = extension.Replace(".", string.Empty, StringComparison.OrdinalIgnoreCase);

        return this.DocumentTypes.FirstOrDefault(doc => doc.Extension.Equals(normalizedExtension, StringComparison.OrdinalIgnoreCase))?.LanguageID;
    }
}
