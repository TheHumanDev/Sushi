using Sushi.Core.Analysis;

namespace Sushi.Compiler.Configuration;

/// <summary>
/// Represents the result of a project configuration binding.
/// </summary>
/// <param name="Project">
/// The emitted project definition.
/// </param>
public sealed record ProjectConfigurationBindResult(ProjectDefinition? Project);
