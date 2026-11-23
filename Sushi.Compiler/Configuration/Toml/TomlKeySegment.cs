using Sushi.Core.Source;

/// <summary>
/// Represents a Toml key segment.
/// </summary>
/// <param name="Name">
/// The name of the key segment.
/// </param>
/// <param name="Span">
/// The Sushi <see cref="SourceSpan" />.
/// </param>
internal readonly record struct TomlKeySegment(string Name, SourceSpan Span);
