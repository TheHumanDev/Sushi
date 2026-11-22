namespace Sushi.Core.Analysis;

/// <summary>
/// Represents the source configuration section of a sushi project file.
/// </summary>
/// <param name="DefaultNamespace">
/// The default namespace.
/// </param>
/// <param name="Exclude">
/// Contains what files to exclude.
/// </param>
public sealed record ProjectSourceDefinition(string? DefaultNamespace, IReadOnlyList<string> Exclude);
