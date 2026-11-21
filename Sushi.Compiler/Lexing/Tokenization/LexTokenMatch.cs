using Sushi.Core.Diagnostics;

namespace Sushi.Compiler.Lexing.Tokenization;

/// <summary>
/// Describes a lexical element recognized by a tokenizer before it is committed to the lexical and diagnostic streams.
/// </summary>
/// <param name="Type">
/// The type of the recognized element.
/// </param>
/// <param name="Length">
/// The number of source bytes consumed by the recognized element.
/// </param>
/// <param name="DiagnosticReporter">
/// The diagnostic reporter produced while recognizing the element, or null when recognition produced no diagnostics.
/// </param>
public readonly record struct LexTokenMatch(LexTokenType Type, int Length, IDiagnosticReporter? DiagnosticReporter = null);
