# PlanFuzz

**Status:** Phase 0–1 is merged by #306, the three first Wist findings are repaired by #308, Phase 2 adds deterministic program/plan reduction, and Phase 3 adds explicit negative-surface and extension-noninterference evidence. Lifecycle, schedule reduction and controlled-baseline stages remain open research work.

**Specification:** [Russian implementation and experiment specification](technical-specification.ru.md).

**Implementation evidence:** [Phase 0–1 status and preserved Wist pilot](implementation-status.md).

## Purpose

PlanFuzz is the configuration-aware testing layer built on top of the external language-authoring SDK. The SDK makes language packages, artifact routes, backend executors, component lifetimes and canonical plans explicit. PlanFuzz uses those contracts as a test space rather than treating a compiler testcase as source text alone.

A testcase is modeled as:

```text
program
× language-plan variant
× backend / optimization route
× execution schedule
× applicable oracle set
```

The central hypothesis is that configuration-aware differential and metamorphic testing can find defects at the intersections of language features, package contributions, artifact routes, backends, fallback policies and runtime lifecycles that program-only fuzzing and handwritten tests miss under a comparable execution budget.

## Merged implementation

The integrated baseline provides:

```text
language-neutral deterministic core
+ Acme structured generator
+ Wist restricted-Int32 generator
+ registry-order and backend/SSA variants
+ typed decimal and Int32 observations
+ backend, route, plan, negative-surface, extension-noninterference, fallback and canonical-lock oracles
+ fresh worker process per testcase attempt
+ deterministic strict replay and campaign artifacts
+ exact fingerprints separated from triage classes
+ opt-in known-regression corpus
+ one test-owned Acme wrong-arithmetic fault
+ adapter-owned structured program reduction
+ generic plan-contract and unreferenced-variant reduction
+ complete selected/excluded surface and activation-owner evidence
+ fresh-process exact-fingerprint acceptance
```

The evidence path is:

```text
generate
-> canonical testcase serialization and case identity
-> isolated variant execution
-> typed observation normalization
-> oracle evaluation
-> repeated fresh-process confirmation
-> deterministic program/plan reduction when requested
-> replayable artifact with SHA-256 manifest
```

Confirmed, clean, flaky, inconclusive and infrastructure outcomes remain separate. Historical Wist triggers for #302, #303 and #307 remain available only through explicit regression-corpus opt-in; the current source state fixes and regression-protects them, and replaying them does not count as fresh rediscovery.

## Required architecture boundaries

1. PlanFuzz core remains language-neutral and contains no Wist feature IDs, syntax rules or backend types.
2. Program generation and reduction use adapter-owned structured models.
3. Existing runtime planning and executor-selection contracts remain authoritative.
4. Unsupported behavior is classified explicitly and never normalized into silent success.
5. Findings are confirmed out of process with complete oracle evidence before they are counted.
6. Seeded faults, real defects, flaky outcomes, infrastructure failures and inconclusive cases remain separate.
7. Research instrumentation must not expand the public Wist package surface without an independent non-Wist use case and compatibility review.

## Remaining implementation stages

1. Lifecycle/session/concurrency schedules and schedule-dimension reduction.
2. Remaining timeout/order/optimizer seeded faults.
3. Remaining seeded faults and a stable minimized finding corpus.
4. Equal-budget baselines and ablations.
5. Third adapter and clean-machine publication-scale replay.

## Research boundary

The preserved Wist pilot included known regression cases. Its violating-case count is not clean discovery yield, and normalized finding classes are not unique-defect or root-cause identities.

Seeded faults validate the tool but do not count as discovered compiler defects. Publication claims require confirmed previously unknown defects, minimized testcases, root-cause analysis, regression tests and raw reproducible evidence.

Related current documentation:

- [Current architecture status](../../../docs/CURRENT_ARCHITECTURE_STATUS.md)
- [Current verification](../../../docs/evidence/current-verification.md)
- [External language-authoring SDK](../../../docs/architecture/external-language-authoring-sdk.md)
- [Language-authoring workflow](../../../docs/language-authoring/index.md)
- [Callable-first SSA route](../../../docs/architecture/callable-first-ssa.md)

Further stages are tracked in #298.
