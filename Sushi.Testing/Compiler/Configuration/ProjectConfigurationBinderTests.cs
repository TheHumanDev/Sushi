using AwesomeAssertions;
using NUnit.Framework;
using Sushi.Compiler.Analysis;
using Sushi.Compiler.Configuration;
using Sushi.Compiler.Diagnostics;
using Sushi.Core.Diagnostics;
using Sushi.Core.Source;

namespace Sushi.Testing.Compiler.Configuration;

[TestFixture]
public class ProjectConfigurationBinderTests
{
    [TestCase(TestName = "Bind Should Produce Project Definition For Valid Project Identity")]
    public void BindShouldProduceProjectDefinitionForValidProjectIdentity()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text);

        diagnostics
            .Should()
            .BeEmpty();

        result.Project
            .Should()
            .BeEquivalentTo(CreateExpectedProject());
    }

    [TestCase(TestName = "Bind Should Accept Quoted And Escaped Root Keys")]
    public void BindShouldAccpetQuotedAndEscapedRootKeys()
    {
        const string text =
            """
            "na\u006de" = "Sushi Compiler"
            'assembly' = "Sushi.Compiler"
            "language-version" = "1.0"
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text);

        diagnostics
            .Should()
            .BeEmpty();

        result.Project
            .Should()
            .BeEquivalentTo(CreateExpectedProject());
    }

    [TestCase(TestName = "Bind Should Use Decoded String Values")]
    public void BindShouldUseDecodedStringValues()
    {
        const string text =
            """
            name = "Sushi\u0020Compiler"
            assembly = "Sushi\u002eCompiler"
            language-version = "1\u002e0"
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text);

        diagnostics
            .Should()
            .BeEmpty();

        result.Project
            .Should()
            .BeEquivalentTo(CreateExpectedProject());
    }

    [TestCase(TestName = "Bind Should Reject Non String Project Identity Values")]
    public void BindShouldRejectNonStringProjectIdentityValues()
    {
        const string text =
            """
            name = 42
            assembly = true
            language-version = 1.0
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text);

        result.Project
            .Should()
            .BeNull();

        diagnostics
            .Should()
            .HaveCount(3)
            .And
            .OnlyContain(diagnostic => diagnostic.IsType(ErrorType.TomlInvalidValueType));
    }

    [TestCase(TestName = "Bind Should Diagnose Missing Required Project Identity Keys")]
    public void BindShouldDiagnoseMissingRequiredProjectIdentityKeys()
    {
        const string text =
            """
            name = "Sushi Compiler"
            """;

        (SourceSnapshot snapshot, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text);

        result.Project
            .Should()
            .BeNull();

        diagnostics
            .Should()
            .HaveCount(2);

        diagnostics
            .Should()
            .OnlyContain(diagnostic => diagnostic.IsType(ErrorType.TomlMissingRequiredKey)
                && diagnostic.Span.Start == snapshot.Bytes.Length
                && diagnostic.Span.End == snapshot.Bytes.Length);
    }

    [TestCase(TestName = "Bind Should Reject Empty Project Name")]
    public void BindShouldRejectEmptyProjectName()
    {
        const string text =
            """
            name = ""
            assembly = "Sushi.Compiler"
            language-version = "1.0"
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text);

        result.Project
            .Should()
            .BeNull();

        diagnostics
            .Should()
            .ContainSingle()
            .And
            .OnlyContain(diagnostic => diagnostic.IsType(ErrorType.TomlEmptyProjectName));
    }

    [TestCase(TestName = "Bind Should Not Treat Dotted Keys As Required Root Keys")]
    public void BindShouldNotTreatDottedKeysAsRequiredRootKeys()
    {
        const string text =
            """
            name.value = "Sushi Compiler"
            assembly.value = "Sushi.Compiler"
            language-version.value = "1.0"
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text);

        result.Project
            .Should()
            .BeNull();

        diagnostics
            .Should()
            .HaveCount(3)
            .And
            .OnlyContain(diagnostic => diagnostic.IsType(ErrorType.TomlInvalidValueType));
    }

    [TestCase(TestName = "Bind Should Ignore Configuration Not Yet Implemented")]
    public void BindShouldIgnoreConfigurationNotYetImplemented()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"

            some-future-key = "ignored for now"

            [future]
            value = "ignored for now"
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text);

        diagnostics
            .Should()
            .BeEmpty();

        result.Project
            .Should()
            .BeEquivalentTo(CreateExpectedProject());
    }

    [TestCase(TestName = "Bind Should Place Invalid Value Diagnostic On The Value")]
    public void BindShouldPlaceInvalidValueDiagnosticOnTheValue()
    {
        const string text =
            """
            name = 42
            assembly = "Sushi.Compiler"
            language-version = "1.0"
            """;

        (SourceSnapshot snapshot, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text);

        result.Project
            .Should()
            .BeNull();
        
        SushiDiagnostic diagnostic = diagnostics
            .Should()
            .ContainSingle(diagnostic => diagnostic.IsType(ErrorType.TomlInvalidValueType))
            .Which;

        int valueStart = System.Text.Encoding.UTF8.GetByteCount(text[..text.IndexOf("42", StringComparison.Ordinal)]);

        diagnostic.Span.Snapshot.Should().BeSameAs(snapshot);
        diagnostic.Span.Start.Should().Be(valueStart);
        diagnostic.Span.Length.Should().Be(2);
    }

    [TestCase(TestName = "Bind Should Use Empty Source Definition When Source Table Is Missing")]
    public void BindShouldUseEmptySourceDefinitionWhenSourceTableIsMissing()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text);

        diagnostics
            .Should()
            .BeEmpty();

        result.Project
            .Should()
            .BeEquivalentTo(CreateExpectedProject());
    }

    [TestCase(TestName = "Bind Should Bind Source Default Namespace")]
    public void BindShouldBindSourceDefaultNamespace()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"

            [source]
            default-namespace = "Sushi.Compiler"
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text);

        diagnostics
            .Should()
            .BeEmpty();

        result.Project
            .Should()
            .BeEquivalentTo(
                CreateExpectedProject(
                    defaultNamespace: "Sushi.Compiler"));
    }

    [TestCase(TestName = "Bind Should Bind Source Exclusions")]
    public void BindShouldBindSourceExclusions()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"

            [source]
            exclude = [
                "Experimental/**",
                "Generated/Test.sus",
            ]
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text);

        diagnostics
            .Should()
            .BeEmpty();

        result.Project
            .Should()
            .BeEquivalentTo(
                CreateExpectedProject(
                    exclude:
                    [
                        "Experimental/**",
                        "Generated/Test.sus"
                    ]));
    }

    [TestCase(TestName = "Bind Should Decode Source String Values")]
    public void BindShouldDecodeSourceStringValues()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"

            ["source"]
            "default-namespace" = "Sushi\u002eCompiler"
            "exclude" = ["Generated\u002f**"]
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text);

        diagnostics
            .Should()
            .BeEmpty();

        result.Project
            .Should()
            .BeEquivalentTo(
                CreateExpectedProject(
                    defaultNamespace: "Sushi.Compiler",
                    exclude: ["Generated/**"]));
    }

    [TestCase(TestName = "Bind Should Reject Non Array Source Exclude")]
    public void BindShouldRejectNonArraySourceExclude()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"

            [source]
            exclude = "Generated/**"
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text);

        result.Project
            .Should()
            .BeNull();

        SushiDiagnostic diagnostic = diagnostics
            .Should()
            .ContainSingle(diagnostic => diagnostic.IsType(ErrorType.TomlInvalidValueType))
            .Which;

        diagnostic.Message
            .Should()
            .Contain("source.exclude");
    }

    [TestCase(TestName = "Bind Should Diagnose Invalid Source Exclude Element On The Element")]
    public void BindShouldDiagnoseInvalidSourceExcludeElementOnTheElement()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"

            [source]
            exclude = [
                "Experimental/**",
                42,
                "Generated/**",
            ]
            """;

        (SourceSnapshot snapshot, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text);

        result.Project
            .Should()
            .BeNull();

        diagnostics
            .Should()
            .ContainSingle()
            .And
            .OnlyContain(diagnostic => diagnostic.IsType(ErrorType.TomlInvalidValueType));

        SushiDiagnostic diagnostic = diagnostics.Single();

        int valueStart = System.Text.Encoding.UTF8.GetByteCount(text[..text.IndexOf("42", StringComparison.Ordinal)]);

        diagnostic.Span.Snapshot
            .Should()
            .BeSameAs(snapshot);

        diagnostic.Span.Start
            .Should()
            .Be(valueStart);
        diagnostic.Span.Length
            .Should()
            .Be(2);
    }

    [TestCase(TestName = "Bind Should Not Treat Nested Source Table As Source Configuration")]
    public void BindShouldNotTreatNestedSourceTableAsSourceConfiguration()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"
    
            [source.generated]
            default-namespace = "Wrong.Namespace"
            """;
    
        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text);
    
        diagnostics
            .Should()
            .BeEmpty();
    
        result.Project
            .Should()
            .BeEquivalentTo(CreateExpectedProject());
    }

    [TestCase(TestName = "Bind Should Accept Dotted Source Configuration")]
    public void BindShouldAcceptDottedSourceConfiguration()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"

            source.default-namespace = "Sushi.Compiler"
            source.exclude = ["Generated/**"]
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text);

        diagnostics
            .Should()
            .BeEmpty();

        result.Project
            .Should()
            .BeEquivalentTo(
                CreateExpectedProject(
                    defaultNamespace: "Sushi.Compiler",
                    exclude: ["Generated/**"]));
    }

    [TestCase(TestName = "Bind Should Accept Inline Source Configuration")]
    public void BindShouldAcceptInlineSourceConfiguration()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"
    
            source = {
                default-namespace = "Sushi.Compiler",
                exclude = ["Generated/**"],
            }
            """;
    
        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text);
    
        diagnostics
            .Should()
            .BeEmpty();
    
        result.Project
            .Should()
            .BeEquivalentTo(
                CreateExpectedProject(
                    defaultNamespace: "Sushi.Compiler",
                    exclude: ["Generated/**"]));
    }

    [TestCase(TestName = "Bind Should Not Duplicate Syntax Diagnostic For Invalid Required Value")]
    public void BindShouldNotDuplicateSyntaxDiagnosticForInvalidRequiredValue()
    {
        const string text =
            """
            name =
            assembly = "Sushi.Compiler"
            language-version = "1.0"

            [build]
            default = "debug"

            [build.targets.debug]
            type = "Sushi.Compiler.Build.DebugTarget"
            """;

        SourceSnapshot snapshot = CreateSnapshot(text);

        IDiagnosticReporter parserDiagnosticReporter = new DiagnosticReporter();

        TomlConfigurationParseResult parseResult = TomlConfigurationParser.Parse(snapshot, parserDiagnosticReporter, CancellationToken.None);

        IReadOnlyList<SushiDiagnostic> parserDiagnostics = parserDiagnosticReporter.ReportDiagnostics();

        parserDiagnostics
            .Should()
            .NotBeEmpty();

        parserDiagnostics
            .Should()
            .Contain(diagnostic => diagnostic.IsType(ErrorType.TomlSyntaxError));

        IDiagnosticReporter binderDiagnosticReporter = new DiagnosticReporter();

        ProjectConfigurationBindResult bindResult = ProjectConfigurationBinder.Bind(snapshot, binderDiagnosticReporter, parseResult.Document, CancellationToken.None);

        bindResult.Project
            .Should()
            .BeNull();

        binderDiagnosticReporter
            .ReportDiagnostics()
            .Should()
            .BeEmpty();
    }

    [TestCase(TestName = "Bind Should Require Build Configuration")]
    public void BindShouldRequireBuildConfiguration()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text, addValidBuild: false);

        result.Project
            .Should()
            .BeNull();

        diagnostics
            .Should()
            .ContainSingle(diagnostic =>
                diagnostic.IsType(ErrorType.TomlMissingRequiredKey)
                && diagnostic.Message.Contains("\"build\"", StringComparison.Ordinal));
    }

    [TestCase(TestName = "Bind Should Require At Least One Build Target")]
    public void BindShouldRequireAtLeastOneBuildTarget()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"

            [build]
            default = "debug"
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text, addValidBuild: false);

        result.Project
            .Should()
            .BeNull();

        diagnostics
            .Should()
            .Contain(diagnostic => diagnostic.IsType(ErrorType.TomlMissingBuildTargets));
    }

    [TestCase(TestName = "Bind Should Reject Unknown Default Build Target")]
    public void BindShouldRejectUnknownDefaultBuildTarget()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"

            [build]
            default = "release"

            [build.targets.debug]
            type = "Sushi.Compiler.Build.DebugTarget"
            """;

        (SourceSnapshot snapshot, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text, addValidBuild: false);

        result.Project
            .Should()
            .BeNull();

        SushiDiagnostic diagnostic = diagnostics
            .Should()
            .ContainSingle(diagnostic => diagnostic.IsType(ErrorType.TomlUnknownDefaultBuildTarget))
            .Which;

        diagnostic.Message
            .Should()
            .Contain("release");

        int valueStart = System.Text.Encoding.UTF8.GetByteCount(text[ ..text.IndexOf("\"release\"", StringComparison.Ordinal)]);

        diagnostic.Span.Snapshot
            .Should()
            .BeSameAs(snapshot);

        diagnostic.Span.Start
            .Should()
            .Be(valueStart);

        diagnostic.Span.Length
            .Should()
            .Be("\"release\"".Length);
    }

    [TestCase(TestName = "Bind Should Match Default Build Target Case Sensitively")]
    public void BindShouldMatchDefaultBuildTargetCaseSensitively()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"

            [build]
            default = "Debug"

            [build.targets.debug]
            type = "Sushi.Compiler.Build.DebugTarget"
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text, addValidBuild: false);

        result.Project
            .Should()
            .BeNull();

        diagnostics
            .Should()
            .ContainSingle(diagnostic => diagnostic.IsType(ErrorType.TomlUnknownDefaultBuildTarget) && diagnostic.Message.Contains("Debug", StringComparison.Ordinal));
    }

    [TestCase(TestName = "Bind Should Bind Build Configuration")]
    public void BindShouldBindBuildConfiguration()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"

            [build]
            default = "debug"
            sources = [
                "Build/Targets.sus",
                "Build/**/*.sus",
            ]

            [build.targets.debug]
            type = "Sushi.Compiler.Build.DebugTarget"

            [build.targets.release]
            type = "Sushi.Compiler.Build.ReleaseTarget"
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) = Bind(text, addValidBuild: false);

        diagnostics
            .Should()
            .BeEmpty();

        result.Project
            .Should()
            .BeEquivalentTo(CreateExpectedProject(build:
                new ProjectBuildDefinition(
                    "debug",
                    [
                        "Build/Targets.sus",
                        "Build/**/*.sus"
                    ],
                    new Dictionary<string, ProjectBuildTargetDefinition>(StringComparer.Ordinal)
                    {
                        ["debug"] = new("Sushi.Compiler.Build.DebugTarget"),
                        ["release"] = new("Sushi.Compiler.Build.ReleaseTarget")
                    })));
    }

    [TestCase(TestName = "Bind Should Fully Qualify Invalid Build Target Type Diagnostic")]
    public void BindShouldFullyQualifyInvalidBuildTargetTypeDiagnostic()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"

            [build]
            default = "debug"

            [build.targets.debug]
            type = 42
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) =
            Bind(text, addValidBuild: false);

        result.Project
            .Should()
            .BeNull();

        SushiDiagnostic diagnostic = diagnostics
            .Should()
            .ContainSingle(diagnostic => diagnostic.IsType(ErrorType.TomlInvalidValueType))
            .Which;

        diagnostic.Message
            .Should()
            .Contain("\"build.targets.debug.type\"");
    }

    [TestCase(TestName = "Bind Should Fully Qualify Invalid Build Target Diagnostic")]
    public void BindShouldFullyQualifyInvalidBuildTargetDiagnostic()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"

            [build]
            default = "debug"

            [build.targets]
            debug = 42
            """;

        (_, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) =
            Bind(text, addValidBuild: false);

        result.Project
            .Should()
            .BeNull();

        SushiDiagnostic diagnostic = diagnostics
            .Should()
            .ContainSingle(diagnostic => diagnostic.IsType(ErrorType.TomlInvalidValueType))
            .Which;

        diagnostic.Message
            .Should()
            .Contain("\"build.targets.debug\"");
    }

    [TestCase(TestName = "Bind Should Ignore Preexisting Caller Errors When Producing Project")]
    public void BindShouldIgnorePreexistingCallerErrorsWhenProducingProject()
    {
        const string text =
            """
            name = "Sushi Compiler"
            assembly = "Sushi.Compiler"
            language-version = "1.0"
    
            [build]
            default = "debug"
    
            [build.targets.debug]
            type = "Sushi.Compiler.Build.DebugTarget"
            """;
    
        SourceSnapshot snapshot = CreateSnapshot(text);
    
        IDiagnosticReporter parserDiagnosticReporter = new DiagnosticReporter();
        TomlConfigurationParseResult parseResult = TomlConfigurationParser.Parse(snapshot, parserDiagnosticReporter, CancellationToken.None);
    
        parserDiagnosticReporter
            .ReportDiagnostics()
            .Should()
            .BeEmpty();
    
        IDiagnosticReporter binderDiagnosticReporter = new DiagnosticReporter();
        binderDiagnosticReporter.GenerateError(ErrorType.ExpectedSyntax, new SourceSpan(snapshot, 0, 0));
    
        ProjectConfigurationBindResult result = ProjectConfigurationBinder.Bind(snapshot, binderDiagnosticReporter, parseResult.Document, CancellationToken.None);
    
        result.Project
            .Should()
            .BeEquivalentTo(CreateExpectedProject());
    
        binderDiagnosticReporter.ReportDiagnostics()
            .Should()
            .ContainSingle(diagnostic => diagnostic.IsType(ErrorType.ExpectedSyntax));
    }

    private static (SourceSnapshot snapshot, ProjectConfigurationBindResult result, IReadOnlyList<SushiDiagnostic> diagnostics) Bind(string text, bool addValidBuild = true)
    {
        if (addValidBuild)
        {
            text =
            $"""
            {text}

            {ValidBuildConfiguration}
            """;
        }

        SourceSnapshot snapshot = CreateSnapshot(text);

        IDiagnosticReporter parserDiagnosticReporter = new DiagnosticReporter();

        TomlConfigurationParseResult parseResult =
            TomlConfigurationParser.Parse(snapshot, parserDiagnosticReporter, CancellationToken.None);

        parserDiagnosticReporter.ReportDiagnostics()
            .Should()
            .BeEmpty("binder tests require syntactically valid TOML");

        IDiagnosticReporter binderDiagnosticReporter = new DiagnosticReporter();

        ProjectConfigurationBindResult result =
            ProjectConfigurationBinder.Bind(snapshot, binderDiagnosticReporter, parseResult.Document, CancellationToken.None);

        return (snapshot, result, binderDiagnosticReporter.ReportDiagnostics());
    }

    private static SourceSnapshot CreateSnapshot(string text)
    {
        return SourceSnapshot.FromText(
            new Uri("file:///TestProject/Test.susproj"),
            version: null,
            text);
    }

    private static ProjectDefinition CreateExpectedProject(string name = "Sushi Compiler", string assembly = "Sushi.Compiler", string languageVersion = "1.0", string? defaultNamespace = null, IReadOnlyList<string>? exclude = null, ProjectBuildDefinition? build = null)
    {
        ProjectSourceDefinition projectSource = new(defaultNamespace, exclude ?? []);
        ProjectBuildDefinition projectBuild = build ?? CreateValidBuildDefinition();

        return new(name, assembly, languageVersion, projectSource, projectBuild);
    }

    private static ProjectBuildDefinition CreateValidBuildDefinition()
    {
        return new ProjectBuildDefinition(
            "debug",
            [],
            new Dictionary<string, ProjectBuildTargetDefinition>(StringComparer.Ordinal)
            {
                ["debug"] = new ProjectBuildTargetDefinition("Sushi.Compiler.Build.DebugTarget")
            });
    }

    private const string ValidBuildConfiguration =
    """
    [build]
    default = "debug"

    [build.targets.debug]
    type = "Sushi.Compiler.Build.DebugTarget"
    """;
}
