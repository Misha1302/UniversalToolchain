# Documentation authority index

This repository separates public current documentation from repository-only policies, proposals, reviews and historical material.

## Authority order

| Tier | Location | Meaning |
|---|---|---|
| Executable truth | source, tests, build scripts and produced artifacts | strongest evidence for current behavior |
| Public current contracts | `docs/CURRENT_ARCHITECTURE_STATUS.md`, `docs/SECURITY.md`, `docs/limitations.md`, `docs/reference/`, `docs/architecture/` | current behavior and user-facing boundaries |
| Repository policies | `internal-docs/policies-and-reports/PROJECT_RULES.md`, `ARCHITECTURE_RULES.md`, `DOCUMENTATION_RULES.md`, `SYNTAX_OWNERSHIP_RULES.md` | coding, architecture and documentation constraints for contributors and agents |
| Evidence and releases | `docs/evidence/`, `docs/releases/`, `VERIFICATION.md` | claims tied to a named verification baseline or release |
| Proposals | `internal-docs/proposals/` | design input only until implemented and tested |
| Dated reviews | `internal-docs/reviews/` | review snapshots; findings may be superseded |
| Archive and talks | external history/evidence bundle and `internal-docs/vision/` | historical or presentation context only |

## Required reading by task

### Public API, onboarding or documentation

1. `docs/index.md`
2. the role-specific route under `docs/start/`, `docs/language-authoring/`, `docs/build-dsls/` or `docs/write-modules/`
3. `docs/SECURITY.md` and `docs/limitations.md` when claims touch trust or maturity
4. `internal-docs/policies-and-reports/DOCUMENTATION_RULES.md`

### Architecture-affecting code changes

1. `AGENTS.md`
2. `internal-docs/policies-and-reports/PROJECT_RULES.md`
3. `internal-docs/policies-and-reports/ARCHITECTURE_RULES.md`
4. `internal-docs/policies-and-reports/SYNTAX_OWNERSHIP_RULES.md` when syntax or parser ownership changes
5. `docs/CURRENT_ARCHITECTURE_STATUS.md`
6. the subsystem owner page under `docs/architecture/` or `docs/reference/`

### External Language Authoring SDK

1. `docs/language-authoring/`
2. `docs/architecture/external-language-authoring-sdk.md`
3. `docs/reference/lifecycle-concurrency-privacy.md`
4. `docs/evidence/language-authoring-alpha.md`

### PlanFuzz research tooling

1. `internal-docs/proposals/planfuzz/README.md`
2. `internal-docs/proposals/planfuzz/technical-specification.ru.md`
3. `internal-docs/proposals/planfuzz/implementation-status.md`
4. `docs/CURRENT_ARCHITECTURE_STATUS.md`
5. PlanFuzz source and focused tests

### Wist compiler/module changes

1. `docs/write-modules/`
2. `docs/reference/module-contracts.md`
3. `docs/architecture/lowering-walkthrough.md`
4. `internal-docs/policies-and-reports/SYNTAX_OWNERSHIP_RULES.md`

## Split rules

- Public task-oriented and reference material belongs under `docs/`.
- Repository-only policies and maintainer instructions belong under `internal-docs/policies-and-reports/` or `internal-docs/maintainers/`.
- Future designs belong under `internal-docs/proposals/`.
- Dated reviews belong under `internal-docs/reviews/`.
- Historical snapshots belong in Git history or a detached history/evidence bundle.
- Conference material belongs in a separate talks/release asset bundle.

Moving a document requires updating Markdown links, inline repository paths, navigation, authority indexes and documentation checks in the same change.

## Conflict rule

When documentation and implementation disagree, do not silently choose either. Identify the current source of truth, update the canonical owner and add a test or deterministic documentation check where practical.
