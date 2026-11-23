using Sushi.Compiler.Configuration.Toml;
using Sushi.Core.Source;
using Sushi.Core.Diagnostics;
using Tomlyn.Parsing;
using Tomlyn.Syntax;

namespace Sushi.Compiler.Configuration;

/// <summary>
/// Handles parsing of a Toml configuration file.
/// </summary>
public sealed class TomlConfigurationParser
{
    /// <summary>
    /// Parses a source snapshot into a configuration.
    /// </summary>
    /// <param name="snapshot">
    /// The source snapshot.
    /// </param>
    /// <param name="diagnosticReporter">
    /// The reporter used to accumulate diagnostics.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel parsing.
    /// </param>
    /// <returns>
    /// The configuration.
    /// </returns>
    public static TomlConfigurationParseResult Parse(SourceSnapshot snapshot, IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(diagnosticReporter);

        cancellationToken.ThrowIfCancellationRequested();

        DocumentSyntax syntax = SyntaxParser.Parse(snapshot.Text, sourceName: snapshot.Uri.LocalPath, validate: true);

        cancellationToken.ThrowIfCancellationRequested();

        TomlConfigurationTable document = BuildDocument(snapshot, syntax, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        ConvertDiagnostics(snapshot, diagnosticReporter, syntax.Diagnostics);

        return new TomlConfigurationParseResult(snapshot, syntax, document);
    }

    /// <summary>
    /// Converts Toml diagnostics to Sushi diagnostics.
    /// </summary>
    /// <param name="snapshot">
    /// The source snapshot.
    /// </param>
    /// <param name="diagnosticReporter">
    /// The reporter used to accumulate diagnostics.
    /// </param>
    /// <param name="diagnostics">
    /// The diagnostics from Toml.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if any of the diagnostics are not errors or warnings.
    /// </exception>
    private static void ConvertDiagnostics(SourceSnapshot snapshot, IDiagnosticReporter diagnosticReporter, DiagnosticsBag diagnostics)
    {
        foreach (DiagnosticMessage diagnostic in diagnostics)
        {
            if (diagnostic.Kind is DiagnosticMessageKind.Error)
            {
                diagnosticReporter.GenerateErrorWithCustomMessage(ErrorType.TomlSyntaxError, diagnostic.Message, diagnostic.Span.ToSushiSpan(snapshot));
            }
            else if (diagnostic.Kind is DiagnosticMessageKind.Warning)
            {
                diagnosticReporter.GenerateWarningWithCustomMessage(WarningType.TomlSyntaxWarning, diagnostic.Message, diagnostic.Span.ToSushiSpan(snapshot));
            }
            else
            {
                throw new InvalidOperationException("Toml diagnostic kind must be either error or warning.");
            }
        }
    }

    /// <summary>
    /// Builds the table from the raw Toml document.
    /// </summary>
    /// <param name="snapshot">
    /// The source snapshot.
    /// </param>
    /// <param name="syntax">
    /// The raw Toml document syntax.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel parsing.
    /// </param>
    /// <returns>
    /// The parsed table.
    /// </returns>
    private static TomlConfigurationTable BuildDocument(SourceSnapshot snapshot, DocumentSyntax syntax, CancellationToken cancellationToken)
    {
        TomlConfigurationTable root = new(new Core.Source.SourceSpan(snapshot, 0, snapshot.Bytes.Length));

        foreach (KeyValueSyntax keyValue in syntax.KeyValues)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AddKeyValue(snapshot, root, keyValue, cancellationToken);
        }

        foreach (TableSyntaxBase tableSyntax in syntax.Tables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryGetKeyPath(snapshot, tableSyntax.Name, out List<TomlKeySegment> path))
            {
                continue;
            }

            switch (tableSyntax)
            {
                case TableSyntax table:
                {
                    TomlConfigurationTable? target = GetOrCreateTable(root, path, table.Span.ToSushiSpan(snapshot));

                    if (target is null)
                    {
                        continue;
                    }

                    foreach (KeyValueSyntax keyValue in table.Items)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        AddKeyValue(snapshot, target, keyValue, cancellationToken);
                    }

                    break;
                }

                case TableArraySyntax tableArray:
                {
                    TomlConfigurationTable? target = AddTableArrayElement(root, path, tableArray.Span.ToSushiSpan(snapshot));

                    if (target is null)
                    {
                        continue;
                    }

                    foreach (KeyValueSyntax keyValue in tableArray.Items)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        AddKeyValue(snapshot, target, keyValue, cancellationToken);
                    }

                    break;
                }
            }
        }

        return root;
    }

    /// <summary>
    /// Adds a key value pair to the table.
    /// </summary>
    /// <param name="snapshot">
    /// The source snapshot.
    /// </param>
    /// <param name="table">
    /// The table to add the key value pair to.
    /// </param>
    /// <param name="keyValue">
    /// The raw key value pair.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel parsing.
    /// </param>
    private static void AddKeyValue(SourceSnapshot snapshot, TomlConfigurationTable table, KeyValueSyntax keyValue, CancellationToken cancellationToken)
    {
        if (!TryGetKeyPath(snapshot, keyValue.Key, out List<TomlKeySegment> path) || path.Count == 0)
        {
            return;
        }

        TomlConfigurationTable current = table;

        for (int index = 0; index < path.Count - 1; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TomlKeySegment segment = path[index];

            TomlConfigurationTable? child = GetOrCreateChildTable(current, segment);

            if (child is null)
            {
                return;
            }

            current = child;
        }

        TomlKeySegment leaf = path[^1];

        TomlConfigurationValue value = ConvertValue(snapshot, keyValue.Value, keyValue.Span.ToSushiSpan(snapshot), cancellationToken);

        current.Add(new TomlConfigurationProperty(leaf.Name, leaf.Span, value));
    }

    /// <summary>
    /// Converts a raw value to a Toml configuration value.
    /// </summary>
    /// <param name="snapshot">
    /// The source snapshot.
    /// </param>
    /// <param name="value">
    /// The value to convert.
    /// </param>
    /// <param name="fallbackSpan">
    /// The span to fall back to if the value is null.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel parsing.
    /// </param>
    /// <returns>
    /// The parsed value.
    /// </returns>
    private static TomlConfigurationValue ConvertValue(SourceSnapshot snapshot, ValueSyntax? value, Core.Source.SourceSpan fallbackSpan, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (value is null)
        {
            return new TomlConfigurationInvalid(fallbackSpan);
        }

        Core.Source.SourceSpan span = value.Span.ToSushiSpan(snapshot);

        switch (value)
        {
            case StringValueSyntax stringValue:
                return new TomlConfigurationString(stringValue.Value ?? string.Empty, span);

            case ArraySyntax array:
            {
                TomlConfigurationArray result = new(span);

                foreach (ArrayItemSyntax item in array.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    result.Add(ConvertValue(snapshot, item.Value, item.Span.ToSushiSpan(snapshot), cancellationToken));
                }

                return result;
            }

            case InlineTableSyntax inlineTable:
            {
                TomlConfigurationTable result = new(span);

                foreach (InlineTableItemSyntax item in inlineTable.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (item.KeyValue is not null)
                    {
                        AddKeyValue(snapshot, result, item.KeyValue, cancellationToken);
                    }
                }

                return result;
            }

            default:
                return new TomlConfigurationScalar(span);
        }
    }

    /// <summary>
    /// Gets a currently existing table or creates a new one if it doesn't already exist.
    /// </summary>
    /// <param name="root">
    /// The root table.
    /// </param>
    /// <param name="path">
    /// The path to get the table for.
    /// </param>
    /// <param name="tableSpan">
    /// The span of the table.
    /// </param>
    /// <returns>
    /// The new or existing table.
    /// </returns>
    private static TomlConfigurationTable? GetOrCreateTable(TomlConfigurationTable root, IReadOnlyList<TomlKeySegment> path, Core.Source.SourceSpan tableSpan)
    {
        TomlConfigurationTable current = root;

        for (int index = 0; index < path.Count; index++)
        {
            TomlKeySegment segment = path[index];

            TomlConfigurationTable? child = GetOrCreateChildTable(current, segment);
            
            if (child is null)
            {
                return null;
            }

            current = child;
        }

        current.Span = tableSpan;

        return current;
    }

    /// <summary>
    /// Gets a currently existing child table or creates a new one if it doesn't already exist.
    /// </summary>
    /// <param name="table">
    /// The table.
    /// </param>
    /// <param name="segment">
    /// The key segment.
    /// </param>
    /// <returns>
    /// The new or existing table.
    /// </returns>
    private static TomlConfigurationTable? GetOrCreateChildTable(TomlConfigurationTable table, TomlKeySegment segment)
    {
        if (table.TryGetProperty(segment.Name, out TomlConfigurationProperty existing))
        {
            if (existing.Value is TomlConfigurationTable existingTable)
            {
                return existingTable;
            }

            if (existing.Value is TomlConfigurationArray array && array.Items.Count > 0 && array.Items[^1] is TomlConfigurationTable lastTable)
            {
                return lastTable;
            }

            return null;
        }

        TomlConfigurationTable child = new(segment.Span);

        table.Add(new TomlConfigurationProperty(segment.Name, segment.Span, child));

        return child;
    }

    /// <summary>
    /// Adds an array element to the table.
    /// </summary>
    /// <param name="root">
    /// The root table.
    /// </param>
    /// <param name="path">
    /// The path.
    /// </param>
    /// <param name="tableSpan">
    /// The span.
    /// </param>
    /// <returns>
    /// The new table.
    /// </returns>
    private static TomlConfigurationTable? AddTableArrayElement(TomlConfigurationTable root, IReadOnlyList<TomlKeySegment> path, Core.Source.SourceSpan tableSpan)
    {
        if (path.Count == 0)
        {
            return null;
        }

        TomlConfigurationTable current = root;

        for (int index = 0; index < path.Count - 1; index++)
        {
            TomlConfigurationTable? child = GetOrCreateChildTable(current, path[index]);

            if (child is null)
            {
                return null;
            }

            current = child;
        }

        TomlKeySegment leaf = path[^1];

        TomlConfigurationArray array;

        if (current.TryGetProperty(leaf.Name, out TomlConfigurationProperty existing))
        {
            if (existing.Value is not TomlConfigurationArray existingArray)
            {
                return null;
            }

            array = existingArray;
        }
        else
        {
            array = new TomlConfigurationArray(leaf.Span);

            current.Add(new TomlConfigurationProperty(leaf.Name, leaf.Span, array));
        }

        TomlConfigurationTable element = new(tableSpan);

        array.Add(element);

        return element;
    }

    /// <summary>
    /// Tries to get the path for a given key.
    /// </summary>
    /// <param name="snapshot">
    /// The source snapshot.
    /// </param>
    /// <param name="key">
    /// The key to get the path for.
    /// </param>
    /// <param name="path">
    /// The new path if successful.
    /// </param>
    /// <returns>
    /// True if the key path was successfully returned. False otherwise.
    /// </returns>
    private static bool TryGetKeyPath(SourceSnapshot snapshot, KeySyntax? key, out List<TomlKeySegment> path)
    {
        path = [];

        if (key?.Key is null)
        {
            return false;
        }

        if (!TryGetKeySegment(snapshot, key.Key, out TomlKeySegment first))
        {
            return false;
        }

        path.Add(first);

        foreach (DottedKeyItemSyntax dotted in key.DotKeys)
        {
            if (!TryGetKeySegment(snapshot, dotted.Key, out TomlKeySegment segment))
            {
                return false;
            }

            path.Add(segment);
        }

        return true;
    }

    /// <summary>
    /// Tries to get a key segment from the raw key.
    /// </summary>
    /// <param name="snapshot">
    /// The source snapshot.
    /// </param>
    /// <param name="key">
    /// The raw key.
    /// </param>
    /// <param name="segment">
    /// The new segment if successful.
    /// </param>
    /// <returns>
    /// True if the key segment was successfully parsed from the raw key. False otherwise.
    /// </returns>
    private static bool TryGetKeySegment(SourceSnapshot snapshot, BareKeyOrStringValueSyntax? key, out TomlKeySegment segment)
    {
        switch (key)
        {
            case BareKeySyntax bare
                when bare.Key?.Text is { } text:
                segment = new TomlKeySegment(text, bare.Span.ToSushiSpan(snapshot));

                return true;

            case StringValueSyntax quoted
                when quoted.Value is { } value:
                segment = new TomlKeySegment(value, quoted.Span.ToSushiSpan(snapshot));

                return true;

            default:
                segment = default;

                return false;
        }
    }
}
