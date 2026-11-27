using AwesomeAssertions;
using NUnit.Framework;
using Sushi.Compiler;
using Sushi.Compiler.Configuration;
using Sushi.Compiler.Lexing;
using Sushi.Compiler.Parsing;
using Sushi.Compiler.Source;
using Sushi.Core;
using Sushi.Core.Analysis;
using Sushi.Core.Configuration;
using Sushi.Core.Lexing;
using Sushi.Core.Parsing;
using Sushi.Core.Source;

namespace Sushi.Testing.Compiler.Configuration;

[TestFixture]
public sealed class ProjectSourceLoaderTests
{
    private IProjectSourceLoader sourceLoader;
    private ISourceDocumentProvider documentProvider;
    private IKeywordProvider keywordProvider;
    private ILexer lexer;
    private ISourceParser<ParsingContext> parser;
    private ISushiContext<ParsingContext> context;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        this.keywordProvider = new KeywordProvider();
        this.lexer = new SourceLexer(this.keywordProvider);
        this.parser = new SourceParser();
        this.sourceLoader = new ProjectSourceLoader(this.documentProvider);
        this.context = new SushiContext(this.keywordProvider, this.lexer, this.parser, this.sourceLoader);
        this.documentProvider = new SushiSourceDocumentProvider(this.context);
        this.sourceLoader = new ProjectSourceLoader(this.documentProvider);
    }

    [TestCase(TestName = "Project Source Discovery Should Honor Globs Exclusions And Sushi Extensions")]
    public void ProjectSourceDiscoveryShouldHonorGlobsExclusionsAndSushiExtensions()
    {
        string directory = CreateTemporaryDirectory();

        try
        {
            string first = CreateFile(directory, "src/A.sus");
            string second = CreateFile(directory, "src/Nested/B.sus");

            CreateFile(directory, "src/Generated/Generated.sus");
            CreateFile(directory, "src/Nested/Notes.txt");
            CreateFile(directory, "outside/Other.sus");

            ProjectDefinition project = CreateProject(["src/**/*.sus"], ["src/Generated/**"]);

            IReadOnlyList<string> paths = this.sourceLoader.DiscoverSourcePaths(directory, project, CancellationToken.None);

            string[] expected = [first, second];
            Array.Sort(expected, StringComparer.Ordinal);

            paths
                .Should()
                .Equal(expected);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestCase(TestName = "Project Source Discovery Should Not Infer Includes When None Are Configured")]
    public void ProjectSourceDiscoveryShouldNotInferIncludesWhenNoneAreConfigured()
    {
        string directory = CreateTemporaryDirectory();

        try
        {
            CreateFile(directory, "Program.sus");

            ProjectDefinition project = CreateProject([], []);

            this.sourceLoader.DiscoverSourcePaths(directory, project, CancellationToken.None)
                .Should()
                .BeEmpty();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestCase(TestName = "Project Source Discovery Should Respect Cancellation")]
    public void ProjectSourceDiscoveryShouldRespectCancellation()
    {
        ProjectDefinition project = CreateProject(["**/*.sus"], []);
        CancellationToken cancellationToken = new(canceled: true);

        Action action = () => this.sourceLoader.DiscoverSourcePaths(Path.GetTempPath(), project, cancellationToken);

        action
            .Should()
            .Throw<OperationCanceledException>();
    }

    private static ProjectDefinition CreateProject(IReadOnlyList<string> sources, IReadOnlyList<string> exclusions)
        => new("Test", "Test", "1.0", new ProjectSourceDefinition(null, exclusions),
            new ProjectBuildDefinition("debug", sources, new Dictionary<string, ProjectBuildTargetDefinition>()));

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"SushiSourceDiscovery-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string CreateFile(string directory, string relativePath)
    {
        string path = Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "// fixture");
        return path;
    }
}
