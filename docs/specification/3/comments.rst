3.1 Comments
=============================================

Comments are defined as text in source code that has no effect on the semantic meaning
of the output (except for documentation comments, which can change output documentation).

.. code-block:: sushi
    :linenos:
    :caption: Example Syntax
    
    // This is a line comment.
    /* This is a block comment. */
    
    /*
     * This is a third block comment on
     * Multiple lines.
     *
     * /*
     *  * This is a nested block comment.
     *  */
     */

3.1.1 Single Line Comments
---------------------------
Ordinary single line comments begin with ``//`` and continue until the next recognized line terminator
or the end of the source file. A terminating line break is not part of the comment.

3.1.2 Block Comments
----------------------------
Block comments (also known as multiline comments) begin with an opening comment tag (``/*``) and
terminate with the corresponding closing comment tag (``*/``). Block comments may span multiple lines.

Block comments may be nested. Within an open block comment, each ``/*`` begins a nested block comment,
and each corresponding ``*/`` closes the innermost currently open block comment. Sushi defines no
language-level maximum nesting depth for nested block comments.

3.1.3 Mixing Comment Syntax
------------------------------
Within a block comment, ``//`` has no special lexical significance. Within a line comment, ``/*`` and ``*/``
have no special lexical significance.

3.1.4 Unterminated Block Comments
----------------------------------
An unterminated block comment is ill-formed. It remains a block comment lexical element and is classified
as trivia, but produces a diagnostic error.

If no matching closing delimiter (``*/``) exists, the block comment extends through the remainder of the
source. Neither the lexer nor the parser reinterprets its contents as program code in an attempt to
recover.

3.1.5 Comment Token Adjacency
--------------------------------
Comments are distinct lexical elements. Ignoring or removing a comment during later compilation stages
does not cause the source text on either side of the comment to be re-lexed or merged into a different
token.

Comments separate otherwise adjacent lexical elements. For example:

.. code-block:: sushi
    :linenos:
    :caption: Comments

    foo/* comment */bar

The above code contains two identifier tokens ``foo`` and ``bar`` separated by a block comment and is
not interpreted as the single identifier ``foobar``.

3.1.6 Comment Trivia
----------------------
Ordinary comments are preserved as trivia and excluded from the parser’s significant token stream.
Filtering them from that stream shall not cause neighboring lexical elements to be re-lexed or merged.

3.1.7 Comment Encoding
-----------------------
Comments may contain arbitrary Unicode text valid under Sushi’s UTF-8 source encoding.
