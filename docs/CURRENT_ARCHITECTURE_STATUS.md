---
title: Current Architecture Status
description: Current implemented surfaces, experimental areas and tied verification baseline.
---

# Current architecture status

This page describes the supplied language-authoring hardening baseline plus the integrated PlanFuzz research tooling. Historical cycle notes are stored under `internal-docs/` and are not current architecture authority.

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

The repository includes non-packable PlanFuzz Phase 0–3 tooling for configuration-aware differential testing:

- a language-neutral deterministic testcase, observation and oracle core;
- independent Acme and Wist adapters with adapter-owned structured generators;
- Acme interpreter/compiled and registry-order variants;
- Wist interpreter/compiler plus SSA `Disabled`, `Prefer` and `Require` variants;
- backend-parity, optimization-route-parity, plan-determinism, negative-surface, extension-noninterference, controlled-fallback and canonical-lock oracles;
- typed decimal and `Int32` observations plus versioned selected/excluded-surface and activation-owner evidence;
- Wist route/fallback evidence without Wist-specific diagnostic ownership in the generic core;
- fresh-process execution, bounded timeout/output handling, replay and recursive artifact manifests;
- exact replay fingerprints separated from coarser campaign-triage class fingerprints;
- clean, confirmed, flaky, inconclusive and infrastructure outcomes kept distinct;
- default discovery generation separated from an explicit `--include-regressions` corpus;
- test-only Acme wrong-arithmetic, excluded-activation and extension-interference faults kept separate from current Wist findings;
- a configuration-complete `UniversalToolchain/PlanFuzz.sln` built by canonical repository entrypoints;
- deterministic adapter-owned program reduction plus generic oracle-contract/variant pruning, with every accepted candidate reconfirmed in fresh processes against the original exact fingerprint.

This tooling is not part of the public Wist package and does not establish publication claims. The preserved regression-inclusive Wist pilot records the historical behaviors tracked in #302, #303 and #307, while the current source state fixes and regression-protects those behaviors at the arithmetic-optimizer and SSA-verifier owner boundaries. The pilot's normalized finding classes remain historical triage groups rather than clean discovery yield. Program/plan reduction and the hardened seven-oracle Phase 3a surface contract are implemented. Observation schema v4 separates surface IDs from runtime owner IDs, rejects malformed evidence, and records actual Acme component activations. Lifecycle schedules and schedule reduction remain incomplete, so Phase 3 as a whole is not complete.

## Experimental or incomplete

- low-level generic SDK compatibility is alpha;
- high-level grammar, binder, type-system and operation authoring are not provided;
- Wist SSA is opt-in and verifier-gated, not the default backend;
- generic third-party package version negotiation is narrower than a mature ecosystem resolver;
- runtime component traits are package attestations, not hostile-extension proof;
- Wist Bytecode tag verification remains incomplete;
- no hardened in-process sandbox is claimed.

## Verification identity

`VERIFICATION.md` is the canonical checked-in verification record. GitHub Actions additionally enforces the canonical build/test/package path, documentation checks, rollout smoke and recursive manifest equality for the integrated revision.

See [Current Verification](/evidence/current-verification) for the evidence boundary.
