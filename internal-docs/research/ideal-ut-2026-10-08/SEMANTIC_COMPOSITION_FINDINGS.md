# Semantic-composition findings and novelty boundary

**Status:** evidence-bounded research synthesis, not release readiness, not formal verification.
**Review date:** 2026-10-23. **UT source revision:** [`de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1`](https://github.com/Misha1302/UniversalToolchain/tree/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1).
**Canonical question answers:** [CRITICAL_QUESTIONS.md](CRITICAL_QUESTIONS.md); original Q-ID wording remains in [QUESTION_LEDGER_V1](ADVERSARIAL_QUESTION_LEDGER_2026-10-22.md).

## STRONGEST_FINDINGS

| ID | Finding | Evidence class | Reproducible anchor | Consequence |
| --- | --- | --- | --- | --- |
| F1 | The abstract “new analysis improves old optimizer” pattern **already exists** in LLVM AA and MLIR interface-based extension. | **Documented upstream production capability**; no custom controlled A/B here | [LLVM AAManager](https://llvm.org/docs/doxygen/classllvm_1_1AAManager.html), [AAResults](https://llvm.org/doxygen/classllvm_1_1AAResults.html), [MLIR Interfaces](https://mlir.llvm.org/docs/Interfaces/) | Delete any UT-first narrative for that property. Q01/Q02/Q06/Q39. |
| F2 | MLIR already supports analysis/transform separation through One-Shot Bufferization and a fixed-point multi-analysis DataFlowSolver. | **Documented upstream APIs** | [Bufferization](https://mlir.llvm.org/docs/Bufferization/), [DataFlowSolver](https://mlir.llvm.org/doxygen/classmlir_1_1DataFlowSolver.html) | New inference engine needs a new discriminator. Q03/Q06. |
| F3 | A new fact category does not automatically change an optimizer that lacks a query/transform hook. | **Interface/consumer architecture consequence**, local bounded illustration | [small C# control](EXPERIMENT_DESIGN.md#executed-small-c-control-not-a-ut-integration-experiment) plus LLVM/MLIR interface APIs | Make shared query vocabulary, registry or new transform explicit. Q09/Q10. |
| F4 | UT owns an actual deterministic language-configuration graph; route selection **is not** a proof of specific program optimizations. | **Direct UT source inspection** | [LanguageCompiler](../../../UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageCompiler.cs), [ArtifactRoutePhase](../../../UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageArtifactRoutePhase.cs) | Defend existing work without speculative semantic claims. Q26/Q28. |
| F5 | UT has real selected-contribution identity/route checks and narrow typed reverification scheduling, but **no shipped general trust-bearing fact calculus**. | **Direct source and architecture status** | [RouteRuntimeAssembler](../../../UniversalToolchain/UniversalToolchain.Runtime/LanguageRouteRuntimeAssembler.cs), [ModuleContractVerificationScheduler](../../../UniversalToolchain/UniversalToolchain.ModuleContracts/ModuleContractVerificationScheduler.cs), [status](../../../docs/CURRENT_ARCHITECTURE_STATUS.md) | Reuse actual owners, don't create a second planner. Q14/Q15/Q18/Q29. |
| F6 | A direct C# predeclared query service handles the bounded unchanged-consumer decision example without a new ontology. | **Executed 12/12 local decision assertions** only | [protocol and SHA](EXPERIMENT_DESIGN.md#executed-small-c-control-not-a-ut-integration-experiment) | Strong pressure against premature framework creation. Q07/Q09/Q41. |
| F7 | Safe bounds-check elimination needs observation-specific laws: paths, alias/mutation, signedness, overflow, exceptions, revs, implementation trust. | **Research requirements / not independently integrated** | [E1 proposed protocol](EXPERIMENTS.md), [proposed API disclaimer](PROPOSED_API_CONTRACTS.md) | The central example remains a real falsification candidate, not a success story. Q12–Q23. |
| F8 | PlanFuzz already carries concrete cross-configuration oracles, exact replay and reduction, with narrow historical regressions. | **Implemented experimental code; historical project evidence** | [PlanFuzz status](../../proposals/planfuzz/implementation-status.md) | Strong candidate for a defensible tested-demo talk; equal-budget advantage unproven. Q32/Q40. |
| F9 | ContractExperiments compares bounded production verifier policies with frozen faults/oracles; reported timings are not whole compilation cost. | **Protocol + revision-bound historical evidence**, not independently rerun in this pass | [V3 study protocol](../../../UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/STUDY_PROTOCOL_V3.md), [VERIFICATION](../../../VERIFICATION.md) | Potential empirical talk with strict denominators and limits. Q33. |
| F10 | Claiming semantic soundness from provenance, data types, source origin, checked route selection, or agreement on small inputs is not warranted. | **Known proof obligation / negative boundary** | [Alive2 scope](https://github.com/AliveToolkit/alive2), [MLIR conversion](https://mlir.llvm.org/docs/DialectConversion/), [UT proposed contracts](PROPOSED_API_CONTRACTS.md) | Independent oracle or conservative fallback required. Q12/Q15/Q22/Q23. |

## NOVELTY_BOUNDARY

### IMPLEMENTED and defensible (UT scope)

1. Selected `LanguageDefinition` resolves feature dependencies, contributions, backend/runtime provider and typed artifact routes to a canonical `LanguagePlan` through one `LanguageCompiler`. [Source](../../../UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageCompiler.cs).
2. `LanguagePlan` route and exact selected source/manifest component identity are validated at materialization; `LanguageRuntime` does not select a second composition. [Source](../../../UniversalToolchain/UniversalToolchain.Runtime/LanguageRouteRuntimeAssembler.cs).
3. An independent non-Wist `Acme.PricingLanguage` sample uses the authoring SDK and two execution routes; this is not proof of N+1 semantic interop. [Sample](../../../samples/Acme.PricingLanguage/Program.cs).
4. Wist module/IR facts-and-effects/reverification boundary and research-oriented PlanFuzz/ContractExperiments implementations exist; scope and packaging are deliberately limited. [Current architecture](../../../docs/CURRENT_ARCHITECTURE_STATUS.md).

### ALREADY EXISTS elsewhere (not unique)

- Analysis aggregation for unchanged clients of a standard interface: LLVM AA.
- External semantic interface implementations without original dialect source edits: MLIR External Models.
- Separate analysis state and common transformation: MLIR One-Shot Bufferization.
- Coordinated dataflow/fixpoint inference: MLIR DataFlowSolver; LLVM Attributor.
- IR-scoped preserved-analysis invalidation: LLVM/MLIR pass managers.
- Revision-based incremental recomputation: Salsa and other incremental query frameworks.
- Bounded validation of LLVM optimization refinement: Alive2.

### PROPOSED, technically interesting, but not yet measured or novel

An **optional**, **selected-world**, **revision-aware**, **heterogeneous-representation** evidence exchange may be distinctive **as a combination of constraints**: independently shipped producer/provider binaries, unchanged known-query consumers, semantic laws authorized by verifier/trust policy, refusal on incomplete dependencies, exact implementation/provenance invalidation, checked AST↔AIR correspondence, and operational cost accountability. The research has **not yet proven** that this combination is absent in competing frameworks or valuable enough to justify a new API.

### Explicitly unknown

- Whether UT can meet strong E1 semantics with no pairwise adapters and acceptable costs.
- Whether ordinary typed C# contracts + explicit adapters already cover all held-out scenarios more simply.
- Whether cross-representation fact transport improves over keeping local analyses local and re-running them.
- Whether extension authors will reliably establish `Pure`, `NoAlias`, `Serializable`, completeness, or cross-language laws.
- Whether any general service wins at 1/3/5/10 extension scales.
- Whether the intended meetup audience prefers a semantic evidence story over the already implemented PlanFuzz or verification experiments.

## What the counterexample baseline *does* and *does not* say

A local .NET 10 control with independent `Shared`, `Shape`, `Range`, `Consumer`, and `Host` projects yielded 12/12 passing boolean-decision assertions. Its consumer only knows `ISafeIndexQuery`. The conservative Range analysis misses the safe index-zero case, and the precise Range enables it, demonstrating a real precision difference. Adding Range through the host enables a `CanElideCheck` flag; conflicts, missing providers, bad revisions and mismatched **claimed** hashes conservatively disable that flag.

**This does not test actual check removal, static/executable parity, UT pipeline, independently authenticated executable SHA, mixed representations, hostile code, benchmark cost or user adoption.** A successful toy demonstration cannot be promoted to semantics soundness or a completed E1.

## Hypothesis decision and reversible strategy

**Current decision:** **NO-GO for shipping/generalizing a new semantic core now**. This is *not* a proof that such a core can never help. It is a refusal to invest before the discriminating evidence exists.

Potential next step: reuse direct typed C# producer/consumer contracts around one real optimization and verify its exact program semantics. Add a selected-world evidence broker only after a second independently authored consumer needs shared facts plus revocation that direct contracts demonstrably cannot maintain without costly pairwise glue.

**Stop/rollback immediately** if any safety-sensitive `Proven` authorizes an incorrect rewrite; do not dilute correctness into a performance score. Also refuse generalization when simpler baseline achieves equal correctness and no worse relevant integration cost, when trust cannot be verified, or when full recomputation is simpler/cheaper than precise invalidation.

## Four adversarial reviews

1. **LLVM/MLIR engineer:** strongest production equivalences are prominently listed; none silently minimized to “just IR passes”. Future reviewers must inspect real plugin behavior, not only API docs.
2. **Researcher:** an experiment needs frozen corpus/observation model and independent oracle; historical mutation results do not establish generalization.
3. **Systems architect:** default to direct C# interfaces; new ontology/rule engine/planner is a last resort, not architecture taste.
4. **Speaker:** 10–12 minutes supports one concrete demo, one counterexample, and one honest limit. It cannot prove a universal compiler framework.

See also [PRIOR_ART_COMPARISON.md](PRIOR_ART_COMPARISON.md), [EXPERIMENT_DESIGN.md](EXPERIMENT_DESIGN.md), [MEETUP_NARRATIVE_EVIDENCE.md](MEETUP_NARRATIVE_EVIDENCE.md), and [all Q-ID answers](CRITICAL_QUESTIONS.md).
