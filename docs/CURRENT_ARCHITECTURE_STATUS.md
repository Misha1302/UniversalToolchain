---
title: Current Architecture Status
description: Current implemented surfaces, research layers and tied verification boundary.
---

# Current architecture status

This page is the current architecture authority for the repository. Historical cycle notes and dated reviews under `internal-docs/` are evidence snapshots, not current implementation truth.

## Current public/product surface

<!-- wist-source-candidate:begin -->
`UniversalToolchain.Wist` source candidate `0.1.0-alpha.7` is **not published on NuGet.org** and provides:
<!-- wist-source-candidate:end -->

- `WistEngine`;
- restricted arithmetic and broader native presets;
- `Validate`, `Evaluate`, `Compile<TDelegate>` and `TryCompile<TDelegate>`;
- structured diagnostics and preflight limits;
- typed compiled-program metadata;
- optional observable Wist `AIR -> SSA -> AIR` optimization routes.

The source candidate is separate from the older package version installed by the published-package smoke. Do not infer publication from the project version alone.

## Current generic language-authoring surface

The repository implements:

- `UniversalToolchain.Language.Abstractions` for stable IDs, policies and typed artifact contracts;
- `UniversalToolchain.FeatureSdk` for features, package identity and contribution metadata;
- `UniversalToolchain.LanguageSdk` for registry-backed deterministic planning, `LanguagePlan` and schema-v6 lock serialization;
- `UniversalToolchain.LanguageAuthoring` for coupled descriptor/runtime registration builders;
- `UniversalToolchain.Runtime` for exact plan verification, route materialization, executor selection, policy validation and lifecycle;
- `UniversalToolchain.Testing` for reusable contract/parity support;
- `UniversalToolchain.Templates` with `ut-language`;
- `UniversalToolchain.Wist.LanguagePack` as the typed Wist package boundary;
- `samples/Acme.PricingLanguage` as a non-Wist sample.

Package versions are release identities rather than architecture contracts. The active Wist source identity is checked through `eng/documentation-release-state.json`; the full candidate package matrix belongs to release evidence rather than this architecture authority.

## Implemented generic contracts

- typed entry artifacts and route endpoints;
- independently owned features and contributions;
- feature dependencies, conflicts, capabilities and explicit slot replacement;
- explicit contribution exclusions;
- package-level contributions;
- deterministic minimum-cost artifact routes;
- deterministic same-artifact pass ordering;
- planning-only definitions;
- exact package-manifest and implementation-instance binding;
- exact `(contribution, backend, input contract)` executor resolution;
- `PerSession` and explicit `SingletonStateless` component lifetimes;
- synchronous/asynchronous disposal coordinated with in-flight operations;
- runtime gates for determinism and host interop.

## Wist compiler/runtime architecture

The canonical Wist ownership chain is:

```text
.wistdialect / preset
  -> LanguageDefinition
  -> LanguageCompiler
  -> one immutable LanguagePlan
  -> LanguageRuntime
  -> exact planned components
```

The planned execution route then follows the concrete language pipeline:

```text
Source/Text -> Lexer/Parser -> AST -> Bytecode -> AIR -> optimizers/optional SSA -> backend artifact -> execution
```

`WistFacadeLanguageDefinitionFactory` is a configuration translator, not a second planner. It maps Wist-facing aliases and policy to typed generic contracts. `LanguageCompiler` alone owns feature dependency closure, contribution/capability-provider resolution, explicit exclusions, contribution ordering, runtime-provider selection and backend artifact routes.

`LanguageRuntime` materializes the graph already selected by `LanguagePlan`. Runtime materialization validates exact package/source provenance for executable components and must not add features, reorder contributions or choose a second backend plan. Tooling-only planned contributions do not become runtime-source requirements merely because they are present in the semantic plan.

For public embedding, `WistEngine.Create` constructs this plan/runtime once. `Evaluate`, `Validate` and `Compile<TDelegate>` reuse the same canonical ownership chain rather than invoking another Wist composition workflow.

