The Sushi Language Specification
=============================================
This is the comprehensive and authoritative documentation for the Sushi Programming Language.

.. note::
    This document is meant to be the official, authoritative source for Sushi.
    If the behavior of the compiler for this version of the docs does not match
    with this documentation, visit our `Issues <https://github.com/TheHumanDev/Sushi/issues>`__
    page so we can review and see which one needs to change.

.. important::
    This document describes Sushi's intended language behavior. The compiler is
    under active development, and features described here may not yet be implemented.
    Once a feature is implemented, behavior inconsistent with the specification
    should be treated as a potential defect.

Purpose
---------

The purpose of this document is to describe every facet of the language in technical
detail, to ensure that the least amount of ambiguities can exist within the compiler.
Therefore, it will often get well into the weeds of minutiae, and should not be used as
a guide to start with. For a guide, see the :doc:`Home <../index>` page.

.. toctree::
    1. Core Design Goals and Terminology <1/section>
    2. Source Text <2/section>
    3. Lexical Structure <3/section>
    4. Project and Solution Configuration <4/section>
