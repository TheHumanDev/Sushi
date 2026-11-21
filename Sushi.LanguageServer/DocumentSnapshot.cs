using Sushi.Core.Source;

namespace Sushi.LanguageServer;

/// <summary>
/// Represents a snapshot of a specific source document.
/// </summary>
/// <param name="Document">
/// The document that the snapshot originated from.
/// </param>
/// <param name="Snapshot">
/// The snapshot of the source document.
/// </param>
internal sealed record DocumentSnapshot(SourceDocument Document, SourceSnapshot Snapshot);
