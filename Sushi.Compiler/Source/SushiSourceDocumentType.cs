using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Analysis;
using Sushi.Core.Analysis;
using Sushi.Core.Diagnostics;
using Sushi.Core.Source;

namespace Sushi.Compiler.Source;

/// <summary>
/// Represents a sushi source document, or .sus file.
/// </summary>
public sealed class SushiSourceDocumentType : ISourceDocumentType
{
    private readonly SourceAnalyzer analyzer = new();

    /// <inheritdoc />
    public string Extension { get; } = "sus";

    /// <inheritdoc />
    public bool ChangesInvalidateEntireCache => false;

    /// <inheritdoc />
    public bool IsValidCompilationRoot => false;

    /// <inheritdoc />
    public Task<AnalysisResult> Analyze([NotNull] SourceSnapshot snapshot, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
        => this.analyzer.Analyze(snapshot, diagnosticReporter, cancellationToken);
}
