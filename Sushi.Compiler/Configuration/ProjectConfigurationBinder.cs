using Sushi.Compiler.Configuration.Toml;
using Sushi.Compiler.Diagnostics;
using Sushi.Core.Analysis;
using Sushi.Core.Diagnostics;
using Sushi.Core.Source;

namespace Sushi.Compiler.Configuration;

/// <summary>
/// Handles binding of project configuration.
/// </summary>
public sealed class ProjectConfigurationBinder
{
    /// <summary>
    /// Binds the Toml source.
    /// </summary>
    /// <param name="snapshot">
    /// The source snapshot associated with the binding.
    /// </param>
    /// <param name="diagnosticReporter">
    /// The reporter used to accumulate diagnostics.
    /// </param>
    /// <param name="document">
    /// The Toml configuration table.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel binding.
    /// </param>
    /// <returns>
    /// The new <see cref="ProjectConfigurationBindResult" />.
    /// </returns>
    public static ProjectConfigurationBindResult Bind(SourceSnapshot snapshot, IDiagnosticReporter diagnosticReporter, TomlConfigurationTable document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(diagnosticReporter);
        ArgumentNullException.ThrowIfNull(document);

        cancellationToken.ThrowIfCancellationRequested();

        IDiagnosticReporter localDiagnostics = new DiagnosticReporter();

        string? name = BindRequiredString(snapshot, document, localDiagnostics, "name");
        string? assembly = BindRequiredString(snapshot, document, localDiagnostics, "assembly");

        string? languageVersion = BindRequiredString(snapshot, document, localDiagnostics, "language-version");

        if (name is { Length: 0 } && document.TryGetProperty("name", out TomlConfigurationProperty nameProperty))
        {
            localDiagnostics.GenerateError(ErrorType.TomlEmptyProjectName, nameProperty.Value.Span);
        }

        ProjectSourceDefinition source = BindSource(document, localDiagnostics, cancellationToken);

        ProjectBuildDefinition build = BindBuild(snapshot, document, localDiagnostics, cancellationToken);

        ProjectDefinition? project =
            !localDiagnostics.HasErrors()
            && name is not null
            && assembly is not null
            && languageVersion is not null
                ? new ProjectDefinition(name, assembly, languageVersion, source, build)
                : null;

        diagnosticReporter.CommitReporter(localDiagnostics);

        return new ProjectConfigurationBindResult(project);
    }

    /// <summary>
    /// Binds a required string.
    /// </summary>
    /// <param name="snapshot">
    /// The source snapshot.
    /// </param>
    /// <param name="table">
    /// The toml configuration table.
    /// </param>
    /// <param name="diagnosticReporter">
    /// The diagnostic reporter used to accumulate diagnostics.
    /// </param>
    /// <param name="propertyName">
    /// The name of the property.
    /// </param>
    /// <param name="diagnosticPropertyName">
    /// The property name for diagnostics.
    /// </param>
    /// <returns>
    /// The bound string.
    /// </returns>
    private static string? BindRequiredString(SourceSnapshot snapshot, TomlConfigurationTable table, IDiagnosticReporter diagnosticReporter, string propertyName, string diagnosticPropertyName = "")
    {
        if (string.IsNullOrWhiteSpace(diagnosticPropertyName))
        {
            diagnosticPropertyName = propertyName;
        }

        if (!table.TryGetProperty(propertyName, out TomlConfigurationProperty property))
        {
            AccumulateMissingKeyDiagnostic(snapshot, diagnosticReporter, diagnosticPropertyName);

            return null;
        }

        return BindStringValue(property, diagnosticPropertyName, diagnosticReporter);
    }

    /// <summary>
    /// Binds a string value.
    /// </summary>
    /// <param name="property">
    /// The Toml configuration property.
    /// </param>
    /// <param name="diagnosticName">
    /// The name of the diagnostic.
    /// </param>
    /// <param name="diagnosticReporter">
    /// The reporter used to accumulate diagnostics.
    /// </param>
    /// <returns>
    /// The bound string.
    /// </returns>
    private static string? BindStringValue(TomlConfigurationProperty property, string diagnosticName, IDiagnosticReporter diagnosticReporter)
    {
        if (property.Value is TomlConfigurationInvalid)
        {
            return null;
        }

        if (property.Value is TomlConfigurationString stringValue)
        {
            return stringValue.Value;
        }

        AccumulateInvalidValueTypeDiagnostic(diagnosticReporter, diagnosticName, "string", property);

        return null;
    }

