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

    /// <inheritdoc />
    public Task<string> ResolveProjectPath(string projectOrFolderPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string path = Path.GetFullPath(projectOrFolderPath);

        if (File.Exists(path))
        {
            string extension = Path.GetExtension(path);

            // TODO: Incorporate this into the SourceDocumentProvider. Won't be able
            // to make agnostic until the solution file is properly implemented.
            if (extension.Equals(".susproj", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(path);
            }

            if (extension.Equals(".susln", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("Solution compilation is not yet supported. Compile a .susproj directly.");
            }

            throw new ArgumentException($"\"{path}\" is not a Sushi project file.", nameof(projectOrFolderPath));
        }

        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Project directory \"{path}\" does not exist.");
        }

        string[] projects = [.. Directory
            .EnumerateFiles(path, "*.susproj", SearchOption.TopDirectoryOnly)
            .OrderBy(project => project, StringComparer.Ordinal)];

        cancellationToken.ThrowIfCancellationRequested();
        return projects.Length switch
        {
            1 => Task.FromResult(projects[0]),
            0 => throw new InvalidOperationException($"Directory \"{path}\" does not contain a Sushi project file."),
            _ => throw new InvalidOperationException($"Directory \"{path}\" contains multiple Sushi project files. Specify the project to compile explicitly.")
        };
    }

    /// <inheritdoc />
    public async Task<SourceSnapshot> LoadSnapshot(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = Path.GetFullPath(path);
        byte[] bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);

        SourceSnapshot snapshot = SourceSnapshot.FromUTF8(new Uri(fullPath), version: null, bytes);
        cancellationToken.ThrowIfCancellationRequested();
        return snapshot;
    }
}
