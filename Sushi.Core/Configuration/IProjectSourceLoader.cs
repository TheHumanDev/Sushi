using Sushi.Core.Analysis;

namespace Sushi.Core.Configuration;

/// <summary>
/// Discovers source files included by a Sushi project configuration.
/// </summary>
public interface IProjectSourceLoader
{
    /// <summary>
    /// Discovers Sushi source files using the project's include and exclude
    /// patterns. Results are absolute paths in deterministic ordinal order.
    /// </summary>
    /// <param name="projectDirectory">
    /// The directory containing the project configuration file.
    /// </param>
    /// <param name="project">
    /// The successfully bound project configuration.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancels source discovery.
    /// </param>
    /// <returns>
    /// The source paths included by the project.
    /// </returns>
    public IReadOnlyList<string> DiscoverSourcePaths(string projectDirectory, ProjectDefinition project, CancellationToken cancellationToken);
}