Bytecode and AIR remain separate semantic boundaries. Interpreter/CIL parity is required for shared supported behavior and is tested from one canonical multi-route plan where parity itself is the contract.

## Wist configuration semantics

- `use` requests Wist features/modules; typed feature dependencies are closed by `LanguageCompiler` rather than duplicated manually in every dialect file.
- Wist group aliases are data-only alias lists expanded before `LanguageDefinition` construction; they are not dependency resolvers or runtime components.
- `exclude` aliases are translated to `LanguageDefinition.ExcludedContributions`; if dependency closure requires an excluded contribution, planning fails closed.
- backend, security and intrinsic policy are translated to typed generic contracts before planning.
- source identity remains provenance-bearing; semantic equivalence across different source names must not be inferred from `PlanHash` equality.

## Retired compatibility topology boundary

S13 physically removes the old `UniversalToolchain.Dialects.Integration` reflection/runtime-profile host and the non-packable `UniversalToolchain.Dialects.Wist` compatibility project. They are no longer available as transitive dependency containers, build-order helpers, runtime selectors or compatibility owners.

The old `.dialect.runtime.json` emitter/copy/package representation is retired. `DialectComponentContractAttribute` remains only as explicit module-verification identity; it is not discovery metadata. Canonical selection is `LanguageDefinition -> LanguageCompiler -> LanguagePlan -> LanguageRuntime`, with exact route-component sources supplied explicitly.

The retired orchestration surface spans S11-S13: old dialect build/runtime planning, BasicCore end-to-end orchestration, runtime-profile/reflection host topology and compatibility Wist runtime owners are all guarded against reintroduction.

## Module/IR verification boundary

The runtime/compiler pipeline retains explicit module-contract, Bytecode, AIR, ownership, capability and reverification checks. Verification operates on represented typed metadata and contracts; it is not a proof that every future instruction implementation carries a complete formal semantic specification.

Do not use compatibility/observation policy as a hidden route around canonical feature/contribution selection. Module authoring and verification contracts must remain explicit and test-protected.

## Experimental PlanFuzz research tooling

The repository includes non-packable PlanFuzz tooling for configuration-aware relational testing:

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

`UniversalToolchain.ContractExperiments` is a separate non-packable executable that invokes production contract-table, Bytecode, AIR, ownership, capability, facts/effects and reverification components.

It compares a structural baseline, typed contracts without fail-closed unresolved reverification, and the full fail-closed protocol. The canonical workflow independently restores/builds the project, runs its frozen/holdout corpora and controls, and archives evidence with integrity metadata.

The study is bounded evidence for the exact UniversalToolchain boundaries and author-designed fault operators. It is not a replacement for PlanFuzz, externally authored unseen faults or end-to-end source-to-execution evaluation.

## Experimental or incomplete

- low-level generic SDK compatibility is alpha;
- high-level grammar, binder, type-system and operation authoring are not provided;
- Wist SSA is opt-in and verifier-gated, not the universal default route;
- generic third-party package version negotiation is narrower than a mature ecosystem resolver;
- runtime component traits are package attestations, not hostile-extension proof;
- typed metadata coverage is an engineering contract, not a formal proof of semantic completeness;
- no hardened in-process sandbox is claimed;
- legacy runtime-manifest emission/copy/package tooling is retired; component identity and activation are explicit typed contracts owned by the package/plan/runtime path;
- S12 retires the old BasicCore end-to-end orchestration island; reusable BasicCore stage contracts remain while topology cleanup continues in S13;
- release-package compatibility requires reviewed external baseline artifacts and is separate from ordinary CI.

## Verification identity

`VERIFICATION.md` is the detailed checked-in verification authority. The active exact test manifest is `eng/test-counts.json`.

GitHub Actions enforces the ordinary build/test gate, documentation checks, rollout and published-package smokes, benchmark smoke and the contract experiment. Final migration-stage acceptance requires exact-head evidence; superseded workflow runs are diagnostic only.

Package release evidence remains separate and baseline-bearing. NuGet publication and merge to `master` are separate actions and are not implied by a green migration-stage PR.
