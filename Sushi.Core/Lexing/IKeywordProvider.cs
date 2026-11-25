using System.Diagnostics.CodeAnalysis;
using Sushi.Core.Lexing.Tokenization;
using Sushi.Core.Source;

namespace Sushi.Core.Lexing;

/// <summary>
/// Used as an authoritative source of legal keywords, and aids
/// in matching raw source bytes to their representative forms.
/// </summary>
public interface IKeywordProvider
{
    /// <summary>
    /// The list of legal keywords.
    /// </summary>
    public IReadOnlyList<Keyword> Keywords { get; }

    /// <summary>
    /// Returns whether the specified token is an access modifier with respect to the given source snapshot.
    /// </summary>
    /// <param name="token">
    /// The token to check.
    /// </param>
    /// <param name="snapshot">
    /// The source snapshot that the token originated from.
    /// </param>
    /// <returns>
    /// True if the token is an access modifier. False otherwise.
    /// </returns>
    public bool IsAccessModifier(LexToken token, [NotNull] SourceSnapshot snapshot);

    /// <summary>
    /// Returns whether the specified token is a built-in type with respect to the given source snapshot.
    /// </summary>
    /// <param name="token">
    /// The token to check.
    /// </param>
    /// <param name="snapshot">
    /// The source snapshot that the token originated from.
    /// </param>
    /// <returns>
    /// True if the token is a built-in type. False otherwise.
    /// </returns>
    public bool IsBuiltInType(LexToken token, [NotNull] SourceSnapshot snapshot);

    /// <summary>
    /// Returns whether the specified spelling is a keyword reserved by the provider.
    /// </summary>
    /// <param name="spelling">
    /// The complete source spelling.
    /// </param>
    /// <returns>
    /// True if the keyword is a reserved keyword. False otherwise.
    /// </returns>
    public bool IsReservedKeyword(ReadOnlySpan<byte> spelling);
}
