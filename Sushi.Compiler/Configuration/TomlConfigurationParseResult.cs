using Sushi.Compiler.Configuration.Toml;
using Sushi.Core.Source;
using Tomlyn.Syntax;

namespace Sushi.Compiler.Configuration;

/// <summary>
/// Represents the result of a parsed Toml configuration.
/// </summary>
/// <param name="Snapshot">
/// The Sushi source snapshot.
/// </param>
/// <param name="Syntax">
/// The Toml document syntax.
/// </param>
/// <param name="Document">
/// The document as a native Toml table.
/// </param>
public sealed record TomlConfigurationParseResult(SourceSnapshot Snapshot, DocumentSyntax Syntax, TomlConfigurationTable Document);
