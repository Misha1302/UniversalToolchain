# Current Canonical Runtime Pipeline

This document describes the currently supported runtime composition flow in this repository.
It is intentionally limited to behavior that exists today.

## Canonical pipeline

1. **Dialect DSL compilation** — compile `.wistdialect` text or file into a dialect model.
2. **Build-plan projection** — convert the dialect model into `DialectBuildPlan`.
3. **Runtime selection** — resolve a deterministic `SelectedRuntimePlan` from runtime manifests.
4. **Wist execution configuration** — map the selected runtime plan to `WistDialectExecutionConfiguration`.
5. **Host creation** — create a `WistDialectExecutionHost` from that configuration.
6. **Explicit backend registration** — backend runtime registrars must be added explicitly before host creation.
7. **Execution** — run source text through the created host in the selected mode.

## Ownership boundary

- `UniversalToolchain.Dialects.Integration` owns **generic runtime infrastructure** bootstrap:
  - file-system runtime catalog registrations,
  - reflection-based runtime resolution registrations.
- `UniversalToolchain.Dialects.Wist` owns **Wist-specific orchestration**:
  - Wist workflow composition,
  - Wist execution configuration building,
  - Wist host/provider creation.

`AddWistDialectServices()` remains the canonical convenience method for Wist and composes Wist core services with the generic Integration runtime infrastructure blocks.
