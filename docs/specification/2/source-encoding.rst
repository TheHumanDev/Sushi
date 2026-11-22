2.1 Source Encoding
=============================================

A Sushi source file shall be encoded as UTF-8.

2.1.1 Byte-Order Marks
-----------------------
A UTF-8 byte-order mark (BOM) may appear at the beginning of a source file.
When present, the BOM is silently ignored and has no lexical, semantic, or
observable effect.

For otherwise identical source text, the presence or absence of a UTF-8 BOM
does not affect compilation results.

A source file containing an invalid UTF-8 byte sequence is ill-formed and 
produces a compilation diagnostic.

2.1.2 Unicode
--------------
Sushi source text may contain Unicode data in constructs whose contents represent
textual or character data. Unicode is permitted in string literals, character
literals, ordinary comments, and documentation comments.

Sushi does not implicitly Unicode-normalize textual literal contents. The compiler
shall preserve the Unicode sequence represented by the source and shall not silently
apply NFC, NFD, NFKC, NFKD, or any other Unicode normalization form. Canonically
equivalent Unicode sequences are not automatically made identical by the language.
