# Critical questions: answers and evidence, stage 2

**Status:** bounded source review + one local direct-interface control, **not** an integrated four-system comparative experiment.
**Reviewed:** 2026-10-23. **UT source commit:** [`de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1`](https://github.com/Misha1302/UniversalToolchain/tree/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1).
**Input:** [QUESTION_LEDGER_V1](ADVERSARIAL_QUESTION_LEDGER_2026-10-22.md), stable question IDs and original wording.
**Provenance / no overclaims:** Implementation status is taken from reviewed source files, not the Ideal UT proposal. LLVM/MLIR descriptions below refer to documented upstream API behavior and are **not claims that our own production A/B experiment ran**. A 12-assertion local C# decision-only control was executed, but it does **not** remove machine bounds checks.

## How to read this ledger

- `SUPPORTED` = narrow statement directly demonstrated by source implementation/API; not universal soundness.
- `PARTIAL` = real relevant source evidence or bounded execution, but the discriminating integration test is absent.
- `REFUTED` = the exact claim is contradicted by appropriate evidence (none used here to avoid overstating experiment strength).
- `UNTESTED` = proposed test has not run.
- `UNDETERMINED` = competing interpretations remain unresolved.
- "Narrative" means prospective claim hygiene; slides and talk topic were **not** changed.
- Links `U*`, `L*`, `M*`, `S*`, `V*`, `B1` expand in the [source index](#primary-source-index). `B1` is local-only unless separately published.

## EVIDENCE_BRIEF_V1

1. [LLVM AAManager](https://llvm.org/docs/doxygen/classllvm_1_1AAManager.html) already aggregates independently registered analyses for unchanged consumers of `AAResults`. [MLIR external models](https://mlir.llvm.org/docs/Interfaces/) and [bufferization](https://mlir.llvm.org/docs/Bufferization/) independently provide analogous extensibility patterns. Abstract "N+1 improves old optimizer" is therefore not a defensible originality claim.
2. The source-implemented UT strength is **deterministic global selection and exact route/runtime materialization** from `LanguageDefinition → LanguageCompiler → LanguagePlan → LanguageRuntime`, independently of a particular language family. This does not make program-specific optimizations automatically legal.
3. Evidence handling in UT is **narrowly real**: module contracts, `ReverificationRequest` and deterministic verifier scheduling. The general cross-provider fact calculus, proof provenance, trusted laws and transport across IR are still proposals.
4. A local, separate-project C# implementation exercised twelve decision predicates successfully with an unchanged consumer, including Unknown, conflict, stale revision, wrong claimed implementation hash and explicit mutation. Because this is a mock decision gate—not real code generation or independently certified digest—it falsifies no general safety claim and proves no UT integration. It does demonstrate why simpler architectures must be treated seriously.
5. [PlanFuzz](../../proposals/planfuzz/implementation-status.md) and [ContractExperiments](../../../UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/STUDY_PROTOCOL_V3.md) have tangible, inspectable experimental code/protocols and may support a stronger talk once compared against honest baselines.

## ANSWER_LEDGER_V1 — 42 IDs

### Q01 — Может ли MLIR ExternalModel обогатить старый optimizer через Op/Type Interface без изменения consumer и исходного dialect?

- **Status:** `PARTIAL`. **Answer:** External Models attach Op/Type/Attribute interface implementations outside the original dialect. An unchanged consumer that already queries that interface can observe added semantics.
- **Mechanism / evidence:** Official MLIR Interfaces docs explicitly describe registration and context scope; no frozen-binary A/B experiment was run. [M1](https://mlir.llvm.org/docs/Interfaces/)
- **Remaining uncertainty:** Shared interface and registration remain mandatory; not automatic discovery of arbitrary facts.
- **Architecture consequence:** Reframe novelty beyond independent interface implementation.
- **Meetup narrative:** Demonstrate external registration as strong prior art, not UT invention.
- **Next action:** Build pinned MLIR external-model fixture with IR diff.

### Q02 — Изменяет ли подключение независимого LLVM Alias Analysis через AAManager поведение неизменного LICM/GVN-подобного consumer?

- **Status:** `PARTIAL`. **Answer:** LLVM AAManager composes registered analysis results into AAResults; an existing pass using AA can become more precise without source changes.
- **Mechanism / evidence:** AAManager and AAResults source/API prove aggregation and dependency invalidation; a controlled LICM/GVN behavior-change test was not run. [L1](https://llvm.org/docs/doxygen/classllvm_1_1AAManager.html) [L2](https://llvm.org/doxygen/classllvm_1_1AAResults.html)
- **Remaining uncertainty:** The configured AA chain/order matters and existing consumer must already call AA.
- **Architecture consequence:** Abandon unique N+1 claim as an abstract principle.
- **Meetup narrative:** Show LLVM AA as primary counterexample.
- **Next action:** Run fixed-pass AA baseline and compare IR, query traces and registration changes.

### Q03 — Могут ли MLIR DataFlowAnalysis и общий AnalysisState вывести SafeIndex из Range и Shape без собственного rule engine?

- **Status:** `PARTIAL`. **Answer:** MLIR DataFlowSolver loads child analyses, tracks cross-analysis dependencies, and iterates to a fixed point; custom SafeIndex combining Range+Shape is not yet executed.
- **Mechanism / evidence:** DataFlowSolver source/API and tutorial establish multi-analysis mechanics, not this exact derived judgement. [M2](https://mlir.llvm.org/doxygen/classmlir_1_1DataFlowSolver.html)
- **Remaining uncertainty:** No proof of complete safety obligations, non-SSA fact transport or persistent revision-specific invalidation.
- **Architecture consequence:** Do not introduce a Datalog kernel to solve ordinary dataflow.
- **Meetup narrative:** Compare against MLIR solver first.
- **Next action:** Implement typed Range/Shape dataflow, record Unknown and traces.

### Q04 — Что из предполагаемого calculus фактов, зависимостей и выводов уже реализует LLVM Attributor?

- **Status:** `SUPPORTED`. **Answer:** LLVM Attributor is an existing guided-fixpoint framework for deriving interacting IR properties, including non-attribute properties.
- **Mechanism / evidence:** Upstream Attributor API documents shared deduction and isolated analysis behavior. [L3](https://llvm.org/doxygen/structllvm_1_1Attributor.html)
- **Remaining uncertainty:** UT-specific schemas, external module loading, and cross-IR scope are not thereby solved.
- **Architecture consequence:** Remove any claim that fact fixpoint itself is new.
- **Meetup narrative:** Acknowledge a serious production research antecedent.
- **Next action:** Audit exact Attributor features needed by selected use case.

### Q05 — Какие конкретные сценарии invalidation/reuse нельзя покрыть AnalysisManager и PreservedAnalyses LLVM/MLIR?

- **Status:** `PARTIAL`. **Answer:** LLVM/MLIR have analysis caching and preservation-triggered invalidation; their default cache semantics do not establish external implementation-digest revocation for UT.
- **Mechanism / evidence:** LLVM New Pass Manager, AnalysisManager and MLIR PassManagement specify preserve/invalidate behavior. [L4](https://llvm.org/docs/NewPassManager.html) [M3](https://mlir.llvm.org/docs/PassManagement/)
- **Remaining uncertainty:** No matched invalidation workload has been tested.
- **Architecture consequence:** Focus on selected-world provenance only if it yields observed additional property.
- **Meetup narrative:** Show what is reused versus demonstrably missing.
- **Next action:** Build two-analysis edit/preserve/recompute comparison.

### Q06 — Могут ли MLIR One-Shot Bufferization, AnalysisState и BufferizableOpInterface реализовать смену анализа без переписывания преобразования?

- **Status:** `SUPPORTED`. **Answer:** One-Shot Bufferization supports replaceable AnalysisState and separate BufferizableOpInterface, commonly through external models; the same rewriting engine can query changed analysis.
- **Mechanism / evidence:** MLIR official Bufferization docs explicitly describe modular state, AlwaysCopyAnalysisState and external registration. [M4](https://mlir.llvm.org/docs/Bufferization/)
- **Remaining uncertainty:** An equivalent exact two-provider frozen-binary experiment and mapping to UT cross-IR obligations remain untested.
- **Architecture consequence:** Use MLIR Bufferization as strongest rival, not strawman.
- **Meetup narrative:** Avoid claiming UT invented analysis/transform decoupling.
- **Next action:** Implement same corpus with two analysis-state variants before promotion.

### Q07 — Могут ли обычные C# interfaces, DI и explicit adapters пройти тот же Range+Shape→Bounds corpus проще evidence service?

- **Status:** `PARTIAL`. **Answer:** Direct typed C# contracts are demonstrably sufficient for a small unchanged BoundsOptimization decision consumer using independent Shape/Range assemblies.
- **Mechanism / evidence:** Local .NET 10.0.301 baseline compiled and ran 12/12 decision assertions; this is neither real bounds-check elimination nor a UT/MLIR/LLVM integration test. [B1](EXPERIMENT_DESIGN.md#executed-small-c-control-not-a-ut-integration-experiment)
- **Remaining uncertainty:** No equal-budget comparison, proof of actual target semantics or independent package deployment.
- **Architecture consequence:** Treat simple C# as default until defeated by held-out complexity/correctness tests.
- **Meetup narrative:** Present it as negative pressure on necessity, not a general win.
- **Next action:** Implement full source-to-execution E1 for all contenders.

### Q08 — Даёт ли разнородность AST/Bytecode/AIR/SSA задачу semantic transport, не решаемую столь же просто MLIR multi-dialect conversion?

- **Status:** `UNTESTED`. **Answer:** Distinct AST/Bytecode/AIR/SSA forms do not themselves prove an unsolved general problem; MLIR offers dialect conversion and type materialization.
- **Mechanism / evidence:** MLIR dialect-conversion APIs are documented and UT has separate representations. [M5](https://mlir.llvm.org/docs/DialectConversion/) [U9](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/docs/CURRENT_ARCHITECTURE_STATUS.md)
- **Remaining uncertainty:** No validated matched semantic transport baseline exists.
- **Architecture consequence:** State specific unsupported correspondence or revision obligation before building bridge.
- **Meetup narrative:** Never sell multiple representations as automatic novelty.
- **Next action:** Design one-to-many lowering oracle against MLIR.

### Q09 — Какие исходники, adapters, registries, schema и composition root реально меняются при добавлении нового независимого анализа?

- **Status:** `PARTIAL`. **Answer:** The bounded C# baseline changes host registration/provider selection while Consumer.cs and prior provider source remain untouched.
- **Mechanism / evidence:** Consumer/Shape/Range/Shared projects were separately built and 12 decision assertions ran; all consume a preagreed ISafeIndexQuery. [B1](EXPERIMENT_DESIGN.md#executed-small-c-control-not-a-ut-integration-experiment)
- **Remaining uncertainty:** No measured published package upgrade, binary ABI matrix, central registry changes or real optimizer rewrite.
- **Architecture consequence:** Measure A and B separately from C and D; require all integration edits in reports.
- **Meetup narrative:** Say no consumer edits only with explicit registration caveat.
- **Next action:** Freeze binaries and count integration LOC with E1.

### Q10 — Кто использует новую категорию факта, если старый optimizer даже не знает о SafeIndex и не имеет общей точки запроса?

- **Status:** `PARTIAL`. **Answer:** A consumer cannot act on a wholly unqueried semantic category without some predefined generic hook, transformation registry or updated consumer logic.
- **Mechanism / evidence:** The C# sample consumer queries a deliberately predeclared ISafeIndexQuery; MLIR and LLVM also rely on known interfaces. [B1](EXPERIMENT_DESIGN.md#executed-small-c-control-not-a-ut-integration-experiment) [M1](https://mlir.llvm.org/docs/Interfaces/) [L1](https://llvm.org/docs/doxygen/classllvm_1_1AAManager.html)
- **Remaining uncertainty:** No experiment meaningfully adds a never-before-defined consumer obligation.
- **Architecture consequence:** Ban fully automatic semantic discovery wording.
- **Meetup narrative:** Make the predetermined contract explicit in story.
- **Next action:** Test identical consumer with and without registered query hook.

### Q11 — Как версионные shared semantic contracts переживают обновление provider без пересборки consumer?

- **Status:** `UNTESTED`. **Answer:** Stable type names do not automatically solve semantic version evolution or binary compatibility.
- **Mechanism / evidence:** UT has ToolchainApi checks and canonical manifest identity, but cross-version semantic-fact API is proposal only. [U1](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageCompiler.cs) [U11](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/PROPOSED_API_CONTRACTS.md)
- **Remaining uncertainty:** Versioned semantic schemas and migrations are not integrated.
- **Architecture consequence:** Delay public evidence ABI; use versioned small internal contract.
- **Meetup narrative:** Avoid compatibility guarantees.
- **Next action:** Run N/N+1 ABI and schema migration matrix.

### Q12 — Достаточны ли Range и Shape для безопасного удаления Read(a,i) bounds check при aliasing, mutable length, path, exceptions и overflow?

- **Status:** `UNTESTED`. **Answer:** Range and Shape are insufficient alone in presence of mutable length, path uncertainty, aliasing, arithmetic or trap behavior.
- **Mechanism / evidence:** UT proposal itself enumerates extra obligations; local C# sample only gates decisions and never removes an actual instruction. [U7](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/EXPERIMENTS.md) [B1](EXPERIMENT_DESIGN.md#executed-small-c-control-not-a-ut-integration-experiment)
- **Remaining uncertainty:** No independent execution/translation-validation oracle yet.
- **Architecture consequence:** Never allow removal based only on bare Range/Shape.
- **Meetup narrative:** Explain why this is a stringent future experiment, not success.
- **Next action:** Build real checked Read lowering and fail-safe oracle.

### Q13 — Как запретить использование CFG-local i<len(a) за пределами доказанного пути, петли и program point?

- **Status:** `UNTESTED`. **Answer:** CFG-local knowledge must carry program-point and path scope; a revision/value pair is insufficient.
- **Mechanism / evidence:** Proposed SemanticQueryContext specifies program point/path; no integrated E2 witness proof exists. [U11](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/PROPOSED_API_CONTRACTS.md)
- **Remaining uncertainty:** No CFG mutation test against implemented UT semantic service.
- **Architecture consequence:** Bind facts to control context or return Unknown.
- **Meetup narrative:** Use conditional and loop examples to communicate risk.
- **Next action:** Construct branch merge/loop/invalidation fixtures.

### Q14 — Кто удостоверяет сохранение фактов между анализом, промежуточным transform A и optimizer B?

- **Status:** `PARTIAL`. **Answer:** Typed invalidation and reverification scheduling exist in UT module contracts, but an author-declared preserves claim is not an independent semantic proof.
- **Mechanism / evidence:** Production ModuleContractVerificationScheduler distinguishes P2 selective and P3 always verification; broader fact derivation is proposal. [U6](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.ModuleContracts/ModuleContractVerificationScheduler.cs) [U10](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/STUDY_PROTOCOL_V3.md)
- **Remaining uncertainty:** No general cross-provider semantic preservation guarantee for arbitrary optimizer effects.
- **Architecture consequence:** Reuse production obligations while requiring independent verifier for unsafe change.
- **Meetup narrative:** Discuss verification of selected boundaries, not universal correctness.
- **Next action:** Introduce intentionally lying preserves producer and independent oracle.

### Q15 — Как предотвратить опасную оптимизацию от заведомо ложного факта стороннего provider (Pure/NoAlias/SafeIndex)?

- **Status:** `PARTIAL`. **Answer:** A typed false premise remains false: the semantic evidence calculus cannot manufacture trust. UT current module traits are attestations, not hostile-plugin proofs.
- **Mechanism / evidence:** Current architecture explicitly disclaims sandbox/soundness; local baseline conservatively rejects contradictory values but not colluding false positive claims. [U9](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/docs/CURRENT_ARCHITECTURE_STATUS.md) [U11](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/PROPOSED_API_CONTRACTS.md) [B1](EXPERIMENT_DESIGN.md#executed-small-c-control-not-a-ut-integration-experiment)
- **Remaining uncertainty:** No malicious independent provider run against UT; verifier coverage limited to represented contracts.
- **Architecture consequence:** Independent verifier or conservative fallback is mandatory.
- **Meetup narrative:** Do not claim untrusted plugin safety.
- **Next action:** Test intentionally false positive SafeIndex with independent oracle.

### Q16 — Нужна ли четырёхзначная Proven/Disproven/Unknown/Contradiction модель против простого conservative lattice и failure?

- **Status:** `UNDETERMINED`. **Answer:** Four-state logic can distinguish negative, absent and contradictory evidence, but no consumer case proves that four externally visible states are the smallest adequate model.
- **Mechanism / evidence:** Proposed contract specifies states, not tested production API. [U11](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/PROPOSED_API_CONTRACTS.md)
- **Remaining uncertainty:** Lattice/explicit failure alternatives not compared.
- **Architecture consequence:** Keep model experimental and internal.
- **Meetup narrative:** Avoid teaching speculative status algebra in short talk.
- **Next action:** Compare truth tables and observable actions with lattice baseline.

### Q17 — Как доказать DependencySetComplete, если часть зависимостей определяется неизвестными независимыми модулями?

- **Status:** `UNTESTED`. **Answer:** In open-world analysis all-dependencies judgements require independently established completeness rather than absence of contrary facts.
- **Mechanism / evidence:** Proposal explicitly requires DependencySetComplete. [U7](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/EXPERIMENTS.md) [U11](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/PROPOSED_API_CONTRACTS.md)
- **Remaining uncertainty:** No production completeness certificate on an independent extension graph was run.
- **Architecture consequence:** Disallow universal quantification over nonclosed dependency sets.
- **Meetup narrative:** Use missing-dependency counterexample if discussing design.
- **Next action:** Create graph omission oracle and verifier-signed completeness witness.

### Q18 — Сохраняются ли факты при замене DLL/backend/native dependencies, когда PlanHash не изменился?

- **Status:** `PARTIAL`. **Answer:** UT binding verifies selected package registration/manifest and plan canonical hashes; this is not a general semantic-proof revocation model keyed on executable world.
- **Mechanism / evidence:** LanguagePlanCanonicalizer hashes manifest identities; LanguageRouteRuntimeAssembler validates selected runtime component source bindings. [U3](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.Runtime/LanguageRouteRuntimeAssembler.cs) [U4](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.LanguageSdk/LanguagePlanCanonicalizer.cs)
- **Remaining uncertainty:** Native dependency/environment changes and revised fact provenance not studied.
- **Architecture consequence:** Separate plan-data identity from runtime-evidence scope.
- **Meetup narrative:** State precise hash/provenance limits.
- **Next action:** Swap DLL/native dependency with same config and inspect failure/reuse.

### Q19 — Реально ли отзывать только нужные derived facts без false-preserve и без почти полного recomputation?

- **Status:** `UNTESTED`. **Answer:** No general selective revocation of cross-provider derived semantic facts is established.
- **Mechanism / evidence:** Existing P2/P3 verification scheduler is narrower; Salsa provides prior art for revisioned incremental queries. [U6](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.ModuleContracts/ModuleContractVerificationScheduler.cs) [S1](https://salsa-rs.github.io/salsa/reference/algorithm.html)
- **Remaining uncertainty:** No cost/correctness comparison against full recompute across arbitrary edits.
- **Architecture consequence:** Do not construct complex invalidation DAG before measured use case.
- **Meetup narrative:** Acknowledge incremental-computation prior art.
- **Next action:** Run holdout edit streams against full-recompute reference.

### Q20 — Независим ли inference result от порядка регистрации providers, scheduling, рекурсии и исчерпания budget?

- **Status:** `PARTIAL`. **Answer:** UT configuration planning is deterministic by design and tested, but arbitrary future inference-rule scheduling is not implemented or proven deterministic.
- **Mechanism / evidence:** LanguageCompiler/PlanHash route logic supports the former; MLIR solver implements a known fixed-point regime. [U1](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageCompiler.cs) [U4](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.LanguageSdk/LanguagePlanCanonicalizer.cs) [M2](https://mlir.llvm.org/doxygen/classmlir_1_1DataFlowSolver.html)
- **Remaining uncertainty:** No general rule engine test or recursion/time budget stress on UT.
- **Architecture consequence:** Do not transfer guarantees from one subsystem to another.
- **Meetup narrative:** Mention determinism only for actual plan selection.
- **Next action:** Permutation, cyclic rule, and budget tests if kernel proceeds.

### Q21 — Можно ли одним Pure/Associative/Equivalent охватить checked integers, FP, exceptions, target effects?

- **Status:** `UNTESTED`. **Answer:** Semantic laws must be qualified by numeric domain, target and observation model; one unqualified Pure/Associative flag is unsafe.
- **Mechanism / evidence:** Proposal acknowledges checked/unchecked, FP and effects; Alive2 is a production-level bounded validation alternative for LLVM IR. [U11](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/PROPOSED_API_CONTRACTS.md) [V1](https://github.com/AliveToolkit/alive2)
- **Remaining uncertainty:** No UT generic semantic vocabulary tested across these cases.
- **Architecture consequence:** Do not ship generic global algebraic law flags.
- **Meetup narrative:** Use tiny counterexample rather than generic purity claims.
- **Next action:** Create FP NaN/signed-zero/overflow/trap corpus.

### Q22 — Что именно наследует факт при one-to-many AST→Bytecode→AIR→SSA lowering?

- **Status:** `UNTESTED`. **Answer:** Source-origin anchors cannot alone certify preservation of a fact through one-to-many lowering.
- **Mechanism / evidence:** UT research proposal explicitly demands checked correspondence, not implemented across AST/AIR/SSA. [U11](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/PROPOSED_API_CONTRACTS.md) [M5](https://mlir.llvm.org/docs/DialectConversion/)
- **Remaining uncertainty:** No actual mapping witness or independent translation check was run.
- **Architecture consequence:** Defer representation-neutral evidence transfer.
- **Meetup narrative:** Explain source lineage versus semantics.
- **Next action:** Produce source and target IR, mapping witness and independent check.

### Q23 — Когда верное происхождение объекта НЕ доказывает переносимость свойства?

- **Status:** `PARTIAL`. **Answer:** MLIR Dialect Conversion tracks type remapping and legality, while semantic properties still need observation-specific preservation arguments.
- **Mechanism / evidence:** Official MLIR conversion docs support type/operation legality, not arbitrary behavioral equivalence; Alive2 validates bounded LLVM refinements. [M5](https://mlir.llvm.org/docs/DialectConversion/) [V1](https://github.com/AliveToolkit/alive2)
- **Remaining uncertainty:** No checked-to-unchecked UT transport implementation tested.
- **Architecture consequence:** Never use origin as permission for optimization.
- **Meetup narrative:** Differentiate type correctness from semantic equivalence.
- **Next action:** Run checked/unchecked and exception-order counterexamples.

### Q24 — Нужно ли SSA для заявленного механизма или хватает Bytecode/AIR?

- **Status:** `PARTIAL`. **Answer:** UT routes keep SSA optional; current Wist AIR→SSA→AIR route is opt-in rather than mandatory.
- **Mechanism / evidence:** Current architecture and Wist policy document optional SSA behavior. [U9](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/docs/CURRENT_ARCHITECTURE_STATUS.md)
- **Remaining uncertainty:** No matched N+1 evidence engine with and without SSA.
- **Architecture consequence:** No mandatory SSA dependency in any experimental design.
- **Meetup narrative:** Use only verified optionality claim.
- **Next action:** Run same semantic query on Bytecode/AIR vs SSA.

### Q25 — Как сохранить идентичность смыслового субъекта после inlining, CSE, duplication и code motion?

- **Status:** `UNTESTED`. **Answer:** Stable semantic subjects through inlining, CSE, duplication and code motion are not a shipped generic UT feature.
- **Mechanism / evidence:** Proposed API requires AnchorMappingWitness but is explicitly not compiled public code. [U11](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/PROPOSED_API_CONTRACTS.md)
- **Remaining uncertainty:** No end-to-end remapping test.
- **Architecture consequence:** Invalidate ambiguous mappings by default.
- **Meetup narrative:** Keep semantic identity a research challenge.
- **Next action:** Run rewrite corpus with one-to-many witness checking.

### Q26 — Кто решает program-local legality двух программ с одним и тем же LanguagePlan, не запуская второй config planner?

- **Status:** `PARTIAL`. **Answer:** LanguagePlan chooses global route and components; program-local transform legality is a separate responsibility and cannot be inferred from minimum route cost.
- **Mechanism / evidence:** LanguageCompiler and LanguageArtifactRoutePhase source inspect contributions/backends/cost, not program-specific SafeIndex facts. [U1](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageCompiler.cs) [U2](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageArtifactRoutePhase.cs)
- **Remaining uncertainty:** No fully integrated per-program semantic evidence scheduler.
- **Architecture consequence:** Preserve single config planner, add no new global resolver.
- **Meetup narrative:** Frame as separation of configuration versus program semantics.
- **Next action:** Freeze one LanguagePlan and compare two program-specific obligations.

### Q27 — Каковы границы interpreter/CIL parity по exception/effects/overflow, а не только по возвращаемому value?

- **Status:** `PARTIAL`. **Answer:** Value parity is weaker than equivalence of errors, ordering, side effects, memory and numeric corner cases.
- **Mechanism / evidence:** UT has interpreter/CIL parity tests and bounded Wist restricted subsets, not unrestricted equivalence. [U8](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/proposals/planfuzz/implementation-status.md) [U9](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/docs/CURRENT_ARCHITECTURE_STATUS.md)
- **Remaining uncertainty:** No independent full observation-model comparison.
- **Architecture consequence:** Scope parity claims to shared supported behavior.
- **Meetup narrative:** Avoid universal interpreter/CIL equivalence wording.
- **Next action:** Extend corpus with traps, effects and backend-sensitive cases.

### Q28 — Где фактически кончается global config planning в LanguageCompiler и начинается анализ конкретной программы?

- **Status:** `SUPPORTED`. **Answer:** Current LanguageCompiler resolves features/contributions/providers and LanguageArtifactRoutePhase builds costed artifact routes from declared descriptors; it does not take source-level program facts as input.
- **Mechanism / evidence:** Direct reviewed source signatures and bodies identify config-only inputs. [U1](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageCompiler.cs) [U2](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageArtifactRoutePhase.cs)
- **Remaining uncertainty:** This says nothing about completeness of downstream Wist optimizer legality checks.
- **Architecture consequence:** Document boundary; do not replace runtime planner.
- **Meetup narrative:** Distinguish graph selection from per-program proof in the talk.
- **Next action:** Preserve architecture ownership, no production change.

### Q29 — Какие структурно корректные, но семантически неверные программы проходят Bytecode/AIR/module verifiers?

- **Status:** `PARTIAL`. **Answer:** UT module-contract/Bytecode/AIR verification is real but bounded to represented typed metadata, ownership, IR structure and routed verifier obligations.
- **Mechanism / evidence:** Production scheduler/verifier files and CURRENT_ARCHITECTURE_STATUS explicitly exclude a universal semantic proof. [U6](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.ModuleContracts/ModuleContractVerificationScheduler.cs) [U9](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/docs/CURRENT_ARCHITECTURE_STATUS.md) [U10](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/STUDY_PROTOCOL_V3.md)
- **Remaining uncertainty:** No systematic invalid-but-well-formed corpus run here.
- **Architecture consequence:** State verifier-specific guarantee rather than mathematical optimizer correctness.
- **Meetup narrative:** Show one verifiable negative contract case if chosen as story.
- **Next action:** Create independent semantics-preserving and semantics-breaking mutants.

### Q30 — Доказывает ли Acme.PricingLanguage семантическое N+1 расширение или только независимость одного языка от Wist?

- **Status:** `SUPPORTED`. **Answer:** The Acme sample demonstrates a non-Wist language with its own parser and interpreter/compiled backends, not N+1 semantic inference among multiple independent analyses.
- **Mechanism / evidence:** Program.cs registers a single authored language package and runs two backends. [U5](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/samples/Acme.PricingLanguage/Program.cs)
- **Remaining uncertainty:** General SDK external multi-package behavior has other tests, but this sample alone is not semantic cooperation proof.
- **Architecture consequence:** Keep Acme as non-Wist ownership evidence only.
- **Meetup narrative:** Never cite Acme as cross-provider semantic N+1 demo.
- **Next action:** Build separately shipped packages for E1.

### Q31 — Какие реальные AIR↔SSA↔AIR transforms семантически проверены, а где есть только structural gates?

- **Status:** `PARTIAL`. **Answer:** Wist AIR→SSA→AIR is optional and verifier-gated with constrained operation support; it is not a generic proof transport service.
- **Mechanism / evidence:** Current architecture and PlanFuzz Wist SSA policy variants are documented and test-backed historically. [U8](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/proposals/planfuzz/implementation-status.md) [U9](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/docs/CURRENT_ARCHITECTURE_STATUS.md)
- **Remaining uncertainty:** No comprehensive per-operation semantic proof matrix inspected here.
- **Architecture consequence:** Restrict SSA claims to actual route and verifier.
- **Meetup narrative:** Prefer tested route parity to a general SSA novelty narrative.
- **Next action:** Review pass-specific postconditions and negative cases.

### Q32 — Находит ли PlanFuzz классы composition/activation defects сверх обычного differential fuzzing при сопоставимом бюджете?

- **Status:** `PARTIAL`. **Answer:** PlanFuzz implements configuration-aware relational oracles, replay and reduction as experimental tooling; no fair equal-budget comparison establishes superiority to conventional fuzzers.
- **Mechanism / evidence:** Non-packable adapter/Core/CLI tests and implementation-status record describe observed scope and historical regressions. [U8](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/proposals/planfuzz/implementation-status.md)
- **Remaining uncertainty:** Independent competitor baseline and new discovery yield not run.
- **Architecture consequence:** Treat as strongest ready-to-test research candidate, not published victory.
- **Meetup narrative:** Potentially richer and more defensible short talk.
- **Next action:** Compare against same-seed source-only baseline and count unique roots.

### Q33 — Держится ли результат ContractExperiments selective reverification на независимо авторских faults и end-to-end compilation?

- **Status:** `PARTIAL`. **Answer:** ContractExperiments invokes real production verifier components and has frozen corpora, controls and P2/P3 protocol, but current corpus is author-designed and not whole compilation.
- **Mechanism / evidence:** STUDY_PROTOCOL_V3 and VERIFICATION specify policies, oracle, delimiters and explicit timing scope. [U10](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/STUDY_PROTOCOL_V3.md) [U12](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/VERIFICATION.md)
- **Remaining uncertainty:** No independent-blind holdout or new experiment was executed for this review.
- **Architecture consequence:** Do not extrapolate selective-verification results to unseen compilers.
- **Meetup narrative:** A bounded empirical story is promising, with explicit denominators.
- **Next action:** Run external faults and end-to-end workload under frozen oracle.

### Q34 — Сколько стоит UT versus handwritten pipeline для planning/runtime/compile/steady-state, включая amortization?

- **Status:** `UNTESTED`. **Answer:** No equal-workload measured planning/materialization/compile/steady-state comparison supports zero-cost or performance win claims in this review.
- **Mechanism / evidence:** Existing boundary benchmark documents are separate and not controlled A/B/C/D comparisons. [U9](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/docs/CURRENT_ARCHITECTURE_STATUS.md) [B1](EXPERIMENT_DESIGN.md#executed-small-c-control-not-a-ut-integration-experiment)
- **Remaining uncertainty:** No valid numeric estimate; local baseline measured only boolean correctness.
- **Architecture consequence:** Avoid performance adjectives without timing and environments.
- **Meetup narrative:** Talk must not claim zero cost.
- **Next action:** Benchmark cold, warm, allocator, compile, hot and amortization separately.

### Q35 — Может ли выбранный package/runtime policy реально изолировать hostile managed/native plugin?

- **Status:** `SUPPORTED`. **Answer:** UT contract/runtime policy verification does not amount to a hardened sandbox for hostile managed/native components.
- **Mechanism / evidence:** Current architecture explicitly disclaims hardened in-process sandbox and notes attested component traits. [U9](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/docs/CURRENT_ARCHITECTURE_STATUS.md)
- **Remaining uncertainty:** Adversarial isolation of specific host environments not evaluated.
- **Architecture consequence:** Keep trust boundary explicit; never advertise sandboxing.
- **Meetup narrative:** No untrusted-code containment claim.
- **Next action:** Threat-model and isolate if hostile plugins become requirement.

### Q36 — Какую полезную реальную задачу для non-compiler engineer решает предлагаемый механизм лучше простой plugin/workflow системы?

- **Status:** `UNDETERMINED`. **Answer:** No externally validated noncompiler use case shows superior utility of the proposed semantic core over ordinary plugin/workflow architecture.
- **Mechanism / evidence:** Current Acme is a DSL sample and proposed platform/adoption sections are forecasts. [U5](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/samples/Acme.PricingLanguage/Program.cs) [U11](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/PROPOSED_API_CONTRACTS.md)
- **Remaining uncertainty:** No external design partner or adoption metrics.
- **Architecture consequence:** Keep product relevance a hypothesis.
- **Meetup narrative:** Choose audience-appropriate actual demo instead of universal positioning.
- **Next action:** Run one independent workflow/plugin use case and interview users.

### Q37 — Можно ли за 9m30s показать факт проблемы, альтернативу и проверяемый результат без ложных упрощений?

- **Status:** `UNTESTED`. **Answer:** A 9m30s demonstration of the new kernel is not currently defendable as implemented; an existing PlanFuzz or contract-boundary demonstration has a more concrete source basis.
- **Mechanism / evidence:** Meetup deck/script unavailable, no timed dry run. [U8](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/proposals/planfuzz/implementation-status.md) [U10](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/STUDY_PROTOCOL_V3.md)
- **Remaining uncertainty:** No real time and comprehension evaluation.
- **Architecture consequence:** Prefer one narrow observable result.
- **Meetup narrative:** Do not rebuild deck in this stage.
- **Next action:** Inspect real deck and run timed non-author review.

### Q38 — Можно ли пререгистрировать фальсифицируемую гипотезу и отрицательный исход до реализации эксперимента?

- **Status:** `PARTIAL`. **Answer:** The Ideal UT experiment document specifies fail-closed and equal-baseline no-go rules, but the full N+1 protocol has not been executed or externally pre-registered.
- **Mechanism / evidence:** EXPERIMENTS.md contains clear prospective criteria and explicitly says integrated E1–E8 not run. [U7](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/EXPERIMENTS.md)
- **Remaining uncertainty:** No independent frozen experiment result or holdout.
- **Architecture consequence:** Freeze thresholds and corpus before implementation.
- **Meetup narrative:** Keep claims falsifiable, avoid design-vision as evidence.
- **Next action:** Publish full preregistration before E1.

### Q39 — Какие точные statements о LLVM/MLIR опровергнет их штатный production API или минимальный пример?

- **Status:** `PARTIAL`. **Answer:** Any claim that LLVM/MLIR lack composable analyses, external interfaces, invalidation or fixpoint inference is contradicted by documented production mechanisms.
- **Mechanism / evidence:** LLVM AAManager/Attributor/AnalysisManager and MLIR Interfaces/DataFlow/Bufferization directly show the relevant features. [L1](https://llvm.org/docs/doxygen/classllvm_1_1AAManager.html) [L3](https://llvm.org/doxygen/structllvm_1_1Attributor.html) [L4](https://llvm.org/docs/NewPassManager.html) [M1](https://mlir.llvm.org/docs/Interfaces/) [M2](https://mlir.llvm.org/doxygen/classmlir_1_1DataFlowSolver.html) [M4](https://mlir.llvm.org/docs/Bufferization/)
- **Remaining uncertainty:** No expert-reviewed exhaustive counterclaim inventory for entire deck.
- **Architecture consequence:** Remove broad negative competitor statements.
- **Meetup narrative:** Offer respectful concrete comparison with narrowed difference.
- **Next action:** Conduct expert API review and runnable controls.

### Q40 — Сильнее ли история о выявлении ошибок на стыках расширений, чем создание гипотетического semantic kernel?

- **Status:** `PARTIAL`. **Answer:** PlanFuzz and ContractExperiments have substantially more implemented and historically exercised code than the proposed general semantic-evidence kernel, but their comparative scientific value remains unmeasured.
- **Mechanism / evidence:** UT implementation/status/protocol documents support different evidence maturity, not novelty ranking. [U8](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/proposals/planfuzz/implementation-status.md) [U10](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/STUDY_PROTOCOL_V3.md)
- **Remaining uncertainty:** No independent audience, fair baselines or blind faults yet.
- **Architecture consequence:** Keep both alternative narratives alive.
- **Meetup narrative:** Favor reproducible tested engineering over hypothetical theorem engine.
- **Next action:** Stage a controlled research-story evaluation after evidence collection.

### Q41 — Уменьшается ли полная стоимость добавления 1/3/5/10 расширений с учётом glue/tests/schema evolution?

- **Status:** `UNTESTED`. **Answer:** No measured 1/3/5/10 extension integration study establishes lower total engineering cost; source independence alone does not imply lower cost.
- **Mechanism / evidence:** Small C# baseline is insufficient; generic UT composition exists, but no matched authoring-study results. [B1](EXPERIMENT_DESIGN.md#executed-small-c-control-not-a-ut-integration-experiment) [U5](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/samples/Acme.PricingLanguage/Program.cs)
- **Remaining uncertainty:** No developer-time, regression, schema-change or glue budget.
- **Architecture consequence:** Make ROI a measured variable instead of assumption.
- **Meetup narrative:** No broad productivity multiplier claims.
- **Next action:** Run controlled multi-extension authoring study.

### Q42 — При каких заранее заданных исходах следует прекратить разработку общего semantic core?

- **Status:** `PARTIAL`. **Answer:** The proposal already allows abandonment on equal simpler baseline, unsafe transforms, pervasive old-code edits and untrusted proof, but thresholds and integrated runs are not yet frozen.
- **Mechanism / evidence:** UT EXPeriments.md explicit go/no-go and Q07 baseline pressures necessity. [U7](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/EXPERIMENTS.md) [B1](EXPERIMENT_DESIGN.md#executed-small-c-control-not-a-ut-integration-experiment)
- **Remaining uncertainty:** No full E1 or formal precommit record, therefore cannot declare project killed.
- **Architecture consequence:** Keep decision provisional and smallest safe design preferred.
- **Meetup narrative:** State legitimate negative outcomes explicitly.
- **Next action:** Freeze no-go thresholds and stop when met.

## Primary source index

| ID | Exact source (reviewed at UT `de0da4a81b1c` where applicable) |
| --- | --- |
| U1 | [UT LanguageCompiler](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageCompiler.cs) |
| U2 | [UT artifact route search](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageArtifactRoutePhase.cs) |
| U3 | [UT runtime assembler](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.Runtime/LanguageRouteRuntimeAssembler.cs) |
| U4 | [UT plan hashing/verifier](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.LanguageSdk/LanguagePlanCanonicalizer.cs) |
| U5 | [Acme non-Wist sample](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/samples/Acme.PricingLanguage/Program.cs) |
| U6 | [UT reverification scheduler](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.ModuleContracts/ModuleContractVerificationScheduler.cs) |
| U7 | [Ideal UT falsification plan](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/EXPERIMENTS.md) |
| U8 | [PlanFuzz implementation status](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/proposals/planfuzz/implementation-status.md) |
| U9 | [UT current architecture](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/docs/CURRENT_ARCHITECTURE_STATUS.md) |
| U10 | [ContractExperiments V3 protocol](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/STUDY_PROTOCOL_V3.md) |
| U11 | [Ideal UT unimplemented API contracts](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/PROPOSED_API_CONTRACTS.md) |
| U12 | [Verification evidence limits](https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/VERIFICATION.md) |
| L1 | [LLVM AAManager](https://llvm.org/docs/doxygen/classllvm_1_1AAManager.html) |
| L2 | [LLVM AAResults](https://llvm.org/doxygen/classllvm_1_1AAResults.html) |
| L3 | [LLVM Attributor](https://llvm.org/doxygen/structllvm_1_1Attributor.html) |
| L4 | [LLVM new pass manager](https://llvm.org/docs/NewPassManager.html) |
| M1 | [MLIR external interfaces](https://mlir.llvm.org/docs/Interfaces/) |
| M2 | [MLIR DataFlowSolver](https://mlir.llvm.org/doxygen/classmlir_1_1DataFlowSolver.html) |
| M3 | [MLIR pass/analysis management](https://mlir.llvm.org/docs/PassManagement/) |
| M4 | [MLIR One-Shot Bufferization](https://mlir.llvm.org/docs/Bufferization/) |
| M5 | [MLIR dialect conversion](https://mlir.llvm.org/docs/DialectConversion/) |
| S1 | [Salsa incremental recomputation](https://salsa-rs.github.io/salsa/reference/algorithm.html) |
| V1 | [Alive2 bounded LLVM validation](https://github.com/AliveToolkit/alive2) |
| B1 | [Executed local C# baseline](EXPERIMENT_DESIGN.md#executed-small-c-control-not-a-ut-integration-experiment) |

## LIMITS / CRITIQUE

- **LLVM/MLIR engineer:** LLVM AA, MLIR external models, DataFlowSolver, Bufferization, pass invalidation and Attributor all defeat broad novelty claims. No upstream custom plugin or equal-budget IR fixture was compiled by this review.
- **Researcher:** Pinned technical API descriptions are not repeated end-to-end comparative experiments. Every `PARTIAL` and `UNTESTED` needs frozen holdout data before positive scientific claims.
- **Systems architect:** Shared typed interfaces and composition-root glue are substantially simpler than a new ontology; disallow a new core unless demonstrated necessity is independent of appearance/diagram elegance.
- **Speaker:** Distinguish a real demo from an illustrated proposal. Not all 42 questions fit a short talk; a single reversible experiment beats encyclopedic architecture slides.
- **Tool limitation:** No `dotnet`, LLVM `opt` or `mlir-opt` available natively on the checkout; local .NET 10 sidecar ran isolated C# sample, not repository tests. Upstream API findings were checked from official docs, not LLVM/MLIR binary tests. Actual meetup deck/script not present in accessible materials for this review. CI from earlier head is historical unless pinned in a specific run report.
