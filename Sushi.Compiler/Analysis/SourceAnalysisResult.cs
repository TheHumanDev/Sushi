using Sushi.Core.Analysis;
using Sushi.Core.Source;

namespace Sushi.Compiler.Analysis;

/// <summary>
/// Represents an analysis produced for a specific source snapshot.
/// </summary>
/// <param name="Snapshot">
/// The snapshot that was analyzed.
/// </param>
public record SourceAnalysisResult(SourceSnapshot Snapshot) : AnalysisResult(Snapshot);
