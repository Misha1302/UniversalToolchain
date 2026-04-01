# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html) where practical.

## [Unreleased]

### Fixed

- Fixed constant-binding mutation.
- Fixed false-positive comment validation for `// /*`.
- Improved runtime component resolver assembly indexing.
- Prevented silent duplicate runtime component id overwrite during assembly component indexing.

### Added

- Introduced host-level compile-artifact access via `WistDialectExecutionHost.GetArtifactCompiler<TCompilationOutput>(mode)`.
- Added interface-based invoke helpers for `ICompiledArtifactSession`.

### Changed

- Renamed `ParametersSetter` placeholder type.
- Clarified compiled artifact mutability contract in XML documentation.
- Refactored native unary minus in native arithmetic path to use a dedicated negate method.
- Consolidated repository-level documentation around explicit canonical roles (`readme.md`, `PROJECT_RULES.md`,
  `CONTRIBUTING.md`, `AGENTS.md`).
- Updated root documentation to reinforce framework-first positioning: UniversalToolchain as reusable architecture, Wist
  as reference language.
- Added `docs/architecture-overview.md` to preserve concrete architecture details (execution model, dialect workflow,
  entry points, and risk boundaries) after consolidation.
- Removed duplicate project-overview content from legacy `project info.md` and merged key context into canonical docs.
- Added root-level `AGENTS.md` with strict AI-agent instructions focused on universality, low coupling, and anti-legacy
  change strategy.