    /// <summary>
    /// Binds a source definition.
    /// </summary>
    /// <param name="document">
    /// The Toml configuration table.
    /// </param>
    /// <param name="diagnosticReporter">
    /// The reporter used to accumulate diagnostics.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel binding.
    /// </param>
    /// <returns>
    /// The bound project source definition.
    /// </returns>
    private static ProjectSourceDefinition BindSource(TomlConfigurationTable document, IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!document.TryGetProperty("source", out TomlConfigurationProperty sourceProperty))
        {
            return new ProjectSourceDefinition(DefaultNamespace: null, Exclude: []);
        }

        if (sourceProperty.Value is TomlConfigurationInvalid)
        {
            return new ProjectSourceDefinition(DefaultNamespace: null, Exclude: []);
        }

        if (sourceProperty.Value is not TomlConfigurationTable sourceTable)
        {
            AccumulateInvalidValueTypeDiagnostic(diagnosticReporter, "source", "table", sourceProperty);

            return new ProjectSourceDefinition(DefaultNamespace: null, Exclude: []);
        }

        string? defaultNamespace = null;
        List<string> exclude = [];

        if (sourceTable.TryGetProperty("default-namespace", out TomlConfigurationProperty defaultNamespaceProperty))
        {
            defaultNamespace = BindStringValue(defaultNamespaceProperty, "source.default-namespace", diagnosticReporter);
        }

        if (sourceTable.TryGetProperty("exclude", out TomlConfigurationProperty excludeProperty))
        {
            exclude.AddRange(BindStringArray(excludeProperty, "source.exclude", diagnosticReporter, cancellationToken));
        }

