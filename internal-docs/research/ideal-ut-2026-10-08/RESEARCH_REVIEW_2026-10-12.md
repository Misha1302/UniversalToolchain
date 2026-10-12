# Ideal UT research review — 2026-10-12

Status: **independent research assessment, not implementation or approved roadmap**.

Baseline: `master@886bf241623e83566585bf2aec5b635fefe1675f`. Read repository metadata, relevant docs, focused `LanguageCompiler.cs`, `LanguagePlan.cs`, `LanguageRuntime.cs` excerpts and PR #370/#371 metadata through GitHub. No UT build or tests rerun.

## Executive result

Preserve `LanguageDefinition -> LanguageCompiler -> immutable LanguagePlan -> LanguageRuntime`, the existing single owner of configuration, ordering, provider selection and route resolution. The unresolved question is **independently supplied semantic evidence for unchanged consumers**, not a new universal planning subsystem.

**Default to typed C# contracts**. Run a narrowly scoped, program-local query/obligation experiment before a Fact Store, Datalog, global algebra or mandatory SSA. In the strongest conventional baseline, a fixed, shared-representation handwritten pipeline may be smaller and equally reusable. PR #370 explicitly reports this negative result: total 105 vs 61 SLOC for UT vs clone-and-own; RQ3 shared-pipeline downstream change 3 LOC vs UT 5 LOC. Those are author-reported, unmerged branch results, not new measurements here.

PR #371 contains an unmerged transitional declarative Bytecode operation/handler prototype; the PR reports 1327 passing tests. It deliberately does not demonstrate global LanguagePlan-selected handler composition or improved performance. These features are **not current master APIs**.

## Three alternatives

| Choice | Advantage | Limitation | Decision |
| --- | --- | --- | --- |
| A: direct typed interfaces / strong shared C# pipeline | simplest, explicit, usually fast | coordination/adapters possible for independent producers | strongest control |
| B: existing UT plus bounded selected-world semantic queries | may enable N+1 composition with stable obligations | schema, trust, scope, maintenance overhead | experiment, not adoption |
| C: general rule/proof/fact infrastructure | potential expressive composition | unverified premises, cost, ontology explosion | defer pending independent transitive needs |

The meaningful dependency unit is a **versioned semantic obligation about an exact program subject**, not an arbitrary `Fact` object. One feature may supply several IR relations; one consumer may need facts from multiple packages.

## Scoped judgment and correctness boundary

Proposed experimental world `w=(plan, selected binary digests, program revision, IR kind/version, phase, program point/path, backend, observation model, assumptions, trust policy)`. `PlanHash` alone does not authenticate selected executable bytes. Query `q` has positive and explicit negative accepted evidence:

- neither: Unknown;
- positive alone: Proven **under named trust policy**;
- negative alone: Disproven;
- both: Contradiction.

Unknown, conflicting, stale, wrong-schema or untrusted evidence must not authorize destructive rewrites. This four-state result describes evidence acceptance, **not a proof that provider implementation matches its claims**. A new trusted contradictory provider may revoke a previously accepted positive result. Open-world all-dependencies inference requires a separately validated complete enumeration; avoid negation-as-failure.

The simplest bounded legality gate for E1 is `SafeIndex(array,index,path,world)`. Independently authored `Shape` and `Range` providers must enable unchanged `BoundsOptimization` only for safely verified cases. On edits, provider swaps, signedness/overflow changes, array aliasing/mutation, backend changes or incomplete evidence, retain bounds checks. A new provider must be selected in a **new frozen plan**; arbitrary plugins cannot mutate the old selected world.

## Representation transport

AST, Wist semantic program, Bytecode, AIR, SSA, CPS and ANF are different artifacts/invariant families, not inheriting subclasses of a universal representation. A source-span/anchor map gives traceability, not semantic preservation. Proposed transport may reuse `p` only if a checked mapping witness, property-specific preservation and matching observation context all hold; otherwise recompute in destination IR or return Unknown. No universal SSA. Wist currently documents optional, verifier-gated AIR→SSA→AIR; treat it as bounded evidence, not an all-language pipeline.

