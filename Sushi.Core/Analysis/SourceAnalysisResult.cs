using Sushi.Core.Parsing.Syntax;
using Sushi.Core.Semantics;
using Sushi.Core.Source;

namespace Sushi.Core.Analysis;

/// <summary>
/// Represents an analysis produced for a specific source snapshot.
/// </summary>
/// <param name="Snapshot">
/// The snapshot that was analyzed.
/// </param>
/// <param name="Tree">
/// The concrete syntax tree produced from the analyzed source.
/// </param>
public record SourceAnalysisResult(SourceSnapshot Snapshot, IConcreteSyntaxTree Tree) : AnalysisResult(Snapshot)
{
    /// <summary>
    /// The project containing this source file when membership is unambiguous.
    /// Null indicates standalone source, invalid project configuration, or
    /// membership in multiple projects.
    /// </summary>
    public Uri? ProjectUri { get; init; }

    /// <summary>
    /// Gets the successfully bound activity declarations in this source file.
    /// </summary>
    public IReadOnlyList<BoundActivity> Activities { get; init; } = [];
}
