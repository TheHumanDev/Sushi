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

Single Line Comments
---------------------
Ordinary single line comments begin with ``//`` and continue until the next recognized line terminator or the end of the source file. A terminating line break is not part of the comment.

Block Comments
-------------------
Block comments (also known as multiline comments) begin with an opening comment tag (``/*``) and terminate with the corresponding closing comment tag (``*/``). Block comments may span multiple lines.

Block comments may be nested. Within an open block comment, each ``/*`` begins a nested block comment, and each corresponding ``*/`` closes the innermost currently open block comment. Sushi defines no language-level maximum nesting depth for nested block comments.

Mixing Comment Syntax
----------------------
Within a block comment, ``//`` has no special lexical significance. Within a line comment, ``/*`` and ``*/`` have no special lexical significance.
