using System.Collections.ObjectModel;
using Sushi.Core.Source;

namespace Sushi.Compiler.Configuration.Toml;

/// <summary>
/// Represents a configuration array in Toml source.
/// </summary>
/// <param name="span">
/// The <see cref="SourceSpan" /> corresponding to the array.
/// </param>
public sealed class TomlConfigurationArray(SourceSpan span) : TomlConfigurationValue(span)
{
    /// <summary>
    /// The list of items in the array.
    /// </summary>
    private readonly List<TomlConfigurationValue> items = [];

    /// <summary>
    /// Gets the list of items in the Toml configuration array.
    /// </summary>
    public IReadOnlyList<TomlConfigurationValue> Items => new ReadOnlyCollection<TomlConfigurationValue>([..this.items]);

    /// <summary>
    /// Adds a configuration value to the array.
    /// </summary>
    /// <param name="value">
    /// The value to add.
    /// </param>
    public void Add(TomlConfigurationValue value) => this.items.Add(value);
}
