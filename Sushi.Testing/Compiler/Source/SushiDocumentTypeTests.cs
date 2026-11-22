using AwesomeAssertions;
using NUnit.Framework;
using Sushi.Compiler.Analysis;
using Sushi.Compiler.Source;
using Sushi.Core.Analysis;
using Sushi.Core.Source;
using AwesomeAssertions.Execution;
using Sushi.Core.Diagnostics;
using Sushi.Compiler.Diagnostics;

namespace Sushi.Testing.Compiler.Source;

[TestFixture]
public sealed class SushiDocumentTypeTests
{
    [TestCase("sus", typeof(SushiSourceDocumentType), false, false, TestName = "Sushi Source Document Type Should Have Expected Metadata")]
    [TestCase("susproj", typeof(SushiProjectDocumentType), true, true, TestName = "Sushi Project Document Type Should Have Expected Metadata")]
    [TestCase("susln", typeof(SushiSolutionDocumentType), true, true, TestName = "Sushi Solution Document Type Should Have Expected Metadata")]
    public void RegisteredDocumentTypesShouldHaveExpectedMetadata(string extension, Type expectedType, bool compilationRoot, bool invalidatesEntireCache)
    {
        ISourceDocumentProvider provider = new SushiSourceDocumentProvider();

        ISourceDocumentType documentType = provider.DocumentTypes.Single(type => type.Extension == extension);

        documentType
            .GetType()
            .Should()
            .Be(expectedType);

        documentType
            .IsValidCompilationRoot
            .Should()
            .Be(compilationRoot);

        documentType.ChangesInvalidateEntireCache
            .Should()
            .Be(invalidatesEntireCache);

        provider.RegisteredExtensions
            .Should()
            .Contain(extension);
    }

    [TestCase(TestName = "Registered Document Extensions Must Be Unique")]
    public void RegisteredDocumentExtensionsMustBeUnique()
    {
        ISourceDocumentProvider provider = new SushiSourceDocumentProvider();

        IReadOnlyList<string> extensions = provider.RegisteredExtensions;
        int uniqueCount = extensions.Distinct(StringComparer.OrdinalIgnoreCase).Count();

        extensions
            .Should()
            .HaveCount(3);

        uniqueCount
            .Should()
            .Be(extensions.Count);

        extensions
            .Should()
            .BeEquivalentTo(["sus", "susproj", "susln"]);
    }

    [TestCase(TestName = "Project Document Analysis Should Produce Bound Project For Valid TOML")]
    public async Task ProjectDocumentAnalysisShouldProduceBoundProjectForValidTOML()
    {
        ISourceDocumentType documentType = GetDocumentType("susproj");
        SourceSnapshot snapshot = CreateSnapshot("susproj", ValidProjectConfiguration);
        IDiagnosticReporter reporter = new DiagnosticReporter();

        AnalysisResult analysis = await documentType.Analyze(snapshot, reporter, CancellationToken.None);

        ProjectAnalysisResult result = analysis
            .Should()
            .BeOfType<ProjectAnalysisResult>()
            .Which;

        result.Snapshot
            .Should()
            .BeSameAs(snapshot);

        result.Project
            .Should()
            .NotBeNull();

        result.Project!.Name
            .Should()
            .Be("Sushi Compiler");

        result.Project.Assembly
            .Should()
            .Be("Sushi.Compiler");

        result.Project.Build.DefaultTarget
            .Should()
            .Be("debug");

        reporter.ReportDiagnostics()
            .Should()
            .BeEmpty();
    }

    [TestCase(TestName = "Solution Document Analysis Should Accept Valid TOML")]
    public async Task SolutionDocumentAnalysisShouldAcceptValidTOML()
    {
        ISourceDocumentType documentType = GetDocumentType("susln");
        SourceSnapshot snapshot = CreateSnapshot("susln", "name = \"Sushi Solution\"");
        IDiagnosticReporter reporter = new DiagnosticReporter();

        AnalysisResult analysis = await documentType.Analyze(snapshot, reporter, CancellationToken.None);

        SolutionAnalysisResult result = analysis
            .Should()
            .BeOfType<SolutionAnalysisResult>().Which;

        result.Snapshot
            .Should()
            .BeSameAs(snapshot);

        result.Solution
            .Should()
            .NotBeNull();

        reporter.ReportDiagnostics()
            .Should()
            .BeEmpty();
    }

