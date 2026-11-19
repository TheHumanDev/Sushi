using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Analysis;
using Sushi.Core.Diagnostics;

namespace Sushi.Core.Source;

/// <summary>
/// Represents a source document, such as a .sus file.
/// </summary>
public sealed class SourceDocument
{
    /// <summary>
    /// Synchronizes access to the source document.
    /// </summary>
    private readonly Lock syncRoot = new();

    /// <summary>
    /// The snapshot of the source directly from disk.
    /// </summary>
    private SourceSnapshot diskSnapshot;

    /// <summary>
    /// The snapshot of the source as it exists in-memory. Used for open documents with unsaved changes.
    /// </summary>
    private SourceSnapshot? editorSnapshot;

    /// <summary>
    /// The current analysis result for the source document.
    /// </summary>
    private AnalysisResult? analysis;

    /// <summary>
    /// The current accumulated diagnostics for the source document.
    /// </summary>
    private IReadOnlyList<SushiDiagnostic>? diagnostics;

    /// <summary>
    /// The current snapshot for the source document.
    /// </summary>
    public SourceSnapshot CurrentSnapshot
    {
        get
        {
            using (this.syncRoot.EnterScope())
            {
                return this.editorSnapshot ?? this.diskSnapshot;
            }
        }
    }

    /// <summary>
    /// True if the source document is open in the editor. False otherwise.
    /// </summary>
    public bool IsOpen
    {
        get
        {
            using (this.syncRoot.EnterScope())
            {
                return this.editorSnapshot is not null;
            }
        }
    }

    /// <summary>
    /// The document <see cref="System.Uri"/>.
    /// </summary>
    public Uri Uri { get; }

    /// <param name="uri">
    /// The document <see cref="System.Uri"/>.
    /// </param>
    /// <param name="diskSnapshot">
    /// The current disk snapshot for the source document.
    /// </param>
    public SourceDocument([NotNull] Uri uri, [NotNull] SourceSnapshot diskSnapshot)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(diskSnapshot);

        if (!uri.IsSamePath(diskSnapshot.Uri))
        {
            throw new ArgumentException("Snapshot belongs to a different document.", nameof(diskSnapshot));
        }

        this.diskSnapshot = diskSnapshot;
        this.Uri = uri;
    }

    /// <summary>
    /// Updates the disk snapshot.
    /// </summary>
    /// <param name="snapshot">
    /// The new snapshot.
    /// </param>
    public void UpdateDisk([NotNull] SourceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!this.Uri.IsSamePath(snapshot.Uri))
        {
            throw new ArgumentException("Snapshot belongs to a different document.", nameof(snapshot));
        }

        using (this.syncRoot.EnterScope())
        {
            this.diskSnapshot = snapshot;
        }
    }

    /// <summary>
    /// Updates the editor snapshot. Versioned open documents accept only newer editor versions; closing resets that lifecycle.
    /// </summary>
    /// <param name="snapshot">
    /// The new snapshot.
    /// </param>
    public void UpdateEditor([NotNull] SourceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!this.Uri.IsSamePath(snapshot.Uri))
        {
            throw new ArgumentException("Snapshot belongs to a different document.", nameof(snapshot));
        }

        using (this.syncRoot.EnterScope())
        {
            if (this.editorSnapshot?.Version is int currentVersion
                && (snapshot.Version is not int incomingVersion || incomingVersion <= currentVersion))
            {
                return;
            }

            this.editorSnapshot = snapshot;
        }
    }

    /// <summary>
    /// Closes the editor snapshot.
    /// </summary>
    public void Close([NotNull] SourceSnapshot diskSnapshot)
    {
        ArgumentNullException.ThrowIfNull(diskSnapshot);

        if (!this.Uri.IsSamePath(diskSnapshot.Uri))
        {
            throw new ArgumentException("Snapshot belongs to a different document.", nameof(diskSnapshot));
        }

        using (this.syncRoot.EnterScope())
        {
            this.diskSnapshot = diskSnapshot;
            this.editorSnapshot = null;
        }
    }

    /// <summary>
    /// Returns whether the specified snapshot is the same as the current snapshot for this source document.
    /// </summary>
    /// <param name="snapshot">
    /// The snapshot to check.
    /// </param>
    /// <returns>
    /// True if the two snapshots are the same. False otherwise.
    /// </returns>
    public bool IsCurrent(SourceSnapshot snapshot) => ReferenceEquals(this.CurrentSnapshot, snapshot);

    /// <summary>
    /// Attempts to update the analysis of the source document.
    /// </summary>
    /// <param name="analysis">
    /// The new analysis result.
    /// </param>
    /// <param name="diagnosticReporter">
    /// The diagnostics reporter containing the accumulated diagnostics.
    /// </param>
    /// <returns>
    /// True if the analysis was able to be updated. False otherwise.
    /// </returns>
    public bool TryUpdateAnalysis(AnalysisResult analysis, IDiagnosticReporter diagnosticReporter)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(diagnosticReporter);

        using (this.syncRoot.EnterScope())
        {
            SourceSnapshot currentSnapshot = this.editorSnapshot ?? this.diskSnapshot;

            if (!ReferenceEquals(currentSnapshot, analysis.Snapshot))
            {
                return false;
            }

            this.analysis = analysis;
            this.diagnostics = diagnosticReporter.ReportDiagnostics();

            return true;
        }
    }

    /// <summary>
    /// Attempts to retrieve the current analysis result and diagnostics.
    /// </summary>
    /// <param name="result">
    /// The analysis result. If the return value is false, this is null.
    /// </param>
    /// <param name="diagnostics">
    /// The diagnostics. If the return value is false, this is null.
    /// </param>
    /// <returns>
    /// True if the analysis was successfully retrieved. False otherwise.
    /// </returns>
    public bool TryGetCurrentAnalysis([NotNullWhen(true)] out AnalysisResult? result, [NotNullWhen(true)] out IReadOnlyList<SushiDiagnostic>? diagnostics)
    {
        using (this.syncRoot.EnterScope())
        {
            SourceSnapshot currentSnapshot = this.editorSnapshot ?? this.diskSnapshot;

            if (this.analysis is null || this.diagnostics is null || !ReferenceEquals(this.analysis.Snapshot, currentSnapshot))
            {
                result = null;
                diagnostics = null;
                return false;
            }

            result = this.analysis;
            diagnostics = this.diagnostics;

            return true;
        }
    }
}
