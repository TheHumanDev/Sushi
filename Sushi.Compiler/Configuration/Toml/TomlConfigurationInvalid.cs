using Sushi.Core.Source;

namespace Sushi.Compiler.Configuration.Toml;

/// <summary>
/// Represents an invalid value in Toml source.
/// </summary>
/// <param name="span">
/// The <see cref="SourceSpan" /> corresponding to the invalid config.
/// </param>
public sealed class TomlConfigurationInvalid(SourceSpan span) : TomlConfigurationValue(span);
