using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Analysis;
using Sushi.Compiler.Parsing;
using Sushi.Core;
using Sushi.Core.Analysis;
using Sushi.Core.Diagnostics;
using Sushi.Core.Source;

namespace Sushi.Compiler.Source;

/// <summary>
/// Represents a sushi source document, or .sus file.
/// </summary>
public sealed class SushiSourceDocumentType : ISourceDocumentType
{
    private readonly SourceAnalyzer analyzer;

    /// <summary>
    /// Creates a new instance of <see cref="SushiSourceDocumentType" />.
    /// </summary>
    /// <param name="context">
    /// The context used to link parts of the analysis process together.
    /// </param>
    public SushiSourceDocumentType([NotNull] ISushiContext<ParsingContext> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this.analyzer = new(context);
    }

    /// <inheritdoc />
    public string Extension { get; } = "sus";

    /// <inheritdoc />
    public bool ChangesInvalidateEntireCache => false;

    /// <inheritdoc />
    public bool IsValidCompilationRoot => false;

    /// <inheritdoc />
    public bool IsSourceDocument => true;

    /// <inheritdoc />
    public string LanguageID => "sushi";

    /// <inheritdoc />
    public Task<AnalysisResult> Analyze([NotNull] SourceSnapshot snapshot, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
        => this.analyzer.Analyze(snapshot, diagnosticReporter, cancellationToken);
}
