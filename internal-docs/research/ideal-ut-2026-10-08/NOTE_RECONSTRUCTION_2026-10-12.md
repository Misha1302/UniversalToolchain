# IUT note — source-qualified review and representation questions (2026-10-12)

**Epistemic boundary:** The original `IUT_заметки_расшифровка.md` and `IUT (4).pdf` were **not available** in the accessed `master` tree, current attachments, indexed Library or searched Drive. The available user's master task prompt quotes several ideas and says the note has fifteen representation questions, but does **not** supply their original wording. Accordingly, nothing below is presented as a verbatim or complete line-by-line analysis of the missing note.

The **R01–R15 questions below are independently reconstructed research prompts**, not invented quotations or verified one-to-one mappings to the original fifteen. Once the note is provided, replace these provisional prompts with the original fifteen in order, preserve ambiguities, and cite their exact sections. This missing source **blocks completion of the original-note audit**, not the independent architecture review.

## Known idea groups: original idea → interpretation → evidence → prior art → recommendation → action

| Source-qualified idea (quoted or paraphrased from the current master prompt only) | Interpretation and hidden assumption | Existing UT evidence | Prior art | Recommendation / actionable discriminator |
| --- | --- | --- | --- | --- |
| `∀ obj + context ≅ algebra` | Possibly wants operations interpreted differently under environments; silently assumes every object is an algebraic carrier | Callable descriptors and typed execution stages exist, but no universal object/algebra API is needed | Typeclasses, interpreters, algebraic effects | Reject as **universal axiom**; experimentally model one operation with two context-sensitive interpreters and compare typed handlers |
| First-class environment/context | Lexical name lookup, effects, runtime capabilities and state may have been conflated | Wist binding and execution phases have distinct owners | Environment semantics, scope graphs/Statix | Define precise context lifetime and effects before exposing API |
| DTO-only data and algebra-owned behavior | Preserve data/behavior separation and make handlers composable | PR #371 is an **unmerged** small proof of concept with declarations and handlers | MLIR op interfaces, classic visitor/interpreter | Allow records and handlers for chosen bytecode seam; no repository-wide DTO mandate |
| `Compiler → Dialect → Feature → Module → Entity/Algebra/Semantic` | Tries to make all composition hierarchical, perhaps introducing a second selection authority | One current `LanguageCompiler` builds immutable `LanguagePlan` | Racket modules, Silver extension units, MLIR dialect/op separation | Treat as **authoring taxonomy**, never a nested global planner; test independent package contributions |
| Deriving, nesting, splitting facts and relations | Some results need shared premises, scoped negative evidence and exact provenance | Current experimental `ContractExperiments` handles requires/produces/preserves/invalidates; generic fact service is **proposed** | LLVM analyses, Datalog, attribute grammars, program logics | Start with direct typed analysis result; add bounded derivation only if two independent uses need it |
| Proofs of compiler transformations | A derivation tree over assertions is assumed equivalent to correctness | Verifier-gated SSA subset, limited module-contract checks | Alive2 bounded validation, CompCert formal preservation | Distinguish assertion, checked evidence, and actual refinement proof; add falsifying oracles |
| C-like semantics without undefined behavior | Intends stronger source language safety guarantees | Wist and callable SSA are not a general verified C implementation | Clight/CompCert, Rust language semantics | Specify observations and target contracts first; do not infer speed from UB elimination |
| Dynamic typing / System F | Explores polymorphism and runtime flexibility | Existing types of IR are not automatically source typing semantics | System F, System Fω, gradual typing, type erasure | Keep source type system, IR types and trait evidence separate; prototype polymorphism only for concrete language case |
| Agent-first extension authoring | Wants minimum cross-file exploration and safe autonomous editing | Existing registry/typed diagnostics and non-Wist authoring samples | Language workbenches, Roslyn structured APIs | Read-only plan and contract retrieval, versioned editing and independent holdouts; measure benefits |

## Fifteen independently reconstructed **representation research questions** — not the original numbering

