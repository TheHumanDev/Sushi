using Sushi.Core.Parsing.Syntax;

namespace Sushi.Core.Parsing;

/// <summary>
/// Represents the syntax tree.
/// </summary>
/// <param name="Tree">
/// The concrete syntax tree produced by the parser.
/// </param>
public sealed record ParserResult(IConcreteSyntaxTree Tree);
