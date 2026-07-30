---
title: Current Architecture Status
description: Current implemented surfaces, research layers and tied verification boundary.
---

# Current architecture status

This page describes the supplied language-authoring hardening baseline, integrated PlanFuzz tooling and the non-packable production-boundary contract study. Historical cycle notes under `internal-docs/` are evidence, not current architecture authority.

## Current public/product surface

`UniversalToolchain.Wist` `0.1.0-alpha.4` provides:

- `WistEngine`;
- restricted arithmetic and broader native presets;
- `Validate`, `Evaluate`, `Compile<TDelegate>` and `TryCompile<TDelegate>`;
- structured diagnostics and preflight limits;
- typed compiled-program metadata;
- an optional observable Wist `AIR -> SSA -> AIR` optimization route.

## Current generic language-authoring surface

The repository implements:

- `UniversalToolchain.Language.Abstractions` for stable IDs, policies and typed artifact contracts;
- `UniversalToolchain.FeatureSdk` for features and contribution metadata;
- `UniversalToolchain.LanguageSdk` for registry-backed deterministic planning, `LanguagePlan` and schema-v5 lock serialization;
- `UniversalToolchain.LanguageAuthoring` for coupled descriptor/runtime registration builders;
- `UniversalToolchain.Runtime` for route assembly, exact executor selection, policy validation and lifecycle;
- `UniversalToolchain.Testing` for backend parity helpers;
- `UniversalToolchain.Templates` with `ut-language`;
- `UniversalToolchain.Wist.LanguagePack` `0.3.0-alpha.3` as the typed Wist package boundary;
- `samples/Acme.PricingLanguage` as a non-Wist sample.

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
Source/Text -> Lexer/Parser -> AST -> Binding -> Bytecode -> AIR -> Optimization -> Backend -> Execution
```

Bytecode and AIR remain separate semantic boundaries. Wist runtime selection goes through dialect build plans, manifests and capabilities. Interpreter/CIL parity remains required for shared supported behavior.

The current Wist composition path installs `ModuleContractPipelineProfiles.StrictEnforced`. Selected modules without descriptors fail selection. `ModuleContractPipelineObserver` reads observed Bytecode metadata and invokes `BytecodeVerifier` after Bytecode, then verifies AIR before and after optimization. Facts/effects use the actual selected pipeline order, and unresolved invalidations for another verifier rule fail closed.

This closes the earlier claim that Bytecode declared/observed verification existed only as a detached reporter. The remaining boundary is narrower: verification can only reason about the typed metadata and contracts represented by the selected components; it is not a proof that every future instruction implementation carries a complete semantic specification.

## Experimental PlanFuzz research tooling

The repository includes non-packable PlanFuzz Phase 0–3a tooling for configuration-aware relational testing:

- a language-neutral deterministic testcase, observation and oracle core;
- independent Acme and Wist adapters with adapter-owned structured generators;
- Acme interpreter/compiled and registry-order variants;
- Wist interpreter/compiler plus SSA `Disabled`, `Prefer` and `Require` variants;
- backend-parity, optimization-route-parity, plan-determinism, negative-surface, extension-noninterference, controlled-fallback and canonical-lock oracles;
- typed observations and versioned selected/excluded-surface and activation-owner evidence;
- fresh-process execution, bounded timeout/output handling, replay and recursive manifests;
- exact replay fingerprints separated from campaign-triage class fingerprints;
- clean, confirmed, flaky, inconclusive and infrastructure outcomes kept distinct;
- an opt-in historical regression corpus;
- deterministic adapter-owned program reduction plus generic oracle-contract/variant pruning, with accepted candidates reconfirmed against the original exact fingerprint.

PlanFuzz is not part of the public Wist package and does not establish superiority claims. Lifecycle schedules, schedule reduction, equal-budget baselines, a third external adapter and publication-scale evaluation remain incomplete.

## Production-boundary contract study

`UniversalToolchain.ContractExperiments` is a separate non-packable executable that invokes the production contract-table, Bytecode, AIR, ownership, capability, facts/effects and reverification components.

It compares a structural baseline, typed contracts without fail-closed unresolved reverification, and the full fail-closed protocol. The canonical workflow independently restores/builds the project, runs a frozen primary catalog, a post-freeze author-designed challenge catalog, stratified valid controls and process-level timing replicates, then archives raw evidence and a recursive manifest.

The study is bounded evidence for the exact UniversalToolchain boundaries and author-designed fault operators. It is not a replacement for PlanFuzz, externally authored unseen faults or end-to-end source-to-execution evaluation.

## Experimental or incomplete

- low-level generic SDK compatibility is alpha;
- high-level grammar, binder, type-system and operation authoring are not provided;
- Wist SSA is opt-in and verifier-gated, not the default route;
- generic third-party package version negotiation is narrower than a mature ecosystem resolver;
- runtime component traits are package attestations, not hostile-extension proof;
- typed metadata coverage is an engineering contract, not a formal proof of semantic completeness;
- no hardened in-process sandbox is claimed;
- release-package compatibility requires reviewed external baseline artifacts and is separate from ordinary CI.

## Verification identity

`VERIFICATION.md` is the detailed checked-in authority. GitHub Actions enforces the ordinary build/test gate, documentation checks and deployment, rollout and published-package smokes, benchmark smoke, the contract experiment and a master-only aggregate status. Package release evidence remains separate and baseline-bearing.
