using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Analysis;
using Sushi.Core.Analysis;
using Sushi.Core.Diagnostics;
using Sushi.Core.Source;

namespace Sushi.Compiler.Source;

public sealed class SushiProjectDocumentType : ISourceDocumentType
{
    private readonly ProjectAnalyzer analyzer = new();

    /// <inheritdoc />
    public string Extension => "susproj";

    /// <inheritdoc />
    public bool ChangesInvalidateEntireCache => true;

    /// <inheritdoc />
    public bool IsValidCompilationRoot => true;

    /// <inheritdoc />
    public Task<AnalysisResult> Analyze([NotNull] SourceSnapshot snapshot, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
        => this.analyzer.Analyze(snapshot, diagnosticReporter, cancellationToken);
}
