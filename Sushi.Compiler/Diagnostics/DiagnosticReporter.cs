using System.Collections.ObjectModel;
using Sushi.Core.Diagnostics;
using Sushi.Core.Source;

namespace Sushi.Compiler.Diagnostics;

/// <inheritdoc />
public sealed class DiagnosticReporter : IDiagnosticReporter
{
    private readonly List<SushiDiagnostic> diagnostics = [];

    private static readonly Dictionary<ErrorType, string> errorMessages = new()
    {
        [ErrorType.InvalidUtf8] = "Malformed UTF-8 source sequence.",
        [ErrorType.UnterminatedBlockComment] = "Unterminated block comment.",
        [ErrorType.TomlSyntaxError] = "Syntax error in Toml source.",
        [ErrorType.TomlMissingRequiredKey] = "Required configuration key is missing.",
        [ErrorType.TomlInvalidValueType] = "Configuration key has an invalid value type.",
        [ErrorType.TomlEmptyProjectName] = "Required project name is empty.",
        [ErrorType.TomlMissingBuildTargets] = "Project configuration must define at least one build target.",
        [ErrorType.TomlUnknownDefaultBuildTarget] = "Default build target is not declared in \"build.targets\".",
        [ErrorType.ExpectedSyntax] = "Syntax expected.",
        [ErrorType.MissingRootActivity] = "An executable Sushi project must declare exactly one root activity in its own assembly.",
        [ErrorType.DuplicateRootActivity] = "An executable Sushi project cannot declare multiple root activities.",
    };

    private static readonly Dictionary<WarningType, string> warningMessages = [];

    /// <inheritdoc />
    public bool HasErrors() => this.diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    /// <inheritdoc />
    public bool HasWarnings() => this.diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);

    /// <inheritdoc />
    public bool HasDiagnostics() => this.diagnostics.Count != 0;

    /// <inheritdoc />
    public IReadOnlyList<SushiDiagnostic> ReportDiagnostics() => new ReadOnlyCollection<SushiDiagnostic>([.. this.diagnostics]);

    /// <inheritdoc />
    public void GenerateError(ErrorType type, SourceSpan sourceSpan)
    {
        if (!errorMessages.TryGetValue(type, out string? message))
        {
            throw new InvalidOperationException($"ErrorType \"{Enum.GetName(type)}\" does not have an associated error message.");
        }

        this.GenerateErrorWithCustomMessage(type, message, sourceSpan);
    }

    /// <inheritdoc />
    public void GenerateErrorWithCustomMessage(ErrorType type, string message, SourceSpan sourceSpan)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentException($"Type value {(int)type} is invalid for enum ErrorType.");
        }

        this.diagnostics.Add(new SushiDiagnostic((int)type, message, DiagnosticSeverity.Error, sourceSpan));
    }

    /// <inheritdoc />
    public void GenerateWarning(WarningType type, SourceSpan sourceSpan)
    {
        if (!warningMessages.TryGetValue(type, out string? message))
        {
            throw new InvalidOperationException($"WarningType \"{Enum.GetName(type)}\" does not have an associated warning message.");
        }

        this.GenerateWarningWithCustomMessage(type, message, sourceSpan);
    }

    /// <inheritdoc />
    public void GenerateWarningWithCustomMessage(WarningType type, string message, SourceSpan sourceSpan)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentException($"Type value {(int)type} is invalid for enum WarningType.");
        }

        this.diagnostics.Add(new SushiDiagnostic((int)type, message, DiagnosticSeverity.Warning, sourceSpan));
    }

    /// <inheritdoc />
    public void CommitReporter(IDiagnosticReporter diagnosticReporter)
    {
        ArgumentNullException.ThrowIfNull(diagnosticReporter);

        foreach (SushiDiagnostic diagnostic in diagnosticReporter.ReportDiagnostics())
        {
            this.diagnostics.Add(diagnostic);
        }
    }
}
