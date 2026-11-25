using AwesomeAssertions;
using NUnit.Framework;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Sushi.Compiler.Configuration;
using Sushi.Compiler.Diagnostics;
using Sushi.Compiler.Lexing;
using Sushi.Compiler.Source;
using Sushi.Core.Analysis;
using Sushi.Core.Configuration;
using Sushi.Core.Diagnostics;
using Sushi.Core.Lexing;
using Sushi.Core.Source;
using Sushi.LanguageServer.Workspace;
using Sushi.Testing.Mocks;

namespace Sushi.Testing.LanguageServer.Workspace;

[TestFixture]
public sealed class WorkspaceProjectMembershipTests
{
    private ISourceDocumentProvider documentProvider;
    private IProjectSourceLoader sourceLoader;

    private IDiagnosticReporterFactory diagnosticReporterFactory;

    private IKeywordProvider keywordProvider;

    private WorkspaceOrchestrator orchestrator;

    private WorkspaceProjectMembership projectMembership;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        this.keywordProvider = new KeywordProvider();
        this.documentProvider = new SushiSourceDocumentProvider(this.keywordProvider);
        this.sourceLoader = new ProjectSourceLoader(this.documentProvider);
        this.projectMembership = new WorkspaceProjectMembership(this.sourceLoader);
        this.diagnosticReporterFactory = new DiagnosticReporterFactory();
        this.orchestrator = new WorkspaceOrchestrator(this.documentProvider, this.diagnosticReporterFactory, this.projectMembership);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => this.orchestrator.Dispose();

