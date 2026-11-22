namespace Sushi.Compiler.Analysis;

/// <summary>
/// Represents a build target.
/// </summary>
/// <param name="Type">
/// The type of the build target.
/// </param>
public sealed record ProjectBuildTargetDefinition(string Type);
