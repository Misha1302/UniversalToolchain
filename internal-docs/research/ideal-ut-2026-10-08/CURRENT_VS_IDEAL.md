# Current implementation and IUT-01–13 gap inventory

**Revision-bound**: source/docs audited at `master@1d46f17c8dc28f434fa58bdf92f9f8278fa5aaee` (2026-09-20). Recheck after any newer revision.

## Present at that revision

Generic Language.Abstractions/FeatureSdk/LanguageSdk/LanguageAuthoring/Runtime surfaces; packages and typed contributions; one `LanguageCompiler` selecting dependencies, capability providers, ordered contributions, mandatory-pass feasibility, artifact routes, backend/runtime provider and an immutable `LanguagePlan`. `LanguageRuntime` checks/materializes exact choices. Wist provides syntax, semantic binding, Bytecode, AIR, interpreter/CIL, and optional verified SSA; Acme.PricingLanguage independently exercises the generic SDK. PlanFuzz and `ContractExperiments` are research/testing, not general published proof APIs.

Generic high-level grammar, binder, LSP, all-language semantic transport, hostile plugin sandboxing, mathematical optimizer correctness and NuGet publication of source candidates are **not thereby established**.

| ID | Ideal requirement | Status vs full requirement | Missing discriminator |
| --- | --- | --- | --- |
| IUT-01 | Open-world extensibility | PARTIAL | independently composable *semantics* |
| IUT-02 | Global configuration + local feasibility | PARTIAL | revision-specific legality without a second config planner |
| IUT-03 | Semantic interop | EXPERIMENTAL | unchanged consumer uses new independent provider |
| IUT-04 | Evidence-bearing judgments | EXPERIMENTAL | tested typed trust/provenance/negative evidence |
| IUT-05 | Representation neutrality | PARTIAL | validated actual AST↔AIR fact transport |
| IUT-06 | Evidence lifecycle | EXPERIMENTAL | selected-world invalidation and dependent revocation |
| IUT-07 | Correctness-first planning | PARTIAL | program facts before optimization cost |
| IUT-08 | No pairwise glue | UNKNOWN | N+1 controlled multi-package test |
| IUT-09 | Specialization | PARTIAL | full bound compile/steady-state comparison |
| IUT-10 | Minimal semantics vocabulary | EXPERIMENTAL | smallest adequate ontology across 2 languages |
| IUT-11 | Trust/safety | PARTIAL | adversarial providers + independent oracle |
| IUT-12 | Coherent cross-layer contracts | EXPERIMENTAL | source→analysis→transform→backend measured slice |
| IUT-13 | Stable ownership | PARTIAL | no duplicate selection or persistent unsound state |

The existence of similarly named metadata, experimental code, or a theoretical fact proposition does **not** establish full IUT-03/04/06/08/12.

**Strongest baseline:** for one closed fixed DSL and one well-defined IR, handwritten pipeline + typed C# interfaces (or MLIR interfaces in an MLIR-centric design) likely needs fewer concepts. Proposed evidence must win on open-world correctness/decoupling, not generality alone.

Grounding: [current architecture](../../../docs/CURRENT_ARCHITECTURE_STATUS.md), [limitations](../../../docs/limitations.md), [future-work triggers](../../../docs/architecture/future-work.md), [adversarial architecture boundaries](../../../docs/architecture/langdev-adversarial-boundaries.md), [experimental semantic note](../../../docs/research/semantic-fact-proof-layer-2026-09-20.md).
