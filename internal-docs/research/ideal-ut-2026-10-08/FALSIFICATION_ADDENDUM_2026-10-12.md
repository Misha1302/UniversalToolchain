# Falsification addendum — experiments, controls and conditional roadmap (2026-10-12)

**Research protocol, NOT experiment execution.** The owner experiment descriptions are [E1–E8](EXPERIMENTS.md), [M0–M6](ROADMAP.md) and [S0–S8](ENGINEERING_BACKLOG.md). This addendum sharpens their *independent controls, stop conditions and measurements*; it does **not** rename or mark them complete. Prerequisite: work off a fresh checked-out commit and record environment/test receipts.

## Evaluation contract common to every experiment

- **Baseline**: correct ordinary typed C# implementation with documented invariants, stable shared abstractions and allowed incremental helpers—not a deliberately bad switch statement. Where representation-centric, include MLIR-style operation interfaces/conversion as a separate control.
- **Treatment**: smallest additive UT candidate on the *same functional requirement*, using one selected LanguagePlan. Keep consumer and existing providers built as separate assemblies and commit-pinned before novel provider is disclosed.
- **Independent oracle**: reference interpreter or operational semantics, externally authored expectation fixtures, or verified bounded equivalence checker. Two UT backends using the same lowering or verifier do **not** form an independent correctness oracle.
- **Case classes**: positive, explicit negative, Unknown/no provider, contradictory trusted evidence, untrusted provider, stale revision/provider digest, wrong schema, changed effects/path, incomplete dependent set, malformed artifact, held-out third-party operation and resource exhaustion. Generate some mutants *after* implementation freeze.
- **Metrics**: false-safe decisions (must be **zero** on evaluated corpus), false negatives, unchanged assembly checksums, old-source edits, central registry edits, adapter/glue SLOC, entire treatment footprint, build/toolchain complexity, diagnostics, cold compile and warm/steady call, allocations, cache invalidation and agents' total context tokens/human validation effort. Report confidence intervals or raw distributions if comparative performance samples warrant them; no invented weighted 0–5 winner.
- **Failure doctrine**: any unsafe rewrite stops that treatment; a simpler equally safe baseline with lower *total* cost defeats the new abstraction. A successful demo of one example does not establish open-world safety. Completion requires machine-readable logs, exact commands, runtime/OS, binary digests, seeds, and archived counterexamples.

## E1–E8: experiment matrix

| ID | Hypothesis / target | Concrete design: baseline → treatment, independent oracle and holdout | Accept; falsify |
| --- | --- | --- | --- |
| **E1** | Independent providers improve unchanged consumer (N+1) | Frozen `BoundsOptimization` consumer over actual `Read(a,i)`; Shape/Range and later unseen RangeV2 separate packages. A: direct typed interfaces; B: UT semantic query; C: MLIR-style op interface. Oracle: language interpreter with bounds/trap observation and out-of-bounds negatives. Holdout: range with aliasing, overflow, path sensitivity, different provider build. | Zero old-consumer and old-provider source edits, no unverified check removals; at least one new legitimate opportunity without unseen glue. **Falsify B** if A achieves same safety and independence with less total infrastructure. |
| **E2** | Facts can move across IR only under justified correspondence | AST→AIR and optionally SSA, one-to-many lowering, changed evaluation order and scope. Baseline: reanalyse each target IR; treatment: transported fact with typed witness; oracle: trace/value/trap/heap comparison. Holdout: volatile/mutable array + changed exception path. | Changed semantics/stale witnesses invalidate, never reuse unsafe facts. **Falsify witness framework** if reanalysis is equally correct and simpler/faster. |
| **E3** | Per-program legality is distinct from language configuration planning | Two different programs under one `LanguagePlan`, same optimizer, different range/alias premises. Oracle: source interpreter and lowered output. | Different legal optimizations; no second package/provider resolver; Unknown declines; reject if global route cost bypasses legality. |
| **E4** | Independent *non-Wist* author can construct meaningful DSL | Implement policy/pricing DSL in a clean consumer, explicit host symbols, source spans and diagnostics; baseline handcrafted C# interpreter and/or mature DSL tool; treatment UT. Oracle fixed rules and invalid input set. Holdout independent author. | Correct syntax/binding/execution and deterministic errors with bounded authoring steps. Simpler control wins if UT generic plumbing adds unjustified burden. |
| **E5** | Same contracts can support interactive authoring without a second semantic owner | LSP open/change/hover/completion/diagnostics for valid→invalid→valid edits; baseline ordinary language server; treatment read-only revisioned UT projection. Oracle batch binder + stale-response checker. Holdout rapid edits/cancel. | No stale results, missing symbols, or hidden planner state; reject if duplicated parse/bind logic or excessive cold latency. |
| **E6** | Optional semantic service pays its compilation/runtime cost | Same programs on pinned hardware/.NET: simple C#, current UT, evidence UT. Measure planning, resolution, materialization, compilation, memory/allocations, first/warm/steady call and invalidation frequency. Alternate hot/cold workload distributions and confidence intervals. | No safety regression, justifiable amortization for target workloads. Kill or feature-gate treatment if overhead dominates realistic usage without independent authoring benefits. |
| **E7** | Untrusted/missing/stale evidence cannot authorize optimization | Malicious provider lies `Pure`/`Range`; contradictory negative and positive; wrong implementation digest, incomplete dependencies and schema spoofing. Independent oracle includes observable side effects/traps; mutation harness run by different author. | **Zero false-safe** on corpus and comprehensible failure. One unauthorized removal is stop/rollback, not a performance tradeoff. |
| **E8** | Experiment results reproduce from clean source | Independent fresh checkout at pinned SHA, SDK and deps, fixed artifacts and raw log hashes. Compare replayed oracle results and measurement distribution; include deterministic seed and holdout author. | Reproducible classifications and bound claims. Stop claims about external validity without a genuinely independent author/corpus. |

