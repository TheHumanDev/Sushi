namespace Sushi.Core.Analysis;

/// <summary>
/// Represents the build configuration section of a sushi project file.
/// </summary>
/// <param name="DefaultTarget">
/// The default build target.
/// </param>
/// <param name="Sources">
/// Contains what source files (by glob or literal paths) are included in the project.
/// </param>
/// <param name="Targets">
/// The project targets.
/// </param>
public sealed record ProjectBuildDefinition(string? DefaultTarget, IReadOnlyList<string> Sources, IReadOnlyDictionary<string, ProjectBuildTargetDefinition> Targets);
