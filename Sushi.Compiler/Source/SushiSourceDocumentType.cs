using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Analysis;
using Sushi.Core.Analysis;
using Sushi.Core.Diagnostics;
using Sushi.Core.Lexing;
using Sushi.Core.Source;

namespace Sushi.Compiler.Source;

/// <summary>
/// Represents a sushi source document, or .sus file.
/// </summary>
/// <param name="keywordProvider">
/// The keyword provider used to discover registered keywords.
/// </param>
public sealed class SushiSourceDocumentType([NotNull] IKeywordProvider keywordProvider) : ISourceDocumentType
{
    private readonly SourceAnalyzer analyzer = new(keywordProvider);

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