## M0–M6 / S0–S8 dependency reconciliation

| Gate | Existing experiment slices | Completion evidence | Reason not to advance |
| --- | --- | --- | --- |
| M0 / S0 | freeze source identity and one-plan runtime; observe exact selected binary provenance | baseline build/checks + immutable plan/runtime/source digests; note missing exact executable identity | can't reproduce or exact source mismatch |
| M1 / S0–S1 | establish independent oracle and negative mutants before design | oracle catches at least two distinct semantic faults absent from implementation tests | oracle agrees only with same UT assumption |
| M2 / S1–S2 | actual selected-world N+1 check removal | separately built providers, real emitted check change, independent oracle, A/B/C cost comparison | false-safe; old-assembly edits; ordinary interfaces win |
| M3 / S3 | cross-artifact mapping, scope/invalidation | 1:N and invalid effects mutants plus digests; reanalysis control | witness has no actual verification or worse cost |
| M4 / S4–S5 | independent language author and tooling | reproducible non-Wist clean-slate user journey + token/correctness experiment | overfitting, hidden duplicate planner |
| M5 / S6–S7 | specialize selected legality-checked route | pinned costs of compile, materialize, warm-call, memory and switch threshold | runtime improvement depends on hiding compile costs |
| M6 / S7–S8 | optional bounded Horn/rules only after demand | at least two independent transitive problems *not solved as well by direct interfaces*, bounded fixpoint and explainable invalidation | no incremental expressive/engineering value |

Critical path remains **M0→M1→M2→M3→M5**, with M4 parallel and M6 contingent. Avoid scheduling dates absent real staffing/cost evidence.

## Mechanism-specific adversarial cases

### Four-valued knowledge must stay epistemically separate

For a proposition `p`, trusted admissible positive/negative evidence sets `E+, E-` yield: `∅/∅→Unknown`; `+/∅→Proven`; `∅/-→Disproven`; `+/-→Contradiction`. This is a *policy-relative query outcome*, not a universal proof of p. Unknown and Contradiction both forbid a correctness-sensitive action, but contradiction should additionally surface diagnostic provenance. Absence of `Dependency(x,y)` cannot prove `¬Dependency(x,y)` without a certified complete dependency universe.

### A semantic witness is stronger than source mapping

An anchor map `a ↦ {b1,b2}` only locates successors. Fact transport also needs a checked relation over observations, e.g. `⟦lower(a)⟧target ⊑ ⟦a⟧source` under named memory/effect/overflow/trap assumptions. A mapping constructed by the same lowering pass with no independent check is an *asserted claim*; verify or reanalyse. Partial refinements and multiple source outcomes require an explicit relation; do not equate raw equality of output numbers with preserved semantics.

### Cost model and fair rejection condition

`TotalCost = implementation+common interfaces+adapters+versions+docs+tests+rebuild/invalidation+runtime/compile cost`. Compare deltas from a fixed feature baseline **and** totals, since PR #370 illustrates why a smaller marginal edit does not imply smaller complete system. Prioritize independently held-out future changes over contrived repetitive code.

## Independent adversarial review: actual design defects and repairs

| Critic | Defect found in tempting design | Repair in this plan |
| --- | --- | --- |
| Compiler engineer | two planners, compulsory FactStore even for fixed DSL | retain only LanguageCompiler; A as permanent control |
| PL theorist | `Proven` with unspecified semantics is vacuous | exact observations, scope, policy and typed proposition |
| MLIR architect | generic interfaces presented as UT invention | compare mature MLIR interface/conversion explicitly |
| Extension author | “zero glue” ignores schema and packaging work | count all code and adapters, independently authored holdouts |
| Performance engineer | “zero cost” based on steady hot call | compile/materialize/plan/memory/invalidations and amortization |
| Formal methods reviewer | source-authored premise mistaken for theorem | independent verifier or narrowed trusted pass boundary |
| AI engineer | contract documentation optimized for demo prompts | blinded tasks, same token budget and failure oracle |
| Maintainer | hundreds of parallel statuses become stale | one existing dossier + stable owner links, no second backlog |

## How to run the next bounded study

1. Pin a new verified local checkout and dependencies; characterize E1 in actual emitted IL/AIR path (not a fresh toy).
2. Freeze a public predicate/operation boundary and A/B baseline before revealing RangeV2 holdout.
3. Run same case corpus, adverse mutations, code-footprint measurement and independent oracle.
4. Only if B wins, add E2 exact snapshot + one-to-many correspondence; otherwise record rejection without code promotion.
5. Update [IDEAS](IDEAS.md), [EXPERIMENTS](EXPERIMENTS.md) and [ROADMAP](ROADMAP.md) only with commit-tied results, preserving this protocol as a dated review.

**No new experimental success is claimed by this documentation change.**
