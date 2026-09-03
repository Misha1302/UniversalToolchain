---
title: LangDev adversarial architecture boundaries
description: Current UniversalToolchain planning claims, limits, and source-of-truth boundaries for LangDev 2026.
audience: maintainers, conference reviewers, advanced users
navigation: hidden
status: proposed documentation-only hardening
---

# LangDev adversarial architecture boundaries

This note is intentionally conservative. It does not expand UniversalToolchain architecture, public API, runtime policy, cache, dependency solving, source generation, or concurrency semantics. Its purpose is to make the current architecture easier to defend without claiming more than the current implementation and evidence can support.

## Central thesis

UniversalToolchain does not make composition complexity disappear. It changes the representation of that complexity: from distributed runtime control flow into explicit deterministic planning data.

A shorter version for talks:

> Keep local knowledge local. Make global decisions explicit.

The planner gives global composition complexity one explicit owner. It does not prove semantic correctness, remove all cost, make hostile extensions safe, or turn arbitrary independently authored extensions into mutually compatible software.

## Strongest baseline: handwritten pipeline

For one fixed language with known components, the best architecture is often a handwritten pipeline:

```csharp
language
    .UseParser(parser)
    .UseLowering(lowering)
    .UsePass(optimizer)
    .UseBackend(backend);
```

UniversalToolchain becomes justified when independently contributed packages create whole-language decisions that no single local component owns: capability-provider choice, conflict handling, pass ordering, artifact-route connectivity, runtime-provider identity, backend selection, provenance and reproducibility.

The honest decision rule is:

> Use the simplest owner that can correctly make the whole-system decision.

If one host application already knows every component and every order, a framework planner is usually unnecessary.

## What planning can prove

The current planning model can support claims about declared structure:

- feature dependencies;
- declared conflicts;
- capability/provider selection;
- required capabilities;
- artifact contract connectivity;
- explicit ordering constraints;
- runtime-provider identity;
- selected backend routes;
- provenance of selected packages/contributions;
- plan structural consistency;
- reproducible machine-readable plan projection, subject to canonicalization/version rules.

## What planning does not prove

Planning does not automatically prove:

- semantic equivalence between arbitrary extensions;
- correctness of optimizer transformations;
- behavioral compatibility of independently authored features;
- absence of semantic interference;
- absence of malicious code;
- sandboxing;
- thread safety of arbitrary providers/sessions;
- absence of performance overhead;
- universal dependency resolution;
- NativeAOT/trimming compatibility unless measured for a specific consumer and revision.

A valid plan is not a sandbox. Structural validation is not a trust boundary. Hostile inputs or hostile extension packages require a separate threat model, process/OS isolation, resource controls and supply-chain policy.

## Provider ambiguity vs route ambiguity

Provider ambiguity and route ambiguity must not be described with one vague sentence.

### Provider ambiguity

When two capability providers satisfy the same required capability and no explicit preference is supplied, the presentation may claim that this fails before execution only if the current source/evidence still shows diagnostic behavior such as `UTL2002` and a preference API such as `PreferCapabilityProvider`.

Safe talk wording:

> Provider ambiguity fails closed: the planner makes ambiguity observable before execution and requires explicit preference.

### Equal-cost route ambiguity

If two artifact routes are structurally valid and have equal cost, deterministic tie-breaking is defensible only as a reproducibility policy, not as a semantic-equivalence claim.

Safe documentation wording:

> Route selection is deterministic. Equal-cost candidates are resolved by stable canonical ordering. This guarantees reproducibility, not semantic equivalence. If an application needs semantic preference between otherwise valid routes, that preference must become explicit policy.

Do not use “the planner refuses to guess” as a universal statement unless route ambiguity is also confirmed to fail closed in current code.

## LanguagePlan guarantees

`LanguagePlan` should be presented as resolved composition data:

- selected features and contributions;
- selected runtime provider;
- selected artifact routes per backend;
- runtime policy chosen at planning time;
- plan summary;
- plan hash / lock-file projection;
- planning diagnostics when compilation fails.

`LanguagePlan` should not be described as:

- a mutable service container;
- a dependency manager;
- a proof of semantic correctness;
- a security boundary;
- a performance guarantee;
- a general plugin-safety mechanism.

## PlanHash wording

