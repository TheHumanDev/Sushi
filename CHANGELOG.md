# Changelog

## v0.2.0-alpha

Sushi's second alpha release introduces its initial compiler analysis and workspace-aware language-server infrastructure.

### Added
- Source snapshots, UTF-8 validation, and source spans.
- Initial lexer and diagnostic reporting.
- Workspace document tracking and synchronized analysis.
- TOML project and solution configuration support.
- `.susproj` and `.susln` VS Code language definitions.
- Project-aware source membership.
- Refactored CLI command infrastructure.

### Improved
- Separation between compiler, language server, and shared core contracts.
- Analysis cancellation, stale-result protection, and test coverage.

### Limitations
Compilation and executable code generation remain unimplemented. Advanced language-server features are not yet available. Windows x64 is the only published target.

[Full v0.2.0-alpha release notes](https://github.com/TheHumanDev/Sushi/releases/tag/v0.2.0-alpha)

## v0.1.0-alpha

Initial extension and publishing proof of concept.

[Full v0.1.0-alpha release notes](https://github.com/TheHumanDev/Sushi/releases/tag/v0.1.0-alpha)
