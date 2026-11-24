using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Analysis;
using Sushi.Core.Configuration;

namespace Sushi.LanguageServer.Workspace;

/// <summary>
/// Associates source analyses with the projects that explicitly include them.
/// Does not perform cross-file semantic binding.
/// </summary>
public sealed class WorkspaceProjectMembership([NotNull] IProjectSourceLoader projectSourceLoader)
{
    /// <summary>
    /// Associates each source with its containing project when exactly one
    /// valid project includes its file.
    /// </summary>
    /// <param name="analyses">
    /// The document analyses belonging to one captured workspace generation.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancels project discovery and source association.
    /// </param>
    /// <returns>
    /// Analyses in their original order with unambiguous project membership
    /// attached to source analysis results.
    /// </returns>
    internal IReadOnlyList<AnalysisResult> AssociateSources(IReadOnlyList<AnalysisResult> analyses, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(analyses);
        cancellationToken.ThrowIfCancellationRequested();

        List<(Uri ProjectUri, IReadOnlyList<string> SourcePaths)> projects = [];

        foreach (ProjectAnalysisResult analysis in analyses.OfType<ProjectAnalysisResult>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (analysis.Project is null)
            {
                continue;
            }

            string? directory = Path.GetDirectoryName(analysis.Snapshot.Uri.LocalPath);

            if (directory is null || !Directory.Exists(directory))
            {
                continue;
            }

            IReadOnlyList<string> paths = projectSourceLoader.DiscoverSourcePaths(directory, analysis.Project, cancellationToken);
            projects.Add((analysis.Snapshot.Uri, paths));
        }

        List<AnalysisResult> associated = [];

        foreach (AnalysisResult analysis in analyses)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (analysis is not SourceAnalysisResult source)
            {
                associated.Add(analysis);
                continue;
            }

            Uri? projectUri = null;
            bool ambiguous = false;

            foreach ((Uri candidateUri, IReadOnlyList<string> sourcePaths) in projects)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!sourcePaths.Any(source.Snapshot.Uri.IsSamePath))
                {
                    continue;
                }

                if (projectUri is not null)
                {
                    ambiguous = true;
                    break;
                }

                projectUri = candidateUri;
            }

            associated.Add(source with { ProjectUri = ambiguous ? null : projectUri });
        }

        return associated;
    }
}
