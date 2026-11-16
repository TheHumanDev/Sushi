2.1 Source Encoding
=============================================

A Sushi source file shall be encoded as UTF-8.

A UTF-8 byte-order mark (BOM) may appear at the beginning of a source file.
When present, the BOM is silently ignored and has no lexical, semantic, or
observable effect.

For otherwise identical source text, the presence or absence of a UTF-8 BOM
shall not affect compilation results.

A source file containing an invalid UTF-8 byte sequence is ill-formed and
shall produce a compilation diagnostic.
