using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Source;

namespace Sushi.Core.Parsing.Syntax;

/// <summary>
/// Represents a declared activity with an explicit lifetime classification and ordered members.
/// </summary>
public sealed class ActivityDeclarationSyntax : SyntaxNode
{
    /// <inheritdoc />
    public override SyntaxType Type => SyntaxType.ActivityDeclaration;

    /// <inheritdoc />
    public override SourceSpan Span => new(this.AccessModifier.Span.Snapshot, this.AccessModifier.Span.Start, this.CloseBraceToken.Span.End);

    /// <summary>
    /// Gets the activity's declared accessibility.
    /// </summary>
    public SyntaxToken AccessModifier { get; }

    /// <summary>
    /// Gets the optional root modifier, present only for a root declaration.
    /// </summary>
    public SyntaxToken? RootKeyword { get; }

    /// <summary>
    /// Gets the activity keyword introducing the declaration.
    /// </summary>
    public SyntaxToken ActivityKeyword { get; }

    /// <summary>
    /// Gets the declared activity identifier.
    /// </summary>
    public SyntaxToken Name { get; }

    /// <summary>
    /// Gets the opening brace.
    /// </summary>
    public SyntaxToken OpenBraceToken { get; }

    /// <summary>
    /// Gets the closing brace.
    /// </summary>
    public SyntaxToken CloseBraceToken { get; }

    /// <summary>
    /// Creates an activity declaration while preserving source and recovery information.
    /// </summary>
    /// <param name="accessModifier">
    /// The activity's explicit accessibility.
    /// </param>
    /// <param name="rootKeyword">
    /// The optional root keyword.
    /// </param>
    /// <param name="activityKeyword">
    /// The keyword introducing the activity declaration.
    /// </param>
    /// <param name="name">
    /// The activity identifier.
    /// </param>
    /// <param name="openBraceToken">
    /// The opening brace.
    /// </param>
    /// <param name="closeBraceToken">
    /// The closing brace.
    /// </param>
    public ActivityDeclarationSyntax(
        [NotNull] SyntaxToken accessModifier,
        SyntaxToken? rootKeyword,
        [NotNull] SyntaxToken activityKeyword,
        [NotNull] SyntaxToken name,
        [NotNull] SyntaxToken openBraceToken,
        [NotNull] SyntaxToken closeBraceToken)
    {

        if (accessModifier.Type is not SyntaxType.AccessModifierKeyword)
        {
            throw new ArgumentException("Activity declarations require an access modifier.", nameof(accessModifier));
        }

        if (rootKeyword is SyntaxToken root && root.Type is not SyntaxType.RootKeyword)
        {
            throw new ArgumentException("The optional root modifier must be a root keyword.", nameof(rootKeyword));
        }

        if (activityKeyword.Type is not SyntaxType.ActivityKeyword)
        {
            throw new ArgumentException("Activity declarations require the activity keyword.", nameof(activityKeyword));
        }

        if (name.Type is not (SyntaxType.IdentifierToken or SyntaxType.EscapedIdentifierToken))
        {
            throw new ArgumentException("Activity declarations require an identifier.", nameof(name));
        }

        if (openBraceToken.Type is not SyntaxType.OpenBraceToken || closeBraceToken.Type is not SyntaxType.CloseBraceToken)
        {
            throw new ArgumentException("Activity declarations require brace tokens.");
        }

        SourceSnapshot snapshot = accessModifier.Span.Snapshot;
        SourceSpan[] header =
        [
            .. rootKeyword is SyntaxToken rootToken ? [rootToken.Span] : Array.Empty<SourceSpan>(),
            activityKeyword.Span,
            name.Span,
            openBraceToken.Span
        ];

        int previousEnd = accessModifier.Span.End;

        foreach (SourceSpan span in header)
        {
            if (!ReferenceEquals(span.Snapshot, snapshot) || span.Start < previousEnd)
            {
                throw new ArgumentException("Activity declaration tokens must occur in order in one source snapshot.");
            }

            previousEnd = span.End;
        }

        if (!ReferenceEquals(closeBraceToken.Span.Snapshot, snapshot) || closeBraceToken.Span.Start < previousEnd)
        {
            throw new ArgumentException("The activity closing brace must follow its members.", nameof(closeBraceToken));
        }

        this.AccessModifier = accessModifier;
        this.RootKeyword = rootKeyword;
        this.ActivityKeyword = activityKeyword;
        this.Name = name;
        this.OpenBraceToken = openBraceToken;
        this.CloseBraceToken = closeBraceToken;
    }
}