`PlanHash` should be described conservatively:

> PlanHash is an identity for the canonical resolved plan representation. It is useful for reproducibility and evidence binding. It is not a semantic proof that two independently authored packages behave compatibly, nor a claim that selected code is safe or optimal.

If canonicalization versioning changes, hash interpretation must be bound to the canonicalization/version context.

## Runtime boundary

Safe wording:

> Runtime materializes the selected plan and validates exact planned provider/route identity. It must not rediscover global composition decisions already made by the planner.

This allows runtime validation without turning runtime into a second planner. Runtime may reject a mismatched or impossible materialization. That is exact binding validation, not fresh global composition.

## Concurrency boundary

If the current source contains a lifecycle gate such as `RuntimeLifetimeGate`, document its exact guarantees only after source verification. The safe generic boundary is:

- lifecycle coordination may reject work after disposal begins;
- lifecycle coordination may wait for active operations before completing disposal;
- lifecycle coordination may protect self-dispose or double-dispose cases;
- it does not imply arbitrary provider/session thread safety.

Do not claim “thread-safe runtime” without specific tests and provider contracts.

## NativeAOT / trimming boundary

Do not claim NativeAOT or trimming support as a general property until an exact consumer, command, target framework, operating system, SDK and revision have been measured.

Safe current wording:

> NativeAOT/trimming are deployment experiments, not current universal support claims.

## Wist / UniversalToolchain / PlanFuzz boundary

The dependency direction should remain:

```text
Wist -----------------------> UniversalToolchain
PlanFuzz -------------------> UniversalToolchain
PlanFuzz.Adapter.Wist ------> Wist
```

Forbidden pressure:

- UniversalToolchain must not add public API only because PlanFuzz wants internal planner state.
- UniversalToolchain generic packages must not grow Wist-specific assumptions.
- PlanFuzz.Core must remain language-neutral.
- Wist-specific fuzz oracles belong in adapter/policy code, not in generic planning.

Safe PlanFuzz claim:

> Once composition becomes explicit data, configuration-aware testing becomes possible.

Do not claim PlanFuzz proves the architecture or beats normal fuzzing without equal-budget comparative experiments.

## Source-of-truth audit

| Concept | Canonical owner | Derived representations | Drift risk | Action |
| --- | --- | --- | --- | --- |
| Semantics | language implementation / Wist contracts | docs, slides, demo prose | high | state structural/semantic distinction explicitly |
| Provider identity | planner registry + `LanguagePlan` | README, claims.md, demo | medium | bind talk claim to current source/evidence |
| Route | artifact route phase + `LanguagePlan.Routes` | diagrams, demo failure slide | high | document equal-cost route policy separately |
| Artifact contract | current contract/connectivity code | docs, examples | medium | never equate structural connection with semantic compatibility |
| Package version | package metadata/current source candidate docs | README, install docs | high | always distinguish published package from source candidate |
| Toolchain API | current public API source | docs/snippets/slides | high | snippet checks or manual evidence matrix |
| Manifest schema | current schema/source | lock docs | medium | do not invent version-management subsystem |
| Plan | `LanguagePlan` | lock/explain docs/slides | medium | keep projections subordinate to typed plan |
| PlanHash | canonicalizer/hash owner | docs/slides/lock | high | define as representation identity, not semantics |
| Lock | lock serialization owner | docs/CI/evidence | medium | bind to exact revision/canonicalization version |
| Provenance | package registry + plan | evidence package | medium | include exact commit/package identity |
| Backend semantics | language/backend tests | parity docs | high | claim only tested parity boundaries |
| PlanFuzz policy | PlanFuzz docs/adapters | talk appendix | high | keep as research/testing layer, not runtime feature |

## Architecture invariants to preserve

1. Planning must be deterministic.
2. Provider ambiguity must fail before execution unless explicitly resolved.
3. Runtime must not rediscover global composition decisions already captured in the plan.
4. Language-specific semantics belong outside generic infrastructure.
5. PlanFuzz.Core remains language-neutral.
6. Testing infrastructure may consume UniversalToolchain contracts, but UniversalToolchain must not become test-framework-specific.
7. A LanguagePlan represents resolved decisions, not a mutable DI container.
8. Future work should be triggered by measured pain or second-consumer pressure, not by speculative neatness.
