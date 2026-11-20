using System.Diagnostics.CodeAnalysis;

namespace Sushi.LanguageServer.Workspace;

/// <summary>
/// Represents a versioned input for workspace analysis.
/// </summary>
internal sealed record WorkspaceAnalysisInput
{
    /// <summary>
    /// The generation number of the workspace analysis.
    /// </summary>
    public long Generation { get; }

    /// <summary>
    /// The document snapshots participating in workspace analysis.
    /// </summary>
    public IReadOnlyList<DocumentSnapshot> Documents { get; }

    /// <summary>
    /// Creates a new instance of <see cref="WorkspaceAnalysisInput" />.
    /// </summary>
    /// <param name="generation">
    /// The generation number of the workspace analysis.
    /// </param>
    /// <param name="documents">
    /// The document snapshots participating in workspace analysis.
    /// </param>
    internal WorkspaceAnalysisInput(long generation, [NotNull] IReadOnlyList<DocumentSnapshot> documents)
    {
        this.Generation = generation;

        this.Documents = documents;
    }
}
