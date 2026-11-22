using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.FileSystemGlobbing;
using Sushi.Core.Analysis;
using Sushi.Core.Configuration;
using Sushi.Core.Source;

namespace Sushi.Compiler.Configuration;

/// <inheritdoc />
public class ProjectSourceLoader([NotNull] ISourceDocumentProvider documentProvider) : IProjectSourceLoader
{
    /// <inheritdoc />
    public IReadOnlyList<string> DiscoverSourcePaths(string projectDirectory, ProjectDefinition project, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);
        ArgumentNullException.ThrowIfNull(project);

        cancellationToken.ThrowIfCancellationRequested();

        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        Matcher matcher = new(comparison);

        foreach (string pattern in project.Build.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            matcher.AddInclude(pattern);
        }

        foreach (string pattern in project.Source.Exclude)
        {
            cancellationToken.ThrowIfCancellationRequested();
            matcher.AddExclude(pattern);
        }

        string[] paths =
        [
            .. matcher
                .GetResultsInFullPath(projectDirectory)
                .Where(documentProvider.IsSourceDocument)
                .OrderBy(path => path, StringComparer.Ordinal)
        ];

        cancellationToken.ThrowIfCancellationRequested();

        return paths;
    }
}