        return new ProjectSourceDefinition(defaultNamespace, exclude);
    }

    /// <summary>
    /// Binds the build configuration.
    /// </summary>
    /// <param name="snapshot">
    /// The source snapshot.
    /// </param>
    /// <param name="document">
    /// The Toml configuration table.
    /// </param>
    /// <param name="diagnosticReporter">
    /// The reporter used to accumulate diagnostics.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel binding.
    /// </param>
    /// <returns>
    /// The bound project build definition.
    /// </returns>
    private static ProjectBuildDefinition BindBuild(SourceSnapshot snapshot, TomlConfigurationTable document, IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!document.TryGetProperty("build", out TomlConfigurationProperty buildProperty))
        {
            AccumulateMissingKeyDiagnostic(snapshot, diagnosticReporter, "build");

            return CreateEmptyBuild();
        }

        if (buildProperty.Value is TomlConfigurationInvalid)
        {
            return CreateEmptyBuild();
        }

        if (buildProperty.Value is not TomlConfigurationTable buildTable)
        {
            AccumulateInvalidValueTypeDiagnostic(diagnosticReporter, "build", "table", buildProperty);

            return CreateEmptyBuild();
        }

        string? defaultTarget = null;
        TomlConfigurationProperty? defaultProperty = null;
        List<string> sources = [];

        Dictionary<string, ProjectBuildTargetDefinition> targets = [with(StringComparer.Ordinal)];
        HashSet<string> declaredTargetNames = [with(StringComparer.Ordinal)];

        if (buildTable.TryGetProperty("default", out TomlConfigurationProperty foundDefaultProperty))
        {
            defaultProperty = foundDefaultProperty;

            defaultTarget = BindStringValue(foundDefaultProperty, "build.default", diagnosticReporter);
        }
        else
        {
            AccumulateMissingKeyDiagnostic(snapshot, diagnosticReporter, "build.default");
        }

        if (buildTable.TryGetProperty("sources", out TomlConfigurationProperty sourcesProperty))
        {
            sources.AddRange(BindStringArray(sourcesProperty, "build.sources", diagnosticReporter, cancellationToken));
        }

        if (buildTable.TryGetProperty("targets", out TomlConfigurationProperty targetsProperty))
        {
            BindBuildTargets(snapshot, targetsProperty, targets, declaredTargetNames, diagnosticReporter, cancellationToken);
        }

        if (declaredTargetNames.Count == 0)
        {
            diagnosticReporter.GenerateError(ErrorType.TomlMissingBuildTargets, buildTable.Span);
        }

        if (defaultTarget is not null && defaultProperty is not null && !declaredTargetNames.Contains(defaultTarget))
        {
            diagnosticReporter.GenerateErrorWithCustomMessage(ErrorType.TomlUnknownDefaultBuildTarget, $"Default build target \"{defaultTarget}\" is not declared in \"build.targets\".", defaultProperty.Value.Span);
        }

        return new ProjectBuildDefinition(defaultTarget, sources, targets);
    }

    /// <summary>
    /// Binds the build targets configuration. 
    /// </summary>
    /// <param name="snapshot">
    /// The source snapshot.
    /// </param>
    /// <param name="targetsProperty">
    /// The Toml targets property.
    /// </param>
    /// <param name="targets">
    /// The Toml dictionary of targets.
    /// </param>
    /// <param name="declaredTargetNames">
    /// The declared target names.
    /// </param>
    /// <param name="diagnosticReporter">
    /// The reporter used to accumulate diagnostics.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel binding.
    /// </param>
    private static void BindBuildTargets(SourceSnapshot snapshot, TomlConfigurationProperty targetsProperty, Dictionary<string, ProjectBuildTargetDefinition> targets, HashSet<string> declaredTargetNames, IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (targetsProperty.Value is TomlConfigurationInvalid)
        {
            return;
        }

        if (targetsProperty.Value is not TomlConfigurationTable targetsTable)
        {
            AccumulateInvalidValueTypeDiagnostic(diagnosticReporter, "build.targets", "table", targetsProperty);

            return;
        }

        foreach (TomlConfigurationProperty targetProperty in targetsTable.Properties.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();

            declaredTargetNames.Add(targetProperty.Name);

            if (targetProperty.Value is TomlConfigurationInvalid)
            {
                continue;
            }

            if (targetProperty.Value is not TomlConfigurationTable targetTable)
            {
                AccumulateInvalidValueTypeDiagnostic(diagnosticReporter, $"build.targets.{targetProperty.Name}", "table", targetProperty);

                continue;
            }

            string? type = BindRequiredString(snapshot, targetTable, diagnosticReporter, "type", $"build.targets.{targetProperty.Name}.type");

            if (type is null)
            {
                continue;
            }

            targets.TryAdd(targetProperty.Name, new ProjectBuildTargetDefinition(type));
        }
    }

    /// <summary>
    /// Binds a string array.
    /// </summary>
    /// <param name="property">
    /// The Toml property.
    /// </param>
    /// <param name="diagnosticName">
    /// The diagnostic name of the property.
    /// </param>
    /// <param name="diagnosticReporter">
    /// The reporter used to accumulate diagnostics.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel binding.
    /// </param>
    /// <returns>
    /// The bound string list.
    /// </returns>
    private static IReadOnlyList<string> BindStringArray(TomlConfigurationProperty property, string diagnosticName, IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
    {
        if (property.Value is TomlConfigurationInvalid)
        {
            return [];
        }

        if (property.Value is not TomlConfigurationArray array)
        {
            AccumulateInvalidValueTypeDiagnostic(diagnosticReporter, diagnosticName, "string array", property);

            return [];
        }

        List<string> values = [];

        foreach (TomlConfigurationValue item in array.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (item is TomlConfigurationString stringValue)
            {
                values.Add(stringValue.Value);

                continue;
            }

            diagnosticReporter.GenerateErrorWithCustomMessage(ErrorType.TomlInvalidValueType, $"Project configuration key \"{diagnosticName}\" must contain only strings.", item.Span);
        }

        return values;
    }

    /// <summary>
    /// Creates an empty build definition.
    /// </summary>
    /// <returns>
    /// The new build definition.
    /// </returns>
    private static ProjectBuildDefinition CreateEmptyBuild() => new(DefaultTarget: null, Sources: [], Targets: new Dictionary<string, ProjectBuildTargetDefinition>(StringComparer.Ordinal));

    /// <summary>
    /// Accumulates a missing required key diagnostic into the diagnostic reporter.
    /// </summary>
    /// <param name="snapshot">
    /// The source snapshot.
    /// </param>
    /// <param name="diagnosticReporter">
    /// The reporter used to accumulate diagnostics.
    /// </param>
    /// <param name="keyName">
    /// The name of the required key.
    /// </param>
    private static void AccumulateMissingKeyDiagnostic(SourceSnapshot snapshot, IDiagnosticReporter diagnosticReporter, string keyName)
    {
        int eof = snapshot.Bytes.Length;
        diagnosticReporter.GenerateErrorWithCustomMessage(ErrorType.TomlMissingRequiredKey, $"Required project configuration key \"{keyName}\" is missing.", new SourceSpan(snapshot, eof, eof));
    }

    /// <summary>
    /// Accumulates an invalid value type diagnostic into the diagnostic reporter.
    /// </summary>
    /// <param name="diagnosticReporter">
    /// The reporter used to accumulate diagnostics.
    /// </param>
    /// <param name="propertyName">
    /// The property name.
    /// </param>
    /// <param name="typeName">
    /// The name of the expected type.
    /// </param>
    /// <param name="sourceProperty">
    /// The Toml source property.
    /// </param>
    private static void AccumulateInvalidValueTypeDiagnostic(IDiagnosticReporter diagnosticReporter, string propertyName, string typeName, TomlConfigurationProperty sourceProperty)
        => diagnosticReporter.GenerateErrorWithCustomMessage(
            ErrorType.TomlInvalidValueType,
            $"Project configuration key \"{propertyName}\" must be a {typeName}.",
            sourceProperty.Value.Span);
}
