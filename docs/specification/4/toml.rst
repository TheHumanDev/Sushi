4.1 Project and Solution syntax (TOML)
=============================================

4.1.1 TOML Foundation and Shared Schema Rules
----------------------------------------------
Sushi project files use the **.susproj** extension and Sushi solution files use the **.susln** extension.
Both are TOML-based configuration files.

TOML 1.1.0 is normative for the current compiler version. The TOML revision used to parse these files is
selected by the compiler version rather than by a project's ``language-version``. A future compiler may
explicitly adopt a later TOML revision without changing the meaning of ``language-version``.

Every syntactically valid TOML 1.1.0 construct is permitted. Sushi does not restrict equivalent TOML
spellings, but the parsed TOML data model must satisfy the applicable Sushi schema and semantic rules.

4.1.2 Project and Solution Encoding
------------------------------------
Project and solution files are encoded in UTF-8 as required by TOML. This specification depends normatively
on TOML syntax and data-model rules rather than reproducing or altering the TOML grammar.

4.1.3 Case Sensitivity
------------------------------------
Sushi-defined TOML keys and table names are case-sensitive.
