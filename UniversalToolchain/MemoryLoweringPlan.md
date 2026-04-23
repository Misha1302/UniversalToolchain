# Plan: lazy loading only required modules to reduce memory usage

> **Status and scope notice (historical/migration document):** This file tracks migration work and partial implementation history. It is not the canonical source for current runtime behavior. For current truth, use `readme.md` and `docs/current-canonical-runtime-pipeline.md`.

## Current state summary

### Implemented current behavior

- The normal Wist dialect path is manifest-backed and selection-driven.
- Dialect source is compiled into a build plan before runtime host creation.
- `ComposeText` and `ComposeFile` produce a selected runtime plan and do not create a host.
- `CreateHost` builds runtime provider state from the selected plan.
- Backend aliases such as `compiler` resolve to canonical backend aliases declared in manifests.

### Partially implemented behavior

- Runtime manifests, file-based catalog loading, and exact activation metadata are in place.
- Reflection-based infrastructure remains in use for exact selected-type activation and backend registrar resolution.
- Compatibility/eager discovery helpers still exist for older/manual wiring paths.

### Future work

- Additional selective assembly loading for unloaded feature packs.
- Memory baselines and regression thresholds for composition/host-creation paths.
- Broader cleanup of eager discovery APIs where this does not break compatibility contracts.
- Descriptor/deployment improvements beyond current Wist validation scope.

## Goal

Reduce memory usage during dialect composition and runtime startup while preserving the canonical selection-driven flow:

~~~text
discover everything -> register everything -> filter
~~~

toward:

~~~text
parse dialect -> build plan -> resolve selected components from manifests -> activate selected runtime surface
~~~

The existing eager path remains a compatibility path and is not the canonical runtime execution story.

---

## Historical migration notes

The sections below are retained as migration planning notes and implementation sketches. Treat them as historical context unless a point is explicitly reflected in canonical docs.