    [TestCase("susproj", TestName = "Invalid Project TOML Should Not Produce Configuration Definition")]
    [TestCase("susln", TestName = "Invalid Solution TOML Should Not Produce Configuration Definition")]
    public async Task InvalidTOMLShouldNotProduceConfigurationDefinition(string extension)
    {
        ISourceDocumentType documentType = GetDocumentType(extension);
        SourceSnapshot snapshot = CreateSnapshot(extension, "broken = @");
        IDiagnosticReporter reporter = new DiagnosticReporter();

        AnalysisResult result = await documentType.Analyze(snapshot, reporter, CancellationToken.None);
        IReadOnlyList<SushiDiagnostic> diagnostics = reporter.ReportDiagnostics();

        AssertNoConfigurationDefinition(result);

        result.Snapshot
            .Should()
            .BeSameAs(snapshot);

        diagnostics
            .Should()
            .Contain(diagnostic => diagnostic.IsType(ErrorType.TomlSyntaxError));

        diagnostics
            .Should()
            .OnlyContain(diagnostic => ReferenceEquals(diagnostic.Span.Snapshot, snapshot));
    }

    [TestCase("susproj", TestName = "Invalid UTF-8 In Project Should Prevent Configuration Analysis")]
    [TestCase("susln", TestName = "Invalid UTF-8 In Solution Should Prevent Configuration Analysis")]
    public async Task InvalidUTF8ShouldPreventConfigurationAnalysis(string extension)
    {
        ISourceDocumentType documentType = GetDocumentType(extension);
        SourceSnapshot snapshot = SourceSnapshot.FromUTF8(new Uri($"file:///TestProject/Invalid.{extension}"), version: null, [0xFF]);
        IDiagnosticReporter reporter = new DiagnosticReporter();

        AnalysisResult result = await documentType.Analyze(snapshot, reporter, CancellationToken.None);
        IReadOnlyList<SushiDiagnostic> diagnostics = reporter.ReportDiagnostics();

        AssertNoConfigurationDefinition(result);

        result.Snapshot
            .Should()
            .BeSameAs(snapshot);

        diagnostics
            .Should()
            .ContainSingle();

        diagnostics[0].IsType(ErrorType.InvalidUtf8)
            .Should()
            .BeTrue();

        diagnostics[0].Span.Snapshot
            .Should()
            .BeSameAs(snapshot);

        diagnostics[0].Span.Start
            .Should()
            .Be(0);

        diagnostics[0].Span.End
            .Should()
            .Be(1);
    }

    [TestCase("susproj", TestName = "Cancelled Project Configuration Analysis Should Not Publish Diagnostics")]
    [TestCase("susln", TestName = "Cancelled Solution Analysis Should Not Publish Diagnostics")]
    public async Task CancelledConfigurationAnalysisShouldNotPublishDiagnostics(string extension)
    {
        ISourceDocumentType documentType = GetDocumentType(extension);
        SourceSnapshot snapshot = CreateSnapshot(extension, ValidProjectConfiguration);
        IDiagnosticReporter reporter = new DiagnosticReporter();
        CancellationToken cancellationToken = new(canceled: true);

        Func<Task> action = async () => await documentType.Analyze(snapshot, reporter, cancellationToken);

        await action.Should().ThrowAsync<OperationCanceledException>();

        reporter.ReportDiagnostics().Should().BeEmpty();
    }

    private static ISourceDocumentType GetDocumentType(string extension)
    {
        ISourceDocumentProvider provider = new SushiSourceDocumentProvider();

        return provider.DocumentTypes.Single(type => type.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase));
    }

    private static SourceSnapshot CreateSnapshot(string extension, string text)
        => SourceSnapshot.FromText(new Uri($"file:///TestProject/Test.{extension}"), version: null, text);

    private static void AssertNoConfigurationDefinition(AnalysisResult result)
    {
        switch (result)
        {
            case ProjectAnalysisResult project:
                project.Project
                    .Should()
                    .BeNull();
                break;

            case SolutionAnalysisResult solution:
                solution.Solution
                    .Should()
                    .BeNull();
                break;

            default:
                AssertionChain.GetOrCreate().FailWith($"Unexpected analysis result type: {result.GetType().Name}");
                break;
        }
    }

    private const string ValidProjectConfiguration =
        """
        name = "Sushi Compiler"
        assembly = "Sushi.Compiler"
        language-version = "1.0"

        [build]
        default = "debug"

        [build.targets.debug]
        type = "Sushi.Compiler.Build.DebugTarget"
        """;
}
