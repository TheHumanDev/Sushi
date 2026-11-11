# Contributing to Sushi

Thank you for your interest in contributing to the Sushi programming language.

Sushi is an experimental, strongly typed systems programming language focused on memory safety, predictable behavior, and giving developers low-level control without unnecessary complexity.

Contributions of all sizes are welcome, from documentation improvements and bug fixes to compiler enhancements and language design proposals.

# Getting Started

Before contributing, please familiarize yourself with the project's
[README](https://github.com/TheHumanDev/Sushi/blob/master/README.md),
[Code of Conduct](https://github.com/TheHumanDev/Sushi/blob/master/.github/CODE_OF_CONDUCT.md),
and [Security Policy](https://github.com/TheHumanDev/Sushi/blob/master/.github/SECURITY.md).

External contributors should fork the repository, create a development branch, and submit a pull request against the official repository. Direct repository write access is reserved for authorized maintainers.

# Reporting and Tracking Issues

Use the repository's GitHub Issue Forms to report bugs, documentation problems, or actionable implementation requests.

Maintainers evaluate and prioritize issues according to the [Issue Triage and Lifecycle Guide](https://github.com/TheHumanDev/Sushi/blob/master/.github/TRIAGE.md). The guide explains issue labels, acceptance, duplicate handling, regressions, and closure policies.

An accepted issue does not necessarily indicate that implementation has been scheduled.

# Making Changes

Not every contribution requires prior discussion. Small, self-contained improvements, such as documentation corrections, straightforward bug fixes, tests, and minor implementation changes, may be submitted directly through pull requests.

Substantial changes should be discussed before implementation. Examples include:

- New language features or changes to existing language semantics.
- Significant compiler architecture changes.
- Major refactors affecting multiple compiler subsystems.
- Changes to fundamental ownership, concurrency, or memory-safety behavior.
- Changes with significant compatibility implications.

Use GitHub Discussions to propose and discuss these changes. Discussion and community feedback help inform the project's direction, but proposals do not automatically become accepted language features.
Official language design decisions remain under the authority of the project maintainers and must be reflected in the language specification when applicable.

# Development Standards

Contributions should follow the existing conventions and architecture of the repository:

- Follow the repository's .editorconfig and established formatting conventions.
- Prefer clear, focused, maintainable implementations.
- Preserve existing behavior unless an intentional change has been accepted.
- Avoid unrelated refactors or formatting changes within a contribution.
- Document meaningful changes to public APIs, language behavior, or developer-facing functionality.

Additional development documentation may be introduced as the project evolves.

# Testing
Contributions should include testing appropriate to the change.

Bug fixes should include regression tests where practical. New features should include relevant automated tests, and behavioral changes should update existing tests when necessary.
Tests should always be reviewed for staleness. In other words, if the feature changes an assumption or removes a part of the code, then a test or even multiple tests may be overcome by events.
When writing tests, be thorough. A compiler leaves little room for error.

Documentation-only changes do not require unrelated code tests.

Before submitting a pull request, ensure all of your tests pass. If you add, modify, or remove an existing feature, that feature needs to be run and debugged. Do not simply rely on passing tests.
Tests make it easy to catch bugs, but not every bug is easily caught by tests.

Maintainers may exercise discretion when specific testing or documentation requirements do not reasonably apply.

# Commit Requirements
All commits entering the official repository must have valid cryptographic signatures recognized by GitHub. Contributors may use GitHub-supported GPG, SSH, or S/MIME signing.
See GitHub's [commit signature verification documentation](https://docs.github.com/en/authentication/managing-commit-signature-verification) for setup instructions. If any of
the commits in your pull request are not signed, **they will be rejected**.

# Preserving Git History
Sushi values accurate, traceable development history. Pull requests are integrated using merge commits rather than squash or rebase merging. Contributors should preserve
meaningful individual commits and avoid unnecessarily rewriting published commit history during review. When practical, address review feedback through additional commits.
If rewriting commits is necessary to resolve signing problems or other exceptional circumstances, coordinate with the maintainers.

# Submitting Pull Requests
When submitting a pull request:
- Clearly explain what changed and why.
- Reference relevant issues or discussions where applicable.
- Describe the testing performed.
- Include relevant documentation updates.
- Identify compatibility considerations or known limitations when applicable.

Pull requests should remain reasonably focused. Larger contributions may be divided into logically distinct changes when doing so improves reviewability.
Always submit a pull request to merge into the correct working version. If you are working on a hotfix for a specific version, then that pull request should
target the branch with that version name. For example, if you are working on a hotfix for `v0.29.3-alpha`, then your pull request should target `v0.29.3-alpha`.
Only the maintainers may target `master` with a pull request during the release process.

Maintainers review contributions for correctness, maintainability, compatibility, security, and alignment with the project's direction. Acceptance and merging are
at the discretion of authorized maintainers.

# Licensing
Sushi is licensed under the Apache License, Version 2.0. By submitting a contribution, you confirm that you have the necessary rights to submit it under the
Apache License 2.0 and understand that contributions are subject to its terms. Contributors must not submit code, documentation, or other material they are not
authorized to contribute.

# Security Vulnerabilities
Please do not publicly disclose suspected security vulnerabilities through issues, discussions, or pull requests.
Instead follow the private reporting procedures described in the [Security Policy](https://github.com/TheHumanDev/Sushi/blob/master/.github/SECURITY.md).

# Code of Conduct
All contributors are expected to follow Sushi's [Code of Conduct](https://github.com/TheHumanDev/Sushi/blob/master/.github/CODE_OF_CONDUCT.md).
Technical disagreements are welcome. Respectful communication, constructive criticism, and good-faith collaboration are expected.

Thank you for helping us improve Sushi.
