using Sushi.Core.Source;

namespace Sushi.Compiler.Configuration.Toml;

/// <summary>
/// Represents a Toml configuration string.
/// </summary>
/// <param name="value">
/// The value of the string.
/// </param>
/// <param name="span">
/// The <see cref="SourceSpan" /> corresponding to the string.
/// </param>
public sealed class TomlConfigurationString(string value, SourceSpan span) : TomlConfigurationValue(span)
{
    public string Value { get; } = value;
}