    [TestCase(TestName = "Project Membership Should Associate Only Included Sources")]
    public void ProjectMembershipShouldAssociateOnlyIncludedSources()
    {
        string directory = CreateTemporaryDirectory();

        try
        {
            string includedPath = CreateFile(directory, "src/Main.sus", "");
            string excludedPath = CreateFile(directory, "src/Generated/Auto.sus", "");
            string standalonePath = CreateFile(directory, "outside/Standalone.sus", "");

            SourceSnapshot projectSnapshot = CreateSnapshot(CreateFile(directory, ".susproj", ""));
            SourceSnapshot includedSnapshot = CreateSnapshot(includedPath);
            SourceSnapshot excludedSnapshot = CreateSnapshot(excludedPath);
            SourceSnapshot standaloneSnapshot = CreateSnapshot(standalonePath);

            AnalysisResult[] analyses =
            [
                new SourceAnalysisResult(includedSnapshot, MockConcreteSyntaxTree.FromSnapshot(includedSnapshot)),
                new SourceAnalysisResult(excludedSnapshot, MockConcreteSyntaxTree.FromSnapshot(excludedSnapshot)),
                new SourceAnalysisResult(standaloneSnapshot, MockConcreteSyntaxTree.FromSnapshot(standaloneSnapshot)),
                new ProjectAnalysisResult(projectSnapshot, CreateProject(["src/**/*.sus"], ["src/Generated/**"]))
            ];

            IReadOnlyList<AnalysisResult> associated = this.projectMembership.AssociateSources(analyses, CancellationToken.None);
            SourceAnalysisResult[] sources = [.. associated.OfType<SourceAnalysisResult>()];

            sources[0].ProjectUri
                .Should()
                .Be(projectSnapshot.Uri);
            
            sources[1].ProjectUri
                .Should()
                .BeNull();
            sources[2].ProjectUri
                .Should()
                .BeNull();

            associated[3]
                .Should()
                .BeSameAs(analyses[3]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestCase(TestName = "Overlapping Project Membership Should Remain Ambiguous")]
    public void OverlappingProjectMembershipShouldRemainAmbiguous()
    {
        string directory = CreateTemporaryDirectory();

        try
        {
            string sourcePath = CreateFile(directory, "sub/Shared.sus", "");
            SourceSnapshot rootProject = CreateSnapshot(CreateFile(directory, ".susproj", ""));
            SourceSnapshot nestedProject = CreateSnapshot(CreateFile(directory, "sub/.susproj", ""));
            SourceSnapshot sourceAnalysisSnapshot = CreateSnapshot(sourcePath);
            AnalysisResult[] analyses =
            [
                new ProjectAnalysisResult(rootProject, CreateProject(["**/*.sus"], [])),
                new ProjectAnalysisResult(nestedProject, CreateProject(["**/*.sus"], [])),
                new SourceAnalysisResult(sourceAnalysisSnapshot, MockConcreteSyntaxTree.FromSnapshot(sourceAnalysisSnapshot))
            ];

            IReadOnlyList<AnalysisResult> associated = this.projectMembership.AssociateSources(analyses, CancellationToken.None);

            associated.OfType<SourceAnalysisResult>()
                .Single()
                .ProjectUri
                .Should()
                .BeNull();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestCase(TestName = "Invalid Projects Should Not Claim Standalone Sources")]
    public void InvalidProjectsShouldNotClaimStandaloneSources()
    {
        string directory = CreateTemporaryDirectory();

        try
        {
            string sourcePath = CreateFile(directory, "Main.sus", "");
            SourceSnapshot projectSnapshot = CreateSnapshot(CreateFile(directory, ".susproj", ""));
            SourceSnapshot sourceSnapshot = CreateSnapshot(sourcePath);

            AnalysisResult[] analyses =
            [
                new ProjectAnalysisResult(projectSnapshot, null),
                new SourceAnalysisResult(sourceSnapshot, MockConcreteSyntaxTree.FromSnapshot(sourceSnapshot))
            ];

            IReadOnlyList<AnalysisResult> associated = this.projectMembership.AssociateSources(analyses, CancellationToken.None);

            associated.OfType<SourceAnalysisResult>()
                .Single()
                .ProjectUri
                .Should()
                .BeNull();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestCase(TestName = "Workspace Initialization Should Analyze Solutions Projects And Included Sources")]
    public async Task WorkspaceInitializationShouldAnalyzeSolutionsProjectsAndIncludedSources()
    {
        string directory = CreateTemporaryDirectory();

        try
        {
            string solutionPath = CreateFile(directory, ".susln", "name = \"Test Solution\"");
            string projectPath = CreateFile(directory, ".susproj", ValidProjectConfiguration);
            string includedPath = CreateFile(directory, "src/Main.sus", "// included");
            string excludedPath = CreateFile(directory, "src/Generated/Auto.sus", "// excluded");

            WorkspaceFolder folder = new()
            {
                Name = "Test",
                Uri = DocumentUri.FromFileSystemPath(directory)
            };

            await this.orchestrator.Initialize([folder], CancellationToken.None);

            WorkspaceAnalysisInput input = await this.orchestrator.CaptureAnalysisInput(CancellationToken.None);

            AnalysisResult solution = GetAnalysis(input, solutionPath);
            AnalysisResult project = GetAnalysis(input, projectPath);
            AnalysisResult included = GetAnalysis(input, includedPath);
            AnalysisResult excluded = GetAnalysis(input, excludedPath);

            solution
                .Should()
                .BeOfType<SolutionAnalysisResult>()
                .Which.Solution
                .Should()
                .NotBeNull();

            project
                .Should()
                .BeOfType<ProjectAnalysisResult>()
                .Which.Project
                .Should()
                .NotBeNull();

            included
                .Should()
                .BeOfType<SourceAnalysisResult>()
                .Which.ProjectUri
                .Should()
                .Be(new Uri(projectPath));

            excluded
                .Should()
                .BeOfType<SourceAnalysisResult>()
                .Which.ProjectUri
                .Should()
                .BeNull();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestCase(TestName = "Workspace Refresh Should Recalculate Project Source Membership")]
    public async Task WorkspaceRefreshShouldRecalculateProjectSourceMembership()
    {
        string directory = CreateTemporaryDirectory();

        try
        {
            string projectPath = CreateFile(directory, ".susproj", ValidProjectConfiguration);
            string sourcePath = CreateFile(directory, "src/Main.sus", "// source");

            WorkspaceFolder folder = new()
            {
                Name = "Test",
                Uri = DocumentUri.FromFileSystemPath(directory)
            };

            await this.orchestrator.Initialize([folder], CancellationToken.None);

            WorkspaceAnalysisInput initial = await this.orchestrator.CaptureAnalysisInput(CancellationToken.None);

            GetAnalysis(initial, sourcePath)
                .Should()
                .BeOfType<SourceAnalysisResult>()
                .Which.ProjectUri
                .Should()
                .Be(new Uri(projectPath));

            await File.WriteAllTextAsync(projectPath, ValidProjectConfiguration.Replace(
                "exclude = [\"src/Generated/**\"]",
                "exclude = [\"src/**\"]",
                StringComparison.Ordinal));

            await this.orchestrator.UpdateWorkspaceFolders([], [], CancellationToken.None);

            WorkspaceAnalysisInput updated = await this.orchestrator.CaptureAnalysisInput(CancellationToken.None);

            GetAnalysis(updated, sourcePath)
                .Should()
                .BeOfType<SourceAnalysisResult>()
                .Which.ProjectUri
                .Should()
                .BeNull();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static AnalysisResult GetAnalysis(WorkspaceAnalysisInput input, string path)
    {
        SourceDocument document = input.Documents
            .Single(item => item.Document.Uri.IsSamePath(path))
            .Document;

        document.TryGetCurrentAnalysis(out AnalysisResult? result, out IReadOnlyList<SushiDiagnostic>? diagnostics)
            .Should()
            .BeTrue();

        result
            .Should()
            .NotBeNull();
        
        diagnostics
            .Should()
            .BeEmpty();

        return result!;
    }

    private static ProjectDefinition CreateProject(IReadOnlyList<string> sources, IReadOnlyList<string> exclusions)
        => new("Test", "Test", "1.0", new ProjectSourceDefinition(null, exclusions),
            new ProjectBuildDefinition("debug", sources, new Dictionary<string, ProjectBuildTargetDefinition>()));

    private static SourceSnapshot CreateSnapshot(string path)
        => SourceSnapshot.FromText(new Uri(path), version: null, text: "");

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"SushiWorkspace-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string CreateFile(string directory, string relativePath, string text)
    {
        string path = Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        return path;
    }

    private const string ValidProjectConfiguration =
        """
        name = "Test"
        assembly = "Test"
        language-version = "1.0"

        [source]
        exclude = ["src/Generated/**"]

        [build]
        default = "debug"
        sources = ["src/**/*.sus"]

        [build.targets.debug]
        type = "Test.DebugTarget"
        """;
}