Operations as pure data + typed handlers are useful where they remove behavior-bearing IR payloads; they need not replace every AST object, and neither `object + context ≅ algebra` nor all objects-as-DTO is a universal requirement. Separate carrier/domain, environment, evaluation context, operational semantics, traits, effects and mutable state.

## Agent and product boundary

Queryable contracts, revisioned plan/diagnostics, read-only explanations, explicit selected provider identity, patch preview and negative tests are plausible agent enablers. These projections must reuse planner outputs, not create a second compiler authority. Agent assertions of purity or preservation do not become trusted evidence. Compare agent time, token use, invalid edits and new extension independence against conventional SDK/docs/tests.

Parallel non-Wist language-authoring and tooling experiments may create product value even if general semantics research fails.

## Scientific comparisons (primary-source links)

- Racket language modules/macros: [Languages as Libraries (2011)](https://blog.racket-lang.org/2011/03/languages-as-libraries-pldi-2011.html): language extenders can reuse host static semantics without a universal algebra.
- [Silver/ableC](https://github.com/melt-umn/ableC): independent syntax/static semantics composition with modular checks is prior art, so test a narrower genuine novelty claim.
- [Spoofax/Statix](https://spoofax.dev/references/statix/): scope graphs and declarative constraints are strong alternatives for bind/type analyses.
- [MLIR dialect conversion](https://mlir.llvm.org/docs/DialectConversion/): op legality and rewrite/type conversion already exist; compare against MLIR-style typed interfaces.
- [LLVM New Pass Manager](https://llvm.org/docs/NewPassManager.html): cache invalidation and preserved analyses are prior art; exact selected-world evidence is only a **hypothetical** difference.
- [Alive2](https://github.com/AliveToolkit/alive2): bounded translation validation differs from derivation traces over unverified facts.
- [CompCert, Leroy 2009](https://xavierleroy.org/bibrefs/leroy_bib.html): actual compiler correctness depends on formal source/target semantics and preservation proofs.
- [egg, Willsey et al. POPL 2021](https://popl21.sigplan.org/details/POPL-2021-research-papers/23/egg-Fast-and-Extensible-Equality-Saturation): equality saturation is an optional optimization mechanism, not a universal UT substrate.

These sources establish competitor mechanisms, **not** claimed UT superiority. There was no systematic exhaustive literature review.

## Risks, kill conditions, remaining evidence

E1 must compare equivalent closed-world typed interfaces, shared conventional pipeline and optional evidence seam. Record entire integration footprint, old consumer/provider source edits, provider assembly independence, false-safe rewrites (required zero), conservative fallback, diagnostics, memory, planning/compile/materialization/steady-state time, invalidation and independent held-out oracle results.

Reject the generic semantic service if the ordinary design achieves the same legal transformations with lower total cost, if provider assertions bypass trust, if source/IR changes leave stale truths accepted, or if adding a new provider requires modifying existing implementation assemblies. Never promote an isolated prototype to current master.

The 2026-10-09 registry contains **222 planning candidates across 15 domains plus five supplemental recommendations**; no new implementation status is established here. Compress dependencies, not historical records: identity/oracle → independent N+1 → optional cross-IR → optional specialization; separately language authoring + diagnostics. General inference, persistent provenance and full proof tooling remain speculative.

## Adversarial critique

A handwritten pipeline may win fixed workloads; controlled comparison is mandatory. An evidence DAG is not a formal proof; require independently accepted premises. A mapping is not semantic equivalence; recompute when uncertain. Hot-path queries may be costly; test bound specialization against ordinary method calls. Agent schemas may merely duplicate documentation; measure agent error rate. 222 automatic tasks overwhelm maintainers; activate conditional entries only after their experiments.

**Recommendation:** do not implement a generic Fact Store yet. Deliver the E1 falsification slice and independent conventional baseline, then decide whether any general semantic query layer has earned its complexity.

See [EXPERIMENTS.md](EXPERIMENTS.md), [ROADMAP.md](ROADMAP.md), [RESEARCH_DECISIONS.md](RESEARCH_DECISIONS.md) and [NOTE_RECONSTRUCTION_2026-10-12.md](NOTE_RECONSTRUCTION_2026-10-12.md).