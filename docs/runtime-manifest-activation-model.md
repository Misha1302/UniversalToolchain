# Runtime Manifest Activation Model

> **Status: compatibility / historical infrastructure, not the canonical Wist execution model.**
>
> The current Wist production path is documented in [Current Canonical Runtime Pipeline](./current-canonical-runtime-pipeline.md): `LanguageDefinition -> LanguageCompiler -> LanguagePlan -> LanguageRuntime`.

## What remains

The repository still contains generic dialect runtime-manifest serializer/emitter infrastructure and tests for its deterministic artifact contracts. Those artifacts can describe component metadata for compatibility, tooling or non-canonical integration scenarios.

Keeping that format alive does **not** make it a semantic owner for current Wist execution.

## What S11 retired for Wist

Current Wist execution no longer performs this sequence:

```text
DialectBuildPlan
  -> SelectedRuntimePlan
  -> manifest-backed Wist runtime selection
  -> WistDialectExecutionHost
```

The old Wist planner/runtime-selection owners were physically removed in S11 and permanent architecture guards reject their return.

## Current Wist activation story

For Wist, activation is plan-backed rather than manifest-selected:

1. Wist configuration is translated to `LanguageDefinition`.
2. `LanguageCompiler` closes dependencies and produces the only semantic `LanguagePlan`.
3. The plan records exact package identity, contribution identity and backend artifact routes.
4. `LanguageRuntime` binds only the materialized runtime graph to exact package/component sources.
5. Wist implementation factories instantiate the components already selected by the plan.

Runtime materialization validates exact package id/version/manifest/implementation provenance. It does not scan a manifest catalog to choose a different set of Wist modules or backends.

## Runtime-manifest artifact boundary

When working on the retained generic runtime-manifest infrastructure:

- keep serialization/emission deterministic;
- preserve explicit assembly/type identities;
- validate malformed/duplicate entries fail closed;
- do not make reflection enumeration order semantic;
- do not route Wist public execution back through manifest-selected component planning;
- keep runtime-manifest tests separate from canonical Wist `LanguagePlan` tests.

## Contributor rule

A runtime manifest may describe an implementation artifact. It must not become a second source of Wist semantic truth alongside `LanguagePlan`.
