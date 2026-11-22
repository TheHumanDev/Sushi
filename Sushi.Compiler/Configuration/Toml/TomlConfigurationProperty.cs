using Sushi.Core.Source;

namespace Sushi.Compiler.Configuration.Toml;

/// <summary>
/// Represents a Toml property.
/// </summary>
/// <param name="Name">
/// The name of the configuration property.
/// </param>
/// <param name="span">
/// The <see cref="SourceSpan" /> corresponding to the property.
/// </param>
/// <param name="Value">
/// The value of the configuration property.
/// </param>
public sealed record TomlConfigurationProperty(string Name, SourceSpan NameSpan, TomlConfigurationValue Value);
