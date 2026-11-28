using Sushi.Compiler.Diagnostics;
using Sushi.Compiler.Parsing.Syntax;
using Sushi.Core.Diagnostics;
using Sushi.Core.Parsing.Syntax;
using Sushi.Core.Semantics;

namespace Sushi.Compiler.Semantics;

/// <summary>
/// Binds file-level semantic information and validates invariants spanning
/// complete declarations.
/// </summary>
public sealed class SourceFileBinder
{
    /// <summary>
    /// Binds source-file declarations.
    /// </summary>
    /// <param name="sourceFile">
    /// The parsed source file.
    /// </param>
    /// <param name="diagnosticReporter">
    /// The reporter used to accumulate diagnostics.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancels binding.
    /// </param>
    /// <param name="signatureOnly">
    /// Whether package functions should expose only their signatures.
    /// </param>
    /// <returns>
    /// File-level semantic declarations for the selected binding phase.
    /// </returns>
    internal static SourceFileBindResult Bind(SourceFileSyntax sourceFile, IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sourceFile);
        ArgumentNullException.ThrowIfNull(diagnosticReporter);
        cancellationToken.ThrowIfCancellationRequested();

        IDiagnosticReporter localDiagnostics = new DiagnosticReporter();

        List<BoundActivity> activities = [];

        foreach (ActivityDeclarationSyntax declaration in sourceFile.ActivityDeclarations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            BoundActivity? activity = ActivityDeclarationBinder.Bind(declaration, localDiagnostics, cancellationToken);

            if (activity is not null)
            {
                activities.Add(activity);
            }
        }

        diagnosticReporter.CommitReporter(localDiagnostics);

        return new SourceFileBindResult
        {
            Activities = activities
        };
    }

    private static int GetFirstDeclarationPosition(SourceFileSyntax sourceFile)
        => sourceFile.Declarations.Count > 0
            ? sourceFile.Declarations[0].Span.Start
            : sourceFile.UnparsedContentSpan?.Start ?? sourceFile.Snapshot.SourceLength;
}
