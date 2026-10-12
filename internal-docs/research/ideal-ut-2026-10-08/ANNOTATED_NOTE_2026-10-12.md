# Annotated original IUT note — conceptual claims and all 15 IR questions

**Status:** review of user-supplied `IUT_заметки_расшифровка.md`, transcribed from a one-page `IUT (4).pdf`; only transcription bytes were available for this review. The original handwriting was **not** visually rechecked. `[?]` ambiguities remain unresolved rather than silently repaired. Interpretation below is explicitly an interpretation, not a source quotation. The verbatim historical note is not in this repository.

## A. Original claims: intent, assumptions, alternatives and action

| Note idea (faithful short rendering) | Interpreted need / hidden assumption | Existing UT evidence / prior art | Assessment and falsifiable action |
| --- | --- | --- | --- |
| `∀ obj + context ≅ algebra` | Same bit-pattern can denote different values/operations under context; assumes every object has useful algebra | UT `CallableDescriptor` and semantic type IDs; algebraic effects/typeclasses; MLIR op interfaces | **REVISE.** Formalize `interpret: Representation × Domain × Environment → Meaning`; algebra only when operations and laws exist; demonstrate integer mod-256 vs real/FP semantics |
| `n = 00000101₂`, `n∈ℤ`/ `n∈ℝ`, `⊕` | Distinguish carrier, representation, semantics and overflow | callable-first SSA and MLIR typed operations | **RETAIN intuition.** Bits alone do not decide signedness, trap, FP/NaN or overflow; tests must vary domain policy |
| `Preds: Fact*→Fact` [?] | Composed inference may derive new semantic facts | semantic-fact research; Silver attributes; Soufflé | **CONDITIONAL.** Typed Horn rules only after simple query baseline fails; ensure termination and scope |
| “Environment is first-class” | Explicit state/context/control over effects | Racket interpreters, algebraic effect handlers, explicit C# contexts | **REVISE.** Separate lexical scope, runtime store, target environment and evidence world; use explicit immutable snapshot or capability parameter only when needed |
| `env.push_producer`, `env.push_algebra`, `add(x,y)` | Data/behavior decoupling across modules; concrete implementation hidden | C# extension services, visitor/handler, PR #371 prototype (unmerged) | **TEST**, not a universal API; compare three approaches on plugin reusability and boilerplate |
| “Obj must be DTO” [?]; “Functions only in Algebra” [?] | Prevent behavior coupling and mutable shared state | pure core patterns, MLIR operations | **REJECT universal quantifier.** Some owned behavior, typed invariants and validation methods are safer than DTOs with unchecked external operations; holdout authoring test |
| “Feature preferred over modules/groups” | Public unit should describe user-visible capability, not deployment topology | existing FeatureSdk, package, contribution | **KEEP distinction.** Feature=capability; package=distribution; contribution=executable/service offering; avoid inventing another feature hierarchy |
| `Compiler→Dialect→Feature→Module→Entity/Algebra/Semantic` | Language assembly from independently developed pieces | existing LanguageDefinition→LanguageCompiler→LanguagePlan→LanguageRuntime | **REVISE ownership.** Dialect/configuration must compile into one definition; never make `Compiler` second global selection owner |
| `Semantic* = Relation* ∈ (Fact*→Fact*)` [?] | Relations/inference encode semantic compatibility | current semantic research, dataflow/attributes | **UNRESOLVED literal meaning.** Split predicate schema, claim, inference, verifier, obligation and semantics; no direct source interpretation beyond [?] |
| “Facts may contain facts / split facts” | Structured evidence with derivation | records, typed product types, proof DAGs | **NO SPECIAL NESTING REQUIRED.** Use structured payload + provenance edges; only add decomposition rules when actual consumers need them |
| “No module directly depends on another” | Open-world independence | current FeatureSdk and package contracts; ableC/Silver modular composition | **REVISE to no concrete *incidental* dependency.** Stable shared contracts and intentional required feature dependencies are legitimate |
| “Same semantics on platforms; no UB/exceptions” | Observable backend parity | existing Wist interpreter/CIL parity; CompCert formal preservation | **NEW DIALECT SPEC REQUIRED**: define traps/errors, integer/FP/pointer/heap/races; no blanket guarantees |
| “C-like; add dynamic typing and System F for +perf?” | Evaluate semantics vs representational/runtime choices | System F theory; typed/erased polymorphism; Truffle specializing interpreters | **SEPARATE objectives**. System F does not imply +perf; implement baseline, then controlled type-erasure/specialization comparisons |
| “AI agent should not know future extensions” | N+1 noninterference, prevent overfitting | held-out independent author tests | **STRONG TEST DESIGN.** Freeze consumer/contract before disclosing RangeV2/SIMD/other extension |
| “Agent only reads contracts; CLI/MCP help” | Token economy, less wrong-code coupling | schema/tooling and agent literature remain inconclusive for UT | **CONDITIONAL.** Compare plain docs vs indexed contracts vs generator with correct builds, review time, total context tokens and regressions; allow source reads when contract insufficient |

