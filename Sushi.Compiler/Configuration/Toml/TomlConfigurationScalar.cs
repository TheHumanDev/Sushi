using Sushi.Core.Source;

namespace Sushi.Compiler.Configuration.Toml;

/// <summary>
/// Represents a Toml scalar.
/// </summary>
/// <param name="span">
/// The <see cref="SourceSpan" /> corresponding to the scalar.
/// </param>
public sealed class TomlConfigurationScalar(SourceSpan span) : TomlConfigurationValue(span);
