# Experimental engineering backlog — from source S0–S8, not completed tasks

This is a condensed, task-ready transfer of the supplied `08_CODEX_EXECUTION_TASKS.md`. All tasks are **PROPOSED**, have not been implemented through this documentation PR, and require a fresh baseline audit on the actual target branch. Keep changes experimental, reversible, Wist-independent in generic code, with exact positive and negative tests. Stated effort estimates in the original are *unmeasured*, not delivery promises.

| Task | Required evidence and owner | Acceptance | Key risk / stop |
| --- | --- | --- | --- |
| **S0 characterization** | baseline CI/source checkout, external LanguageSdk tests and independent Acme sample | exact SDK/revision/build/test receipts, clean baseline | without runnable actual UT, claims stay source-only |
| **S1 schema spike** | optional Semantics.Abstractions/ContractExperiments adjacent experiment | canonical versioned Range/Length schemas; new producer improves unchanged consumer; mismatches/Unknown fail | new central registry source edits or same-name incompatible schemas |
| **S2 actual bounds elimination** | **real UT** represented optimizer/IR, three independent packages | emitted check removed only when proved; invalid boundary/overflow/alias cases keep check; independent reference agrees | false-safe is hard abort |
| **S3 invalidation/concurrency** | immutable ProgramSnapshot, scoped provenance, optional mapping witnesses | provider hash/revision/IR/branch/cancel mutations cannot reuse stale verdicts | hidden cache/shared mutable state |
| **S4 two external languages** | clean consumer authoring paths, real LanguageAuthoring SDK | strict Rules/Policy DSL + divergent configuration/tensor example; source spans, deterministic diagnostics, no Wist dependency | parser/binder workbench sprawl |
| **S5 semantic services and LSP** | language services and optional LSP adapter | valid→invalid→valid, hover/completion/definition, stale result cannot overwrite new version | duplicate binder or second mutable syntax model |
| **S6 program-local feasibility** | after selected plan, bounded obligation query on real IR | hard semantic legality before route cost; mandatory pass behavior unchanged; no new global planner | config selection duplicated |
| **S7 compare and benchmark** | identical inputs, direct/interface/evidence controls; independent oracle | measured changed LOC, code volume, correctness/precision, memory, compile/materialize/hot latencies + variance | unmeasured zero-overhead/novelty claims |
| **S8 decision/review** | adversarial code/evidence review and independent author | decide A: typed interfaces only; B: bounded checked evidence; C: drop semantic generalization in favor of product | promoting a framework without independent value |

### Execution sequence

S0 → S1 → S2 → S3 → S6 → S7 → S8; S4 may start alongside S1 and feeds S5; S5 can progress alongside S2 once the respective contracts are fixed. Core promotion must follow negative tests and second external consumer, never precede them.

### Minimum acceptance invariants

- No consumer or provider may import the other's implementation assembly; record **all** integration LOC and package config edits.
- Fact identity includes selected world, schema version, subject, phase/path, program revision and trust; Unknown/Contradiction never legalize destructive transformations.
- Representation changes require accepted mapping certificate or fresh independent verification; `preserves` is only a claim until checked under policy.
- No universal IR, no second LanguageCompiler, no feature-specific Core switch, no runtime reflection graph lookup on the steady-state critical path.
- Proof legality is not profitability; test exceptions, heap, signed zero/FP modes, effects, control-flow and overflow.
- Any new public abstraction requires a named second external consumer, API/version owner, full negative corpus, clean restore/tests, no documentation-check weakening and rollback plan.

### What to do next

Create a **separate experimental PR** from a freshly observed current head. Start with S0, then S1 in the existing module-contract/ContractExperiments vicinity (not a shipped `Fact<T>` API). A successful toy stand-alone Python/.NET model is not sufficient to mark S2 complete.
