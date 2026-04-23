# Current Canonical Runtime Pipeline

This document describes the currently supported runtime composition flow in this repository.
It is intentionally limited to behavior that exists today.

## Canonical pipeline

1. **Dialect DSL compilation** — compile `.wistdialect` text or file into a dialect model.
2. **Build-plan projection** — convert the dialect model into `DialectBuildPlan`.
3. **Manifest-backed runtime selection** — resolve a deterministic `SelectedRuntimePlan` from runtime manifests.
4. **Wist execution configuration** — map the selected runtime plan to `WistDialectExecutionConfiguration`.
5. **Host creation with selected activation only** — create `WistDialectExecutionHost` from that configuration and activate only selected runtime components/backends; backend registrars are resolved from selected backend manifest entries.
6. **Execution** — run source text through the created host in the selected mode.

## Canonical runtime constraints

- Runtime selection is completed before host creation.
- Host creation depends on the selected runtime surface, not on full-catalog eager discovery.
- Reflection-based resolution remains part of runtime infrastructure, but in the canonical path it is targeted exact activation of selected types, not broad eager assembly/type discovery.
- Known backends exposed for execution are derived from selected backend entries in the runtime plan.

## Ownership boundary

- `UniversalToolchain.Dialects.Integration` owns **generic runtime infrastructure** bootstrap:
  - file-system runtime catalog registrations,
  - reflection-based runtime resolution registrations,
  - selected-runtime activation classification and backend runtime configuration projection,
  - intrinsic semantic bootstrap contracts and two-phase validation helpers.
- `UniversalToolchain.Dialects.Wist` owns **Wist-specific orchestration**:
  - Wist workflow composition,
  - Wist execution configuration building,
  - Wist host/provider creation.

`AddWistDialectServices()` remains the canonical convenience method for Wist and composes Wist core services with the
generic Integration runtime infrastructure blocks. Canonical shipped paths do not require explicit backend registrar
imports; compatibility helpers may still register backend registrars for older/manual wiring, but manifest-selected exact
activation is the default runtime path.
