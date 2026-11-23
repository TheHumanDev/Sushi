2.2 Non-Printable Characters
=============================================

2.2.1 Line Terminators
-----------------------
Sushi source text recognizes the following as line terminators:
- LF (U+000A)
- CRLF (U+000D U+000A)
- Lone CR (U+000D)

Each recognized line terminator represents exactly one logical line break.
A CRLF sequence is treated as one line terminator, not two.

The choice among supported line-terminator representations has no semantic
effect on the program. Otherwise-equivalent source does not acquire different
language semantics merely because LF, CRLF, or lone CR is used.

2.2.2 Whitespace
------------------
Outside of literals and other constructs whose contents are defined as textual data,
Sushi recognizes only U+0009 HORIZONTAL TAB, U+0020 SPACE, and recognized line
terminators as whitespace.

No other Unicode character is treated as source whitespace. This exclusion includes
non-breaking space, zero-width space, Unicode separator characters, form feed,
vertical tab, and other Unicode whitespace or formatting characters.

An otherwise unsupported Unicode whitespace or formatting character appearing where
no lexical construct permits it makes the source ill-formed and produces a compilation
diagnostic.

Unicode whitespace characters may appear as data inside constructs that permit arbitrary
Unicode textual content, including string literals and comments.

Whitespace and recognized line terminators are lexically recognized and preserved as trivia.
They are excluded from the parser’s significant token stream. Their exclusion does not remove
them from the source model and shall not cause neighboring tokens to be re-lexed or merged
Whitespace is syntactically insignificant unless a later explicitly specified feature states otherwise.
