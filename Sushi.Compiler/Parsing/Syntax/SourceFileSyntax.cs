using System.Collections.ObjectModel;
using Sushi.Core.Parsing.Syntax;
using Sushi.Core.Source;

namespace Sushi.Compiler.Parsing.Syntax;

/// <summary>
/// Represents a Sushi source file, including parsed declarations and remaining unsupported source.
/// </summary>
public sealed class SourceFileSyntax : SyntaxNode
{
    /// <inheritdoc />
    public override SyntaxType Type => SyntaxType.SourceFile;

    /// <inheritdoc />
    public override SourceSpan Span => new(this.Snapshot, 0, this.Snapshot.SourceLength);

    /// <summary>
    /// Gets the authoritative source snapshot.
    /// </summary>
    public SourceSnapshot Snapshot { get; }

    /// <summary>
    /// Gets activity declarations in source order.
    /// </summary>
    public IReadOnlyList<ActivityDeclarationSyntax> ActivityDeclarations { get; }

    /// <summary>
    /// Gets all recognized package-level functions and activities in their original source order.
    /// </summary>
    public IReadOnlyList<SyntaxNode> Declarations { get; }

    /// <summary>
    /// Gets the remaining unparsed source range, or null when no significant content remains.
    /// </summary>
    public SourceSpan? UnparsedContentSpan { get; }

    /// <summary>
    /// Creates a source file with separate and source-ordered function and activity declarations.
    /// </summary>
    /// <param name="snapshot">
    /// The authoritative source snapshot.
    /// </param>
    /// <param name="packageDeclarations">
    /// The leading package declarations, including duplicates.
    /// </param>
    /// <param name="namespaceDeclarations">
    /// The leading namespace declarations, including duplicates.
    /// </param>
    /// <param name="functionDeclarations">
    /// The package-level functions.
    /// </param>
    /// <param name="activityDeclarations">
    /// The activity declarations.
    /// </param>
    /// <param name="unparsedContentSpan">
    /// The source remainder beginning at the first unparsed significant element.
    /// </param>
    public SourceFileSyntax(
        SourceSnapshot snapshot,
        IReadOnlyList<ActivityDeclarationSyntax> activityDeclarations,
        SourceSpan? unparsedContentSpan)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(activityDeclarations);

        IReadOnlyList<ActivityDeclarationSyntax> activities = new ReadOnlyCollection<ActivityDeclarationSyntax>([.. activityDeclarations]);

        ValidateDeclarations(snapshot, activities, nameof(activityDeclarations));

        SyntaxNode[] declarations = [.. activities];
        Array.Sort(declarations, static (left, right) => left.Span.Start.CompareTo(right.Span.Start));

        if (unparsedContentSpan is SourceSpan remainder)
        {
            if (!ReferenceEquals(remainder.Snapshot, snapshot)
                || remainder.Start >= remainder.End
                || remainder.End != snapshot.SourceLength)
            {
                throw new ArgumentException("The unparsed source range must follow parsed declarations and end at the source boundary.", nameof(unparsedContentSpan));
            }
        }

        this.Snapshot = snapshot;
        this.ActivityDeclarations = activities;
        this.Declarations = declarations;
        this.UnparsedContentSpan = unparsedContentSpan;
    }

    /// <summary>
    /// Validates that the given declarations are correctly formed. Note that this is not meant to
    /// generate diagnostics or handle recovery; this is meant to reject genuinely exceptional
    /// state that would result in undefined syntactic meaning.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the declarations in the list.
    /// </typeparam>
    /// <param name="snapshot">
    /// The source snapshot that the declarations originated from.
    /// </param>
    /// <param name="declarations">
    /// The list of declarations.
    /// </param>
    /// <param name="parameterName">
    /// The name of the parameter of the calling method.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if the declarations do not belong to the source snapshot, do not occur in order,
    /// or overlap in some way.
    /// </exception>
    private static void ValidateDeclarations<T>(SourceSnapshot snapshot, IReadOnlyList<T> declarations, string parameterName) where T : SyntaxNode
    {
        int previousEnd = 0;

        foreach (T declaration in declarations)
        {
            if (declaration is null || !ReferenceEquals(declaration.Span.Snapshot, snapshot) || declaration.Span.Start < previousEnd)
            {
                throw new ArgumentException("Declarations must belong to the source snapshot and occur in order without overlapping.", parameterName);
            }

            previousEnd = declaration.Span.End;
        }
    }
}
