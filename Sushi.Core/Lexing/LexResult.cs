using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Lexing.Tokenization;
using Sushi.Core.Source;

namespace Sushi.Core.Lexing;

/// <summary>
/// Represents a result returned by an <see cref="ILexer"/> when performing lexical analysis on a source snapshot.
/// </summary>
/// <param name="Snapshot">
/// The source snapshot that was lexed.
/// </param>
/// <param name="Tokens">
/// The <see cref="LexToken"/> objects produced during the lexing pass.
/// </param>
public sealed record LexResult([NotNull] SourceSnapshot Snapshot, [NotNull] IReadOnlyList<LexToken> Tokens);
