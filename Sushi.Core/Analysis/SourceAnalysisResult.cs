using Sushi.Core.Source;

namespace Sushi.Core.Analysis;

/// <summary>
/// Represents an analysis produced for a specific source snapshot.
/// </summary>
/// <param name="Snapshot">
/// The snapshot that was analyzed.
/// </param>
public record SourceAnalysisResult(SourceSnapshot Snapshot) : AnalysisResult(Snapshot)
{
    /// <summary>
    /// The project containing this source file when membership is unambiguous.
    /// Null indicates standalone source, invalid project configuration, or
    /// membership in multiple projects.
    /// </summary>
    public Uri? ProjectUri { get; init; }
}
