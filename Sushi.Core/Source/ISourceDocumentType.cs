using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Analysis;
using Sushi.Core.Diagnostics;

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

    /// <summary>
    /// If true, changes to documents of this type invalidate the entire analysis cache.
    /// This is used for, say, a project file that might change the inclusion or exclusion
    /// of whole directories worth of source documents.
    /// </summary>
    public bool ChangesInvalidateEntireCache { get; }

    /// <summary>
    /// If true, this source document type can act as a compilation root. That means that
    /// it can be passed to the compiler as a command line argument. 
    /// </summary>
    public bool IsValidCompilationRoot { get; }

    /// <summary>
    /// If true, this document is considered a source code document.
    /// </summary>
    public bool IsSourceDocument { get; }
    
    /// <summary>
    /// The ID of the language that this document type is associated with.
    /// </summary>
    public string LanguageID { get; }

    /// <summary>
    /// Analyzes a source snapshot.
    /// </summary>
    /// <param name="snapshot">
    /// The authoritative source snapshot.
    /// </param>
    /// <param name="diagnosticReporter">
    /// Receives analysis diagnostics.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel analysis.
    /// </param>
    /// <returns>
    /// The analyzed syntax.
    /// </returns>
    public Task<AnalysisResult> Analyze([NotNull] SourceSnapshot snapshot, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken);
}
