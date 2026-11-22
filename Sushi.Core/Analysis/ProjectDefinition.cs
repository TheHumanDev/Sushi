namespace Sushi.Core.Analysis;

/// <summary>
/// Represents a project with its defined configuration, loaded from source.
/// </summary>
/// <param name="Name">
/// The friendly name of the project.
/// </param>
/// <param name="Assembly">
/// The assembly name.
/// </param>
/// <param name="LanguageVersion">
/// The Sushi language version.
/// </param>
/// <param name="Source">
/// The source section of the configuration.
/// </param>
/// <param name="Build">
/// The build section of the configuration.
/// </param>
public sealed record ProjectDefinition(string Name, string Assembly, string LanguageVersion, ProjectSourceDefinition Source, ProjectBuildDefinition Build);