**Terminology repair:** `data` is not `meaning`; `algebra` is not `semantics`; `environment` is not always evaluation context; `feature` is not `package`; `capability` is not a verified behavioral law; `type safety` is not observational preservation; `source mapping` is not a semantic correspondence proof. See [review](REVIEW_2026-10-12.md).

## B. Representation questions: all 15 separately

The numbered headings reproduce the 15 questions *in the transcription's order* with modest punctuation normalization. A response describes the most plausible engineering meaning, not a newly discovered theorem.

### 1. Представление кода — это feature?
**Answer:** usually **artifact kind plus versioned contract**, not necessarily a user-facing feature. A package may *offer* the ability to generate/use it as a contribution; a dialect may request that capability. Current evidence: `LanguageArtifactContract` route endpoints exist. **Test:** independent new IR provider composes without changing older producers/consumers beyond an explicitly necessary adapter.

### 2. Как должны выглядеть представления по своей структуре?
**Answer:** structure is representation-specific: tree nodes and spans for AST, operation graphs/regions for MLIR-like models, basic blocks/values for SSA, explicit continuations in CPS. Require explicit invariant checker, identity, ownership, revision, effects/observations as appropriate. **Unknown:** a single current UT abstraction sufficient across multiple independently authored IR families. **Test:** introduce structurally incompatible holdout IR.

### 3. `optimized AST ≟ AST`?
**Answer:** usually same *family/schema*, different artifact instance and revision; it may cease to preserve lossless concrete syntax or source trivia. Equality of representation family does not imply semantic equivalence or identity of nodes. **Test:** optimization changes evaluation order or removes source spans; validate preserved observations independently.

### 4. `pruned SSA ≟ minimal SSA ≟ SSA`?
**Answer:** alternative SSA construction algorithms/normal-form constraints, not automatically distinct semantic languages. “Minimal” and “pruned” differ in phi placement, including liveness sensitivity. Call variants `SSA + invariants` rather than conflating them with separate backends; a verified converter may normalize. **Test:** liveness and phi coverage corpus, dominance and use-before-def verification.

### 5. Могут ли представления иметь внутренние представления?
**Answer:** yes; nested regions, graphs, syntax quotations, embedded DSLs and closures exist. Parent and nested artifact contracts must name ownership, scoping, symbol capture and verifiers. **Risk:** a nested IR silently skips parent effects. **Test:** nested region with exceptions and symbol capture.

### 6. Как правильно вставлять обработку представления, которой не было изначально?
**Answer:** register independently authored, typed transformation contributions; one `LanguageCompiler` selects routes; `LanguageRuntime` materializes only selected routes. If no legal route, fail and explain. **Test:** add third-party pass to old frozen language definition via explicit new selection, without modifying the old pass assemblies or creating another selector.

