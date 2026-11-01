---
title: Composition and Plan Explainability
description: Current explain surface for deterministic LanguagePlan compilation.
---

# Composition and plan explainability

UniversalToolchain uses one planning authority and one explainable result.

`LanguageCompiler` returns `LanguageBuildResult`: planning diagnostics plus an immutable `LanguagePlan` on success. The plan exposes resolved features/contributions, runtime-provider identity, backend artifact routes, runtime policy, `LanguagePlanSummary` and `PlanHash`; `LanguageLockFile` is the stable machine-readable projection.

A useful report should show language/version, entry artifact, exact package identity, selected features/contributions, runtime provider, one ordered route per backend, runtime policy, diagnostics and plan hash.

## Authority boundary

- package registry and `LanguageCompiler` own planning truth;
- `LanguagePlan` owns selected execution truth;
- `LanguageRuntime` verifies and materializes exactly that plan;
- formatted explanations are projections only.

The former dialect-integration explanation/runtime-selection graph was removed in S13 rather than retained as a parallel source of truth.

## What the plan actually establishes

Configuration-time checks can reject unsatisfied dependencies, conflicting single-owner slots, ambiguous providers and routes, invalid typed artifact connections, or impossible pass ordering. The successful plan fixes the declared selection and package provenance. `LanguageRuntime` verifies and materializes matching components; it does not prove that independently written compiler passes preserve meaning.

The **package feature manifest** is schema v5 (`LanguageFeatureManifestSerializer`). The **language lock snapshot** is schema v6 (`LanguageLockFile`). These two projections describe different identities; neither is a certificate of arbitrary extension semantics. See [planning diagnostics](/language-authoring/contribution-planning) and [limitations](/limitations).
