using Sushi.Core.Source;

namespace Sushi.Compiler.Configuration.Toml;

/// <summary>
/// Represents a Toml value.
/// </summary>
/// <param name="span">
/// The <see cref="SourceSpan" /> corresponding to the value.
/// </param>
public abstract class TomlConfigurationValue(SourceSpan span)
{
    /// <summary>
    /// The <see cref="SourceSpan" /> corresponding to the scalar.
    /// </summary>
    public SourceSpan Span { get; internal set; } = span;
}