### 7. Какая иерархия представлений?
**Answer:** **not necessarily a tree.** Better model a graph of typed artifact contracts and explicit validated conversions. A partial order can encode abstraction/lowering when actually meaningful; cross-family conversions are not always reversible. **Test:** round-trip versus intentionally one-way lowering, effects and loss of source information.

### 8. М. б. представления, features, etc. можно обобщить до чего-то более абстрактного?
**Answer:** shared declarative “contribution with typed input/output/requirements” is useful; identity and semantics of *features* and *artifacts* remain distinct. **Reject** a universal Entity/Algebra abstraction until the same contract measurably simplifies independent authoring. **Test:** generic abstraction against direct type-safe interfaces with LOC/adapters/diagnostics.

### 9. Self-bootstrapping.
**Answer:** ambiguous: could mean self-hosted implementation language, source-generated toolchain, or language defining its own compiler. Do **not** silently resolve. Start by defining which bootstrap and a trust chain. **Status: UNRESOLVED;** not a prerequisite for E1 nor language composition.

### 10. Какие контракты имеют представления?
**Answer:** `kind/version`, schema, invariants, node/value identity, scope, revision, validity, serialization, ownership, observations, supported transformations. Current typed artifact contract ensures route connectivity, **not** semantic equivalence. **Test:** version/schema mismatch and malformed SSA must fail closed.

### 11. Как не терять семантическую инфу?
**Answer:** explicit properties tied to exact subject/snapshot; preserve only when transform has justified transport or reanalysis; track provenance and invalidation. An origin/source-span map is not a proof. **Test:** one-to-many AST→AIR rewrite, changed overflow/trap semantics and stale mapping.

### 12. Как добавлять обработчики представлений?
**Answer:** typed handlers registered by semantic operation/representation family (when warranted); provider selection via the existing plan; no central operation switch when independent extension is objective. PR #371 tests *internal* Wist handler registration and is **unmerged**, not full provider composition. **Test:** external package third-party operation without old-source edits.

### 13. Какие контракты обработчиков? Умная типизация?
**Answer:** signature, accepted artifact invariant, effects, preconditions, produced result invariant, applicability/decline diagnostics, lifetime, target constraints and verifier strategy. “Smart typing” needs an exact meaning: static generic interfaces and refinements may avoid dynamic casts but do not prove semantic preservation. **Test:** incompatible generic/malformed operation and ambiguous handler providers.

### 14. Какие виды типизаций ∃ на ∀ уровне архитектуры и абстракций?
**Answer:** distinguish user language typing, meta/authoring API typing, typed IR values, predicate schemas, effect capabilities, refinement/flow facts and backend ABI types. Not one universal relation; conversions need checked boundaries. **Test:** polymorphic callable passes through a typed IR and backend lowering with explicit erasure.

### 15. Нам нужна некая иная типизация кроме умной?
**Answer:** possibly effect, ownership/borrow, region, refinement or dependent typing **where a workload requires them**. “Smart” has no source-defined formal meaning; no evidence warrants a mandatory new universal type system. **Test:** contrast explicit effect summaries vs refinement vs ordinary typed interface for bounds elimination and controlled FP/overflow.

## C. Decisionable hypotheses from the note

1. **N+1:** an unchanged bounds-check consumer gains safe opportunities from `RangeV2` only when independently verified scope and length/range facts match; compare direct C# interfaces.
2. **Representation:** no fact crosses AST/AIR/SSA unless a verifier certifies its *particular* transport, or it is recomputed.
3. **Agent authoring:** blind independent author/agent implements an unanticipated feature from public contracts; count correct outputs, affected prior assemblies and complete engineering cost.
4. **Cross-target semantics:** interpreter/CIL/other backends preserve a precisely specified observable behavior, not merely the same happy-path numeric value.
5. **Generalization kill:** if a simple typed interface solution meets these requirements with less infrastructure, do not add a universal algebra, fact store or rule engine.

Next: [review and decisions](REVIEW_2026-10-12.md), [experiment owners](EXPERIMENTS.md), [source lineage](SOURCES.md).
