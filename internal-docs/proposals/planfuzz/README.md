# PlanFuzz research proposal

**Status:** Phase 0 and the first Acme vertical slice are implemented on the research branch; Wist, SSA, lifecycle and reduction stages remain proposals.  
**Repository baseline:** `master@7f2b5819f712d03c39270349b6b39e914b79e008`.  
**Specification:** [Russian implementation and experiment specification](technical-specification.ru.md).  
**Implementation evidence:** [Phase 0 and Acme status](implementation-status.md).

## Purpose

PlanFuzz is the next research layer for UniversalToolchain after the external language-authoring SDK. The SDK makes language packages, artifact routes, backend executors, component lifetimes and canonical plans explicit. PlanFuzz uses those contracts as a test space rather than treating a compiler testcase as source text alone.

A testcase is modeled as:

```text
program
× language-plan variant
× backend / optimization route
× execution schedule
× applicable oracle set
```

The central hypothesis is that configuration-aware differential and metamorphic testing can find defects at the intersections of language features, package contributions, artifact routes, backends, fallback policies and runtime lifecycles that program-only fuzzing and handwritten tests miss under a comparable execution budget.

## Implemented first slice

```text
Acme structured generator
+ registry-order mutation
+ interpreter/compiled variants
+ typed decimal observations
+ backend-parity oracle
+ plan-determinism oracle
+ canonical-lock oracle
+ fresh worker process per testcase attempt
+ deterministic replay and recursive artifact manifest
+ one seeded wrong-arithmetic fault
```

The implemented evidence path is:

```text
generate
-> canonical testcase serialization and case identity
-> isolated variant execution
-> typed observation normalization
-> oracle evaluation
-> repeated fresh-process confirmation
-> replayable artifact with SHA-256 manifest
```

Wist, SSA-route variation, negative-surface checks, lifecycle schedules and testcase reduction are introduced only after this slice remains deterministic under bounded campaigns and CI.

## Required architecture boundaries

1. PlanFuzz core remains language-neutral and contains no Wist feature IDs, syntax rules or backend types.
2. Program generation and reduction use adapter-owned structured models.
3. Existing runtime planning and executor-selection contracts remain authoritative.
4. Unsupported behavior is classified explicitly and never normalized into silent success.
5. Findings are confirmed out of process before they are counted as defects.
6. Seeded faults, real defects, flaky outcomes, infrastructure failures and inconclusive cases remain separate.
7. Research instrumentation must not expand the public Wist package surface without an independent non-Wist use case and compatibility review.

## Remaining implementation stages

1. Bounded Acme campaign evidence and worker-timeout fault injection.
2. Wist restricted-arithmetic interpreter/CIL matrix.
3. Applicable `AIR -> SSA -> AIR` policy variation and controlled-fallback oracle.
4. Lifecycle and negative-surface traces.
5. Multidimensional reducer and stable finding corpus.
6. Equal-budget baselines, ablations and publication evidence.
7. Third adapter and clean-machine artifact replay.

## Research boundary

Seeded faults validate the tool but do not count as discovered compiler defects. A publication claim requires confirmed previously unknown defects, minimized testcases, root-cause analysis, regression tests and raw reproducible evidence.

Related current documentation:

- [External language-authoring SDK](../../../docs/architecture/external-language-authoring-sdk.md)
- [Language-authoring workflow](../../../docs/language-authoring/index.md)
- [Callable-first SSA route](../../../docs/architecture/callable-first-ssa.md)
- [Current architecture status](../../../docs/CURRENT_ARCHITECTURE_STATUS.md)