| Provisional ID | Reconstructed research question | Assessment / experiment |
| --- | --- | --- |
| R01 | Is an AST/Bytecode/SSA representation a feature or an artifact? | An artifact with typed kind/version/contract; the pass that provides it can be a selected contribution. Test a new converter contribution without modifying old artifact owners. |
| R02 | Can representation kinds form an inheritance hierarchy? | Only if semantic subtyping is actually established; typed conversion graph is a safer baseline. Compare forced universal superclass vs explicit typed edges. |
| R03 | Can several representations coexist in one compiler? | Yes, typed artifact routes already suggest this; require no implicit interchangeability or global default SSA. |
| R04 | Is a transformed/optimized AST still the source AST? | It is a new immutable program revision; original source anchors may survive as provenance but not identity of semantics. |
| R05 | Who verifies representation invariants? | The format owner declares structural invariants; a consumer/verifier checks them at the boundary. File extension/type tags alone are insufficient. |
| R06 | Can representations be nested or internal to one extension? | Yes, but encapsulation must not leak foreign anchor semantics. An exposed nested artifact requires explicit public contract/version. |
| R07 | Can operation handlers be polymorphic across IRs? | Possible via typed interfaces and explicit admissibility constraints; do not dispatch silently by textual opcode. Compare direct adapters against universal registry. |
| R08 | What makes transformations type-safe? | Well-typed input/output contracts, validation of preconditions and output invariant. Type safety of C# does not prove behavioral preservation. |
| R09 | Can an IR family be added after old modules were written? | Test N+1 with separately built provider and unchanged consumer; explicit adapters allowed but count all glue and registry mutations. |
| R10 | When may semantic facts survive an IR transformation? | Only accepted property-specific preservation or independently recomputed evidence in the new revision. Unknown means do not reuse. |
| R11 | Is source-span or node-ID correspondence a semantic witness? | No: mapping proves association, not observational equivalence. Test one-to-many lowering with checked overflow and effects. |
| R12 | Can unrelated representation families share semantic information? | Sometimes through small common obligations over a shared observation model; otherwise materialize/recompute at boundary. Reject universal coercion. |
| R13 | What if a backend changes exception/FP/overflow behavior? | World/backend/observation changes must invalidate affected queries and suppress rewrite until reverified. |
| R14 | How should route selection differ from local optimizer legality? | `LanguageCompiler` picks frozen global configuration; program-local applicability rejects transformations without changing selected package/provider. |
| R15 | Do all representations need a universal structural or algebraic core? | No demonstrated requirement. Compare specialized adapters, typed common semantics, and an MLIR-like IR only against real divergent-language workloads. |

These answers are **research recommendations** grounded in known prompt themes, not statements of the absent note author's precise intent.

## Explicit traps and open questions

1. The phrase `environment` could refer to a lexical environment, type-checking context, runtime capabilities, mutable store, target environment or proof assumptions. The note's precise intended meaning is unknown.
2. A `Fact` may mean an author assertion, analysis result, observation, refinement proof, cache entry or a logical proposition. Do not unify those by name.
3. `Semantic` may refer to a source-language denotation, an operation descriptor, verified backend behavior, an analysis property or a compiler pass precondition.
4. `Algebra` might be an algebraic structure with laws, an API for operators, an interpreter, or an effect handler; those have different correctness obligations.
5. System F, type erasure and dynamically typed source languages are distinct design commitments; none is required to describe a polymorphic low-level operation.
6. The note's 15 actual questions and their ordering remain **UNRESOLVED until source bytes are available**. Never silently mark this document as the original-note audit.

## Reconciliation procedure when original note arrives

- Pin note SHA-256 and exact file/revision.
- Extract all fifteen numbered representation questions verbatim, keeping spelling and ambiguities.
- For each, map to one Rxx if justified or introduce a new stable question ID. Do not reshape original text to fit existing answers.
- Mark `quoted / interpreted / unresolved` separately; link exact UT code and primary scientific sources.
- Revise statuses in the canonical [RESEARCH_DECISIONS.md](RESEARCH_DECISIONS.md) and relevant [EXPERIMENTS.md](EXPERIMENTS.md), without creating another roadmap.

Related: [ORIGINS.md](ORIGINS.md), [RESEARCH_REVIEW_2026-10-12.md](RESEARCH_REVIEW_2026-10-12.md), [SOURCES.md](SOURCES.md).