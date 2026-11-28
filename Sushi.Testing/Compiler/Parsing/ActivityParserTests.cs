using AwesomeAssertions;
using NUnit.Framework;
using Sushi.Compiler.Diagnostics;
using Sushi.Compiler.Lexing;
using Sushi.Compiler.Parsing;
using Sushi.Compiler.Parsing.Syntax;
using Sushi.Core.Diagnostics;
using Sushi.Core.Lexing;
using Sushi.Core.Parsing;
using Sushi.Core.Parsing.Syntax;
using Sushi.Core.Source;

namespace Sushi.Testing.Compiler.Parsing;

[TestFixture]
public sealed class ActivityParserTests
{
    private static readonly Uri testUri = new("file:///TestProject/Activities.sus");

    private SourceLexer sourceLexer;
    private ActivityParser activityParser;
    private IKeywordProvider keywordProvider;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        this.keywordProvider = new KeywordProvider();
        this.sourceLexer = new(this.keywordProvider);
        this.activityParser = new ActivityParser(this.keywordProvider);
    }

    [TestCase(TestName = "Parser Should Recover Missing Activity Identifier")]
    public async Task ParserShouldRecoverMissingActivityIdentifier()
    {
        const string source = "public root activity {}";

        (ActivityDeclarationSyntax result, IReadOnlyList<SushiDiagnostic> diagnostics) = await this.ParseActivity(source);

        diagnostics
            .Should()
            .ContainSingle(diagnostic => diagnostic.IsType(ErrorType.ExpectedSyntax));

        result.Name.IsMissing
            .Should()
            .BeTrue();
    }

    [TestCase(TestName = "Parser Should Recover Missing Activity Keyword")]
    public async Task ParserShouldRecoverMissingActivityKeyword()
    {
        const string source = "public root Application {}";

        (ActivityDeclarationSyntax result, IReadOnlyList<SushiDiagnostic> diagnostics) = await this.ParseActivity(source);

        diagnostics
            .Should()
            .ContainSingle(diagnostic => diagnostic.IsType(ErrorType.ExpectedSyntax));

        result.ActivityKeyword.IsMissing
            .Should()
            .BeTrue();

        result.Name.IsMissing
            .Should()
            .BeFalse();
    }

    private async Task<(ActivityDeclarationSyntax result, IReadOnlyList<SushiDiagnostic> diagnostics)> ParseActivity(string source)
    {
        SourceSnapshot snapshot = SourceSnapshot.FromText(testUri, version: null, source);
        IDiagnosticReporter reporter = new DiagnosticReporter();
        LexResult lexResult = await this.sourceLexer.Lex(snapshot, reporter, CancellationToken.None);
        CancellationToken cancellationToken = new();
        ParserCursor cursor = new(lexResult, this.keywordProvider, cancellationToken);
        ParsingContext context = new(lexResult, cursor, this.keywordProvider);
        ActivityDeclarationSyntax result = await this.activityParser.Parse(context, reporter, CancellationToken.None);

        return (result, reporter.ReportDiagnostics());
    }

    private static SourceFileSyntax GetSourceFile(ParserResult result)
        => result.Tree.Root
            .Should()
            .BeOfType<SourceFileSyntax>()
            .Subject;
}
