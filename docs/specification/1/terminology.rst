1.2 Terminology
=============================================

Sushi utilizes specific terms to refer to specific concepts,
not all of which are known outside this specification. Furthermore,
some terminology that is known is not known unilaterally across
all developers. This section defines both types for elucidation.

Linear
---------
An object, resource, or type that is *linear* is defined as something
that must be consumed exactly once. This is similar to *affine* types
which can only be used at most once, but more restrictive as it cannot
be discarded.

In other words, If something is *linear* then it must be explicitly
destroyed or transferred before going out of scope, and cannot be
destroyed or transferred again afterwards.
