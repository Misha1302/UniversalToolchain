# PlanFuzz research proposal

**Status:** proposed research direction; no PlanFuzz implementation is present in the current architecture.  
**Repository baseline:** `master@7f2b5819f712d03c39270349b6b39e914b79e008`.  
**Specification:** [Russian implementation and experiment specification](technical-specification.ru.md).

## Purpose

PlanFuzz is the proposed next research layer for UniversalToolchain after the external language-authoring SDK.
The SDK makes language packages, artifact routes, backend executors, component lifetimes and canonical plans
explicit. PlanFuzz uses those explicit contracts as a test space rather than treating a compiler testcase as
source text alone.

A testcase is modeled as:

```text
program
× language-plan variant
× backend / optimization route
× execution schedule
× applicable oracle set
```

The central hypothesis is that configuration-aware differential and metamorphic testing can find defects at
the intersections of language features, package contributions, artifact routes, backends, fallback policies and
runtime lifecycles that program-only fuzzing and handwritten tests miss under a comparable execution budget.

## Why this follows the language-authoring SDK

The current repository now has the prerequisites for this direction:

- immutable language plans and canonical plan hashes;
- independently packaged language contributions;
- configurable artifact routes and exact executor selection;
- explicit component lifetimes;
- Wist interpreter/CIL parity infrastructure;
- an opt-in, verifier-gated Wist `AIR -> SSA -> AIR` route;
- the independent [`Acme.PricingLanguage`](../../../samples/Acme.PricingLanguage/README.md) example;
- reusable language testing contracts.

PlanFuzz is therefore not positioned as a replacement for the compiler or SDK. It is a research and validation
layer built on top of the contracts the SDK already exposes.

## Initial scope

The first mergeable implementation milestone is intentionally narrow:

```text
Acme structured generator
+ registry-order mutation
+ interpreter/compiled variants
+ typed decimal observations
+ backend-parity oracle
+ plan-determinism oracle
+ fresh worker process per testcase
+ finding replay
+ one seeded wrong-arithmetic fault
```

This milestone must prove the complete evidence path:

```text
generate
-> serialize
-> execute in isolation
-> normalize observations
-> evaluate an oracle
-> confirm in a fresh process
-> preserve a replayable finding artifact
```

Wist, SSA-route variation, negative-surface checks, lifecycle schedules and testcase reduction are introduced
only after this vertical slice is deterministic and replayable.

## Required architecture boundaries

Any implementation must preserve the following repository invariants:

1. PlanFuzz core remains language-neutral and contains no Wist feature IDs, syntax rules or backend types.
2. Program generation and reduction use adapter-owned structured models; the core does not parse raw language
   source with regular expressions.
3. Existing runtime planning and executor-selection contracts remain authoritative; the tool does not invent a
   second execution model.
4. Unsupported behavior is classified explicitly and never normalized into silent success.
5. Findings are confirmed out of process before they are counted as defects.
6. Seeded faults, real defects, flaky outcomes, infrastructure failures and inconclusive cases remain separate.
7. Research instrumentation must not expand the public Wist package surface without an independent non-Wist use
   case and compatibility review.

## Mandatory oracle families

The full MVP targets:

- backend parity;
- optimization and route parity;
- plan determinism;
- negative-surface preservation;
- extension noninterference;
- controlled fallback;
- session and runtime-state isolation;
- route conformance;
- canonical lock consistency;
- diagnostic determinism;
- resource-limit consistency;
- worker robustness.

Every oracle has applicability preconditions. An inapplicable oracle reports `NotApplicable`; it must not be
counted as passed and must not create a false finding.

## Research evaluation

The publication-oriented campaign compares equal execution budgets for:

1. existing handwritten tests;
2. program-only generation;
3. pairwise plan enumeration;
4. full PlanFuzz generation across program, plan, route and lifecycle dimensions.

A research claim requires confirmed previously unknown defects, minimized testcases, root-cause analysis,
regression tests and raw reproducible evidence. Seeded faults validate the tool but do not count as discovered
compiler defects.

## Implementation stages

1. **Baseline and skeleton:** lock schemas, adapter contracts, isolation protocol and evidence layout.
2. **Acme vertical slice:** deterministic generation, parity, plan determinism and replay.
3. **Wist arithmetic matrix:** interpreter/CIL and applicable SSA-policy variation.
4. **Lifecycle and negative surface:** sessions, controlled fallback and excluded-capability traces.
5. **Reducer and corpus:** multidimensional reduction with stable defect fingerprints.
6. **Research campaigns:** baselines, ablations, classification and raw artifact publication.
7. **External validation:** a third adapter, clean-machine replay and artifact-evaluation packaging.

## Non-goals

The proposal does not claim:

- a hardened sandbox for hostile extensions or programs;
- formal verification of UniversalToolchain;
- automatic grammar inference;
- fuzzing arbitrary CLR IL;
- that every behavioral difference is a compiler bug;
- that implementing the tool alone proves the research hypothesis.

## Promotion path

This proposal becomes current architecture only after:

1. the first language-neutral core and Acme adapter land through existing package boundaries;
2. seeded-fault and deterministic-replay tests pass;
3. current architecture and documentation indices are updated;
4. CI runs a bounded smoke campaign without introducing flaky release gates;
5. research claims are backed by preserved campaign artifacts rather than proposal text.

Related current documentation:

- [External language-authoring SDK](../../../docs/architecture/external-language-authoring-sdk.md)
- [Language-authoring workflow](../../../docs/language-authoring/index.md)
- [Callable-first SSA route](../../../docs/architecture/callable-first-ssa.md)
- [Current architecture status](../../../docs/CURRENT_ARCHITECTURE_STATUS.md)
