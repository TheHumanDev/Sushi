using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Diagnostics;
using Sushi.Compiler.Parsing.Syntax;
using Sushi.Core.Diagnostics;
using Sushi.Core.Lexing;
using Sushi.Core.Lexing.Tokenization;
using Sushi.Core.Parsing.Syntax;
using Sushi.Core.Source;

namespace Sushi.Compiler.Parsing;

/// <summary>
/// Handles activity declaration parsing.
/// </summary>
public sealed class ActivityParser(IKeywordProvider keywordProvider) : IParser<ActivityDeclarationSyntax>
{
    /// <inheritdoc />
    public bool CanStart([NotNull] ParsingContext context)
    {
        if (!context.Cursor.TryPeek(out LexToken accessModifier) || !keywordProvider.IsAccessModifier(accessModifier, context.Cursor.Snapshot))
        {
            return false;
        }

        if (!context.Cursor.TryPeek(out LexToken next, lookahead: 1) || next.Type is not LexTokenType.Keyword)
        {
            return false;
        }

        return context.Cursor.TokenTextEquals(next, "root"u8) || context.Cursor.TokenTextEquals(next, "activity"u8);
    }

    /// <inheritdoc />
    public async Task<ActivityDeclarationSyntax> Parse([NotNull] ParsingContext context, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(diagnosticReporter);

        cancellationToken.ThrowIfCancellationRequested();

        IDiagnosticReporter localDiagnostics = new DiagnosticReporter();

        SyntaxToken accessModifier = context.Cursor.ParseAccessModifier(localDiagnostics);
        SyntaxToken? rootKeyword = context.Cursor.TryConsumeKeyword("root"u8, out LexToken root) ? new SyntaxToken(SyntaxType.RootKeyword, root) : null;
        SyntaxToken activityKeyword = context.Cursor.ParseKeyword("activity"u8, SyntaxType.ActivityKeyword, "Expected \"activity\" keyword.", localDiagnostics);
        SyntaxToken name = ParseNameComponent(context, localDiagnostics, "Expected activity name.", skipUnknown: false);
        SyntaxToken openBraceToken = context.Cursor.ParsePunctuation((byte)'{', SyntaxType.OpenBraceToken, "Expected \"{\" to begin activity body.", localDiagnostics);

        List<ActivityMemberSyntax> members = [];

        while (context.Cursor.TryPeek(out LexToken next) && !context.Cursor.IsPunctuation(next, (byte)'}'))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (this.IsActivityMethodDeclarationAhead(context))
            {
                members.Add(await ParseActivityMethodDeclaration(context, localDiagnostics, cancellationToken));
            }
            else
            {
                members.Add(this.ParseUnsupportedActivityMember(context, localDiagnostics, cancellationToken));
            }
        }

        SyntaxToken closeBraceToken = context.Cursor.ParsePunctuation((byte)'}', SyntaxType.CloseBraceToken, "Expected \"}\" to end activity body.", localDiagnostics);

        diagnosticReporter.CommitReporter(localDiagnostics);

