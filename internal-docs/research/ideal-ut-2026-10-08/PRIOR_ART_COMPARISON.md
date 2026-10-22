# Prior-art comparison: independent analyses versus semantic evidence

**Status:** checked primary APIs and exact UT source, not a four-way run.
**Reviewed:** 2026-10-23. **UT baseline:** [`de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1`](https://github.com/Misha1302/UniversalToolchain/tree/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1).
**Owner:** Ideal UT research dossier; **no production architecture/API change**.

## Short result

The proposition “add an independent analysis, improve an unchanged optimizer” is **already implemented as an extensibility pattern elsewhere**. The minimal typed C# composition root, LLVM `AAManager/AAResults`, MLIR operation/type interfaces, replaceable One-Shot Bufferization `AnalysisState`, and MLIR `DataFlowSolver` are serious alternatives. An optimizer can use added precision only through a query/interface/transform that it already consumes or that another registered transform consumes. A genuinely new *category* of semantic fact does not automatically rewrite old code.

The surviving **research candidate** (not current UT capability or established novelty) is *independently shipped selected-world semantic evidence with verified trust/invalidation across heterogeneous IR boundaries*, **if** simpler typed interfaces and existing frameworks cannot provide the same held-out properties at lower complexity. No such discriminator is proven yet.

## Reviewed UT implementation (not conjecture)

- `LanguageCompiler.Compile(LanguageDefinition)` reads feature/contribution resolution and invokes `LanguageArtifactRoutePhase.Build`; it has no `ProgramSnapshot` or SafeIndex input. [Source](../../../UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageCompiler.cs).
- `LanguageArtifactRoutePhase` determines backend input contracts, transformation conversion edges and pass placement from descriptor/cost/route constraints—not an instance-specific semantic theorem. [Source](../../../UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageArtifactRoutePhase.cs).
- `LanguagePlanCanonicalizer` hashes the selected configuration and manifest identities. `LanguagePlanVerifier` checks plan consistency, routes and canonical hash. `LanguageRouteRuntimeAssembler` validates selected runtime source bindings. These are **real exact-composition defenses**, not a general claim about proving arbitrary provider semantics. [Hash](../../../UniversalToolchain/UniversalToolchain.LanguageSdk/LanguagePlanCanonicalizer.cs), [verifier](../../../UniversalToolchain/UniversalToolchain.LanguageSdk/LanguagePlanVerifier.cs), [assembler](../../../UniversalToolchain/UniversalToolchain.Runtime/LanguageRouteRuntimeAssembler.cs).
- `ModuleContractVerificationScheduler` performs `P2Selective`/ `P3Always` scheduling using typed `ReverificationRequest`; it does not implement general cross-language inference or certify all `Pure`/ `SafeIndex` statements. [Source](../../../UniversalToolchain/UniversalToolchain.ModuleContracts/ModuleContractVerificationScheduler.cs).
- `samples/Acme.PricingLanguage` uses a non-Wist source parser and two backend routes in one registered package. This proves non-Wist SDK use at source level, **not** three independent semantic providers agreeing on a program-specific optimization. [Source](../../../samples/Acme.PricingLanguage/Program.cs).
- Proposed `SemanticQueryContext`, `ISemanticProvider<...>` and `AnchorMappingWitness` are explicitly design-only sketches; do not cite as shipped API. [Proposals](PROPOSED_API_CONTRACTS.md).

## Closest production alternatives (documented source/API behavior)

| Architecture | Concrete mechanism | What is actually supported | Crucial limitations / fair test |
| --- | --- | --- | --- |
| **A: typed C#** | `IFactProvider`, `ISafeIndexQuery`, explicit composition root | Independent typed assemblies can feed a pre-agreed query used by unchanged `BoundsOptimization`. A small 12-assertion decision baseline was executed locally. | No automatic novel property use, no genuine trust without checking, adapters and version discipline are still needed; only decision flags exercised in current control. |
| **B: LLVM** | `AAManager.registerFunctionAnalysis`, `AAResults.alias`, `ModRefInfo`, `PreservedAnalyses` | Independently registered AAs aggregate and update an unchanged existing AA-consuming pass; cached results have invalidation. | LLVM IR and shared alias interface; dynamic registration/pipeline configuration and query order matter. No UT-specific provenance guarantee; no custom external-plugin A/B run here. |
| **C: MLIR interfaces** | `OpInterface`/`TypeInterface`/`AttributeInterface`, `ExternalModel`, `DialectRegistry` | Existing interface consumers can use externally registered models without editing the original operation's definition. | A consumer still needs a known interface and a selected/registered model. Registry reachability and promised interfaces must be managed. |
| **C: MLIR DataFlow** | `DataFlowSolver.load`, analysis states/anchors/dependencies, worklist/fixpoint | Multiple analyses and shared state can be co-evaluated to a fixed point. | No automatically proven SafeIndex law, hostile-provider trust, or persistent cross-revision reuse. Official solver docs say states are erased and re-analysis is run when IR changes. |
| **C: MLIR Bufferization** | `BufferizableOpInterface`, external models, replaceable `AnalysisState`, `AlwaysCopyAnalysisState` | Analyses and consumer rewriting are already decoupled; adding operation memory semantics externally is supported. | Scope is MLIR tensor/memref, and interface semantics remain implementer responsibility. |
| **D: proposed UT evidence** | Versioned scoped predicates, dependency-complete premises, validated proof obligations, transport witnesses | Research documents outline desired contracts and E1–E8 experiments. **Not production-implemented**. | Must defeat A/B/C on independent held-out tests and record cost. No A/B/C/D comparison exists yet. |

### Direct primary sources

- LLVM [AAManager](https://llvm.org/docs/doxygen/classllvm_1_1AAManager.html) and [AAResults](https://llvm.org/doxygen/classllvm_1_1AAResults.html) explicitly describe aggregation, dependency identities and invalidation.
- LLVM [New Pass Manager](https://llvm.org/docs/NewPassManager.html) describes analysis caching/preservation/proxy handling; [Attributor](https://llvm.org/doxygen/structllvm_1_1Attributor.html) is a guided fixpoint deduction framework for interacting IR properties.
- MLIR [Interfaces and external models](https://mlir.llvm.org/docs/Interfaces/), [DataFlowSolver](https://mlir.llvm.org/doxygen/classmlir_1_1DataFlowSolver.html), [DataFlow Tutorial](https://mlir.llvm.org/docs/Tutorials/DataFlowAnalysis/), [Pass Management](https://mlir.llvm.org/docs/PassManagement/), [One-Shot Bufferization](https://mlir.llvm.org/docs/Bufferization/), and [Dialect Conversion](https://mlir.llvm.org/docs/DialectConversion/).
- [Salsa](https://salsa-rs.github.io/salsa/reference/algorithm.html) tracks query dependencies/revisions (prior art for incremental recomputation, not a plug-in trust system). [Alive2](https://github.com/AliveToolkit/alive2) performs bounded LLVM transformation/refinement checks, with declared feature limitations (not a universal source→IR equivalence oracle).

### Critical nuance: LLVM AA ordering

The upstream `AAManager` documentation says the **order** in which AA implementations are registered is significant for how results are queried. Therefore the honest claim is "registered analyses can improve an existing consumer through a shared interface", **not** "arbitrary addition always monotonically improves precision in every ordering". This requires a pinned plugin test.

### Critical nuance: MLIR state invalidation

Official MLIR `DataFlowSolver` docs describe discarding analysis states after IR mutation before recomputing. This limits claims of free cross-revision, precise dependency-preserving reuse. It does **not** prove UT has solved that problem. The same docs explicitly state the solver tracks dependencies among analyses and reaches a fixed point; those are **not** new capabilities to reinvent by default.

## Eight independent properties: A–H

Legend: **documented** = primary API behavior, **small control** = our bounded C# decision test, **unshown** = no integrated discriminator. Avoid collapsing them into “extensibility”.

| Property | Typed C# | LLVM AA | MLIR interfaces/solver | Proposed UT evidence |
| --- | --- | --- | --- | --- |
| A. Consumer unchanged | Small control, via pre-agreed `ISafeIndexQuery` | Documented for pre-existing `AAResults` consumers; no custom A/B run | Documented for existing interface consumers | Required, unshown |
| B. Existing producers unchanged | Small control when host adds provider | Potential with new registered AA | Potential with new external model | Required, unshown |
| C. No pairwise glue | Possible via shared contracts; host registration persists | Within fixed AA interface | Within known interface/state contract | Required, unshown |
| D. New information usable | Yes **if already queried** | Yes **if AA query already used** | Yes **if interface/state queried** | Required, unshown |
| E. New semantic categories | Requires schema/hook/consumer update or new registered transform | Constrained by existing AA API | Requires new interface/consumer or new transform | Only proposed; no automatic old-consumer uptake |
| F. Cross-representation checks | Manual adapters/witnesses | LLVM IR focused | Dialects/type conversion within MLIR IR framework | Proposed; not integrated |
| G. Trust, revocation, conflicts | Must be designed; control is not a soundness proof | Analysis validity within LLVM-managed IR, not untrusted law proofs | Conservative interfaces/state, not untrusted law proofs | Proposed trust/revision calculus; UT has narrower module verification |
| H. Total cost and simplicity | Small control runs; integration costs unmeasured | Mature framework, substantial LLVM adoption cost for external DSL | Mature IR framework, substantial MLIR adoption cost for external DSL | Not measured; central kernel adds concepts |

**Legality versus feasibility versus profitability:** proof obligations must establish transformation legality before cost ranking. UT's current `LanguagePlan` route cost is a *configuration/artifact path* metric, not automatically a program-dependent transformation profitability model.

## Strong baseline and what would defeat it

The single strongest **minimal** baseline is ordinary C# interfaces and explicit adapters; it has few abstractions, can be used from the current .NET toolchain and was able to pass the limited decision tests without a new fact ontology.

The strongest **LLVM-centric** baseline is `AAManager/AAResults + unchanged pass + IR-specific validators`. The strongest **MLIR-centric** baseline is `ExternalModel + DataFlowSolver + shared transformation`. Those contenders are unlike-for-like where IR restrictions matter; document the unavoidable mapping/adapter cost rather than hiding it.

The proposed UT evidence service merits implementation **only** when an independently-authored second language and two distinct consumers need shared revision-aware semantics, with fewer pairwise changes, zero unsafe rewrites and feasible overhead relative to the best domain-matched baseline. A tie or simpler baseline wins against the general kernel.

## Remaining evidence gaps

No pinned LLVM `opt`/MLIR `mlir-opt` fixture, no actual UT N+1 integration, no all-system equal-budget measurement, no source-to-execution SafeIndex oracle, no independent hostile providers, no separately verified mapping witness, no completed audience study. Do not infer numerical superiority or scientific novelty.

Related documents: [answers to Q01–Q42](CRITICAL_QUESTIONS.md), [experiment pre-registration](EXPERIMENT_DESIGN.md), [bounded findings](SEMANTIC_COMPOSITION_FINDINGS.md), [meetup claim ledger](MEETUP_NARRATIVE_EVIDENCE.md).
