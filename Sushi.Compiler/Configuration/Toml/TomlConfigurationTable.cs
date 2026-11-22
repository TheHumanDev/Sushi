using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Source;

namespace Sushi.Compiler.Configuration.Toml;

/// <summary>
/// Represents a Toml configuration string.
/// </summary>
/// <param name="span">
/// The <see cref="SourceSpan" /> corresponding to the table.
/// </param>
public sealed class TomlConfigurationTable(SourceSpan span) : TomlConfigurationValue(span)
{
    /// <summary>
    /// The properties in the table.
    /// </summary>
    private readonly Dictionary<string, TomlConfigurationProperty> properties = [with(StringComparer.Ordinal)];

    /// <summary>
    /// Gets the properties in the table.
    /// </summary>
    public IReadOnlyDictionary<string, TomlConfigurationProperty> Properties => this.properties;

    /// <summary>
    /// Adds a new value to the configuration table.
    /// </summary>
    /// <param name="property">
    /// The property to add.
    /// </param>
    public void Add([NotNull] TomlConfigurationProperty property) => this.properties.TryAdd(property.Name, property);

    /// <summary>
    /// Attempts to retrieve an existing configuration property.
    /// </summary>
    /// <param name="name">
    /// The name of the property.
    /// </param>
    /// <param name="property">
    /// The resulting property.
    /// </param>
    /// <returns>
    /// True if the attempt succeeded. False otherwise.
    /// </returns>
    public bool TryGetProperty(string name, out TomlConfigurationProperty property) => this.properties.TryGetValue(name, out property!);
}
