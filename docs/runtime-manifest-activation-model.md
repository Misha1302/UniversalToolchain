# Runtime Manifest Activation Model

This document defines the canonical runtime activation path used by dialect execution today.

## Canonical execution story

Canonical dialect execution is selection-driven and manifest-backed:

1. Compile dialect text/file into a `DialectBuildPlan`.
2. Resolve a `SelectedRuntimePlan` from runtime manifests (catalog-known components -> selected components).
3. Build Wist execution configuration from the selected plan.
4. Create host and activate only the selected runtime surface.
5. Execute code.

`ComposeText`/`ComposeFile` perform steps 1-2 and do not create the host. `CreateHost(...)` performs activation from the already selected runtime plan.

## Runtime model boundaries

- **Catalog-known components**: all runtime components exposed by loaded manifests.
- **Selected runtime plan**: deterministic subset required by the dialect build plan.
- **Exact activation**: selected entries are activated from explicit type references in manifest activation metadata.
- **Backend registrar activation**: selected backend entries resolve exact registrar types and activate only those registrars.

Known backends in execution configuration are derived from the selected backends, not from the full catalog.

## Reflection in the canonical path

Reflection is still part of the runtime infrastructure, but its role is constrained:

- centralized in integration/runtime infrastructure,
- scoped to selected manifest entries,
- used for exact type activation and backend registrar resolution.

Broad eager assembly/type discovery is not the canonical runtime execution story.
Compatibility and eager discovery helpers may exist for legacy/manual wiring, but they are not the source of truth for dialect composition.
