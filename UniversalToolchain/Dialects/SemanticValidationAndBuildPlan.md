# Semantic validation and build-plan flow

## Stage contract

- Input: parsed or compiled dialect directives.
- Output: `DialectBuildPlan` + `DialectValidationResult`.

`DialectBuildPlan` is the semantic contract used by downstream runtime resolution.

## What semantic normalization is responsible for

- normalize module include/exclude directives
- normalize backend enable/disable directives
- normalize intrinsic and optimizer policies
- apply deterministic ordering constraints
- emit explicit diagnostics for contradictions/cycles

## Determinism model

- stable ordinal sorting is used where set-like data must be projected
- ordering constraints are resolved deterministically
- diagnostics are explicit instead of hidden conflict resolution

## Important non-goals in semantic stage

- no runtime descriptor lookup
- no DI container mutation
- no module activation
- no backend/intrinsic implementation discovery

These concerns are intentionally deferred to the integration stage.
