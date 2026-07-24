---
title: Current Architecture Status
description: Current implemented surfaces, experimental areas and tied verification baseline.
---

# Current architecture status

This page describes the supplied language-authoring hardening baseline. Historical cycle notes are stored under `internal-docs/` and are not current architecture authority.

## Current public/product surface

`UniversalToolchain.Wist` `0.1.0-alpha.1` provides:

- `WistEngine`;
- restricted arithmetic and broader native presets;
- `Validate`, `Evaluate`, `Compile<TDelegate>` and `TryCompile<TDelegate>`;
- structured diagnostics and preflight limits;
- typed compiled program metadata;
- optional observable Wist `AIR -> SSA -> AIR` optimization route.

## Current generic language-authoring surface

The repository implements:

- `UniversalToolchain.Language.Abstractions` for stable IDs, policies and typed artifact contracts;
- `UniversalToolchain.FeatureSdk` for features and contribution metadata;
- `UniversalToolchain.LanguageSdk` for package registry, deterministic planning, `LanguagePlan` and schema-v5 lock serialization;
- `UniversalToolchain.LanguageAuthoring` for coupled descriptor/runtime registration builders;
- `UniversalToolchain.Runtime` for route assembly, exact executor selection, policy validation and lifecycle;
- `UniversalToolchain.Testing` for backend parity helpers;
- `UniversalToolchain.Templates` with `ut-language`;
- `UniversalToolchain.Wist.LanguagePack` as the Wist compatibility boundary;
- `samples/Acme.PricingLanguage` as a fully non-Wist sample.

## Implemented generic contracts

- typed entry artifacts and route endpoints;
- independently owned features and contributions;
- dependencies, conflicts, capabilities and explicit slot replacement;
- package-level contributions;
- minimum-cost conversion routes;
- deterministic same-artifact pass ordering;
- planning-only definitions;
- exact package-manifest binding;
- exact `(contribution, backend, input contract)` executor resolution;
- `PerSession` and explicit `SingletonStateless` component lifetimes;
- synchronous/asynchronous disposal coordinated with in-flight operations;
- runtime gates for determinism and host interop.

## Wist compiler/runtime architecture

The reference language retains the explicit pipeline:

```text
Source/Text -> Lexer/Parser -> AST -> Bytecode -> AIR -> Optimization -> Backend -> Execution
```

Bytecode and AIR remain separate semantic boundaries. Wist runtime selection goes through dialect build plans, manifests and capabilities. Interpreter/CIL parity remains required for shared supported behavior.

## Experimental PlanFuzz research tooling

The repository's stacked research branches include non-packable PlanFuzz Phase 0–1 tooling for configuration-aware differential testing:

- a language-neutral deterministic testcase, observation and oracle core;
- independent Acme and Wist adapters with adapter-owned structured generators;
- Acme interpreter/compiled and registry-order variants;
- Wist interpreter/compiler plus SSA `Disabled`, `Prefer` and `Require` variants;
- backend-parity, optimization-route-parity, plan-determinism, controlled-fallback and canonical-lock oracles;
- typed decimal and `Int32` observations;
- Wist route/fallback evidence without Wist-specific diagnostic ownership in the generic core;
- fresh-process execution, bounded timeout/output handling, replay and recursive artifact manifests;
- exact replay fingerprints separated from coarser campaign-triage class fingerprints;
- test-only Acme seeded faults kept separate from current Wist findings;
- a configuration-complete `UniversalToolchain/PlanFuzz.sln` built by canonical repository entrypoints.

This tooling is not part of the public Wist package and does not establish publication claims. The Wist pilot exposed reproducible current defects tracked in #302, #303 and #307, but normalized finding classes are not treated as proven root causes. Lifecycle, negative-surface and reducer stages remain incomplete.

## Experimental or incomplete

- low-level generic SDK compatibility is alpha;
- high-level grammar, binder, type-system and operation authoring are not provided;
- Wist SSA is opt-in and verifier-gated, not the default backend;
- generic third-party package version negotiation is narrower than a mature ecosystem resolver;
- runtime component traits are package attestations, not hostile-extension proof;
- Wist Bytecode tag verification remains incomplete;
- no hardened in-process sandbox is claimed.

## Verification identity

The supplied `VERIFICATION.md` records:

```text
85 / 85 solution projects built
1,411 tests succeeded
0 failed
0 skipped
9 package outputs checked
```

See [Current Verification](/evidence/current-verification) for the evidence boundary.
