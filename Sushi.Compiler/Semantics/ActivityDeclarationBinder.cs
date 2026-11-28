using Sushi.Core.Diagnostics;
using Sushi.Core.Parsing.Syntax;
using Sushi.Core.Semantics;

namespace Sushi.Compiler.Semantics;

/// <summary>
/// Handles binding of activity identities.
/// </summary>
public static class ActivityDeclarationBinder
{
    /// <summary>
    /// Validates a parsed activity and returns its semantic representation when complete.
    /// </summary>
    /// <param name="declaration">
    /// The activity declaration to validate.
    /// </param>
    /// <param name="diagnosticReporter">
    /// The reporter used to accumulate diagnostics.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancels semantic binding.
    /// </param>
    /// <returns>
    /// The bound activity, or null when its declaration cannot be represented correctly.
    /// </returns>
    public static BoundActivity? Bind(ActivityDeclarationSyntax declaration, IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(diagnosticReporter);
        cancellationToken.ThrowIfCancellationRequested();

        if (!HasCompleteHeader(declaration)
            || !TryBindAccessibility(declaration.AccessModifier, out Accessibility accessibility))
        {
            return null;
        }

        string? name = IdentifierBinder.Bind(declaration.Name);

        if (name is null)
        {
            return null;
        }

        List<ActivityMethodDeclarationSyntax> entries = [];
        List<ActivityMethodDeclarationSyntax> stops = [];
        bool valid = true;

        if (!valid)
        {
            return null;
        }

        BoundActivityMethod? entryMethod = BindMethod(entries[0]);
        BoundActivityMethod? stopMethod = stops.Count == 0 ? null : BindMethod(stops[0]);

        if (entryMethod is null || (stops.Count != 0 && stopMethod is null))
        {
            return null;
        }

        return new BoundActivity(name, accessibility, entryMethod, stopMethod, declaration);
    }

    private static BoundActivityMethod? BindMethod(ActivityMethodDeclarationSyntax declaration)
    {
        if (!declaration.AccessModifier.IsSourceBacked
            || !declaration.LifecycleModifier.IsSourceBacked
            || !declaration.ReturnType.IsSourceBacked
            || !declaration.OpenParenthesisToken.IsSourceBacked
            || !declaration.CloseParenthesisToken.IsSourceBacked
            || !declaration.Body.OpenBraceToken.IsSourceBacked
            || !declaration.Body.CloseBraceToken.IsSourceBacked
            || declaration.Parameters.Any(parameter => !parameter.TypeToken.IsSourceBacked || !parameter.NameToken.IsSourceBacked)
            || declaration.ParameterSeparators.Any(separator => !separator.IsSourceBacked)
            || !TryBindAccessibility(declaration.AccessModifier, out Accessibility accessibility))
        {
            return null;
        }

        string? name = IdentifierBinder.Bind(declaration.Name);

        return name is null ? null : new BoundActivityMethod(name, accessibility, declaration);
    }

    private static bool HasCompleteHeader(ActivityDeclarationSyntax declaration)
        => declaration.AccessModifier.IsSourceBacked
            && (declaration.RootKeyword is null || declaration.RootKeyword.Value.IsSourceBacked)
            && declaration.ActivityKeyword.IsSourceBacked
            && declaration.Name.IsSourceBacked
            && declaration.OpenBraceToken.IsSourceBacked
            && declaration.CloseBraceToken.IsSourceBacked;

    private static bool TryBindAccessibility(SyntaxToken token, out Accessibility accessibility)
    {
        if (!token.IsSourceBacked)
        {
            accessibility = default;
            return false;
        }

        ReadOnlySpan<byte> spelling = token.Span.Snapshot.Bytes.Span[token.Span.Start..token.Span.End];

        if (spelling.SequenceEqual("public"u8))
        {
            accessibility = Accessibility.Public;
            return true;
        }

        if (spelling.SequenceEqual("internal"u8))
        {
            accessibility = Accessibility.Internal;
            return true;
        }

        if (spelling.SequenceEqual("package"u8))
        {
            accessibility = Accessibility.Package;
            return true;
        }

        if (spelling.SequenceEqual("protected"u8))
        {
            accessibility = Accessibility.Protected;
            return true;
        }

        if (spelling.SequenceEqual("private"u8))
        {
            accessibility = Accessibility.Private;
            return true;
        }

        accessibility = default;
        return false;
    }
}