        return new(
            accessModifier,
            rootKeyword,
            activityKeyword,
            name,
            openBraceToken,
            closeBraceToken
        );
    }

    private UnsupportedActivityMemberSyntax ParseUnsupportedActivityMember([NotNull] ParsingContext context, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
    {
        int start = context.Cursor.Position;
        int end = start;
        int braceDepth = 0;

        while (context.Cursor.TryPeek(out LexToken next))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (braceDepth == 0 && context.Cursor.IsPunctuation(next, (byte)'}'))
            {
                break;
            }

            if (end > start && braceDepth == 0 && this.IsActivityMethodDeclarationAhead(context))
            {
                break;
            }

            context.Cursor.TryAdvance(out LexToken consumed);
            end = consumed.Span.End;

            if (context.Cursor.IsPunctuation(consumed, (byte)'{'))
            {
                braceDepth++;
            }
            else if (context.Cursor.IsPunctuation(consumed, (byte)'}'))
            {
                braceDepth--;

                if (braceDepth == 0)
                {
                    break;
                }
            }
            else if (braceDepth == 0 && context.Cursor.IsPunctuation(consumed, (byte)';'))
            {
                break;
            }
        }

        SourceSpan span = new(context.Cursor.Snapshot, start, end);

        diagnosticReporter.GenerateErrorWithCustomMessage(
            ErrorType.ExpectedSyntax,
            "Unsupported activity member syntax.",
            span);

        return new UnsupportedActivityMemberSyntax(span);
    }

    private static async Task<ActivityMethodDeclarationSyntax> ParseActivityMethodDeclaration([NotNull] ParsingContext context, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
    {
        SyntaxToken accessModifier = context.Cursor.ParseAccessModifier(diagnosticReporter);
        SyntaxToken lifecycleModifier = ParseLifecycleModifier(context, diagnosticReporter);
        SyntaxToken returnType = ParseReturnType(context, diagnosticReporter);
        SyntaxToken name = ParseNameComponent(context, diagnosticReporter, "Expected activity method name.", skipUnknown: false);
        SyntaxToken openParenthesis = context.Cursor.ParsePunctuation((byte)'(', SyntaxType.OpenParenthesisToken, "Expected \"(\" after activity method name.", diagnosticReporter);
        (IReadOnlyList<ParameterSyntax> parameters, IReadOnlyList<SyntaxToken> separators) = ParseParameters(context, diagnosticReporter, cancellationToken);
        SyntaxToken closeParenthesis = context.Cursor.ParsePunctuation((byte)')', SyntaxType.CloseParenthesisToken, "Expected \")\" after activity method parameters.", diagnosticReporter);

        BlockSyntax body = await context.BlockParser.Parse(context, diagnosticReporter, cancellationToken);

        return new ActivityMethodDeclarationSyntax(
            accessModifier, lifecycleModifier, returnType, name, openParenthesis,
            parameters, separators, closeParenthesis, body);
    }

    private static (IReadOnlyList<ParameterSyntax> Parameters, IReadOnlyList<SyntaxToken> Separators) ParseParameters([NotNull] ParsingContext context, [NotNull] IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
    {
        List<ParameterSyntax> parameters = [];
        List<SyntaxToken> separators = [];

        if (!context.Cursor.TryPeek(out LexToken next) || context.Cursor.IsPunctuation(next, (byte)')') || context.Cursor.IsPunctuation(next, (byte)'{'))
        {
            return (parameters, separators);
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SyntaxToken type = ParseParameterType(context, diagnosticReporter);
            SyntaxToken name = ParseParameterName(context, diagnosticReporter);
            parameters.Add(new ParameterSyntax(type, name));

            if (context.Cursor.TryConsumePunctuation((byte)',', out LexToken comma))
            {
                separators.Add(new SyntaxToken(SyntaxType.CommaToken, comma));

                if (!context.Cursor.TryPeek(out next) || context.Cursor.IsPunctuation(next, (byte)')') || context.Cursor.IsPunctuation(next, (byte)'{'))
                {
                    int position = context.Cursor.Position;
                    context.Cursor.AccumulateExpectedDiagnostic("Expected parameter declaration after comma.", position, diagnosticReporter);

                    SyntaxToken missingType = SyntaxToken.Missing(SyntaxType.IdentifierToken, context.Cursor.Snapshot, position);
                    SyntaxToken missingName = SyntaxToken.Missing(SyntaxType.IdentifierToken, context.Cursor.Snapshot, position);

                    parameters.Add(new ParameterSyntax(missingType, missingName));
                    break;
                }

                continue;
            }

            if (!context.Cursor.TryPeek(out next) || context.Cursor.IsPunctuation(next, (byte)')') || context.Cursor.IsPunctuation(next, (byte)'{'))
            {
                break;
            }

            if (next.Type is not (LexTokenType.Identifier or LexTokenType.EscapedIdentifier))
            {
                break;
            }

            int missingCommaPosition = context.Cursor.Position;
            context.Cursor.AccumulateExpectedDiagnostic("Expected comma between parameters.", missingCommaPosition, diagnosticReporter);
            separators.Add(SyntaxToken.Missing(SyntaxType.CommaToken, context.Cursor.Snapshot, missingCommaPosition));
        }

        return (parameters, separators);
    }

    private static SyntaxToken ParseParameterType([NotNull] ParsingContext context, [NotNull] IDiagnosticReporter diagnosticReporter)
    {
        if (context.Cursor.TryConsumeIdentifier(out LexToken token))
        {
            SyntaxType type = token.Type is LexTokenType.EscapedIdentifier ? SyntaxType.EscapedIdentifierToken : SyntaxType.IdentifierToken;
            return new SyntaxToken(type, token);
        }

        int position = context.Cursor.Position;
        context.Cursor.AccumulateExpectedDiagnostic("Expected parameter type.", position, diagnosticReporter);
        return SyntaxToken.Missing(SyntaxType.IdentifierToken, context.Cursor.Snapshot, position);
    }

    private static SyntaxToken ParseParameterName([NotNull] ParsingContext context, [NotNull] IDiagnosticReporter diagnosticReporter)
    {
        if (context.Cursor.TryPeek(out LexToken token)
            && token.Type is LexTokenType.Unknown
            && IsDiscardSpelling(context, token))
        {
            context.Cursor.TryAdvance(out LexToken discard);
            return new SyntaxToken(SyntaxType.IdentifierToken, discard);
        }

        return ParseNameComponent(context, diagnosticReporter, "Expected parameter name.", skipUnknown: false);
    }

    private static bool IsDiscardSpelling([NotNull] ParsingContext context, LexToken token)
        => context.Cursor.Snapshot.Bytes.Span[token.Span.Start..token.Span.End].SequenceEqual("_"u8);

    private static SyntaxToken ParseLifecycleModifier([NotNull] ParsingContext context, [NotNull] IDiagnosticReporter diagnosticReporter)
    {
        if (context.Cursor.TryConsumeKeyword("entry"u8, out LexToken entry))
        {
            return new SyntaxToken(SyntaxType.EntryKeyword, entry);
        }

        if (context.Cursor.TryConsumeKeyword("stop"u8, out LexToken stop))
        {
            return new SyntaxToken(SyntaxType.StopKeyword, stop);
        }

        int position = context.Cursor.Position;
        context.Cursor.AccumulateExpectedDiagnostic("Expected \"entry\" or \"stop\" modifier.", position, diagnosticReporter);

        return SyntaxToken.Missing(SyntaxType.EntryKeyword, context.Cursor.Snapshot, position);
    }

    private static SyntaxToken ParseReturnType([NotNull] ParsingContext context, [NotNull] IDiagnosticReporter diagnosticReporter)
    {
        ArgumentNullException.ThrowIfNull(diagnosticReporter);

        if (context.Cursor.TryPeek(out LexToken token))
        {
            SyntaxType type = token.Type switch
            {
                LexTokenType.Identifier => SyntaxType.IdentifierToken,
                LexTokenType.EscapedIdentifier => SyntaxType.EscapedIdentifierToken,
                LexTokenType.Keyword when context.Cursor.TokenTextEquals(token, "void"u8) => SyntaxType.VoidKeyword,
                _ => default
            };

            if (type != default)
            {
                context.Cursor.TryAdvance(out _);
                return new SyntaxToken(type, token);
            }
        }

        int position = context.Cursor.Position;
        context.Cursor.AccumulateExpectedDiagnostic("Expected function return type.", position, diagnosticReporter);
        return SyntaxToken.Missing(SyntaxType.IdentifierToken, context.Cursor.Snapshot, position);
    }

    private bool IsActivityMethodDeclarationAhead([NotNull] ParsingContext context)
        => context.Cursor.TryPeek(out LexToken accessModifier)
            && keywordProvider.IsAccessModifier(accessModifier, context.Cursor.Snapshot)
            && context.Cursor.TryPeek(out LexToken lifecycle, lookahead: 1)
            && lifecycle.Type is LexTokenType.Keyword
            && (context.Cursor.TokenTextEquals(lifecycle, "entry"u8) || context.Cursor.TokenTextEquals(lifecycle, "stop"u8));

    private static SyntaxToken ParseNameComponent([NotNull] ParsingContext context, [NotNull] IDiagnosticReporter diagnosticReporter, string diagnosticMessage, bool skipUnknown)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(diagnosticReporter);

        if (context.Cursor.TryConsumeIdentifier(out LexToken token, skipUnknown))
        {
            SyntaxType type = token.Type is LexTokenType.EscapedIdentifier
                ? SyntaxType.EscapedIdentifierToken
                : SyntaxType.IdentifierToken;

            return new SyntaxToken(type, token);
        }

        int position = context.Cursor.Position;
        context.Cursor.AccumulateExpectedDiagnostic(diagnosticMessage, position, diagnosticReporter);
        return SyntaxToken.Missing(SyntaxType.IdentifierToken, context.Cursor.Snapshot, position);
    }
}
