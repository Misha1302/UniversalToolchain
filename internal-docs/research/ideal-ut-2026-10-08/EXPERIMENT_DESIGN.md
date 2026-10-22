# Experiment design and preregistration — semantic composition

**Status:** executable protocol / design, **not** completed UT/LLVM/MLIR four-way evaluation.
**Reviewed:** 2026-10-23. **UT source revision:** [`de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1`](https://github.com/Misha1302/UniversalToolchain/tree/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1).
**Question IDs:** Q01–Q42 in [CRITICAL_QUESTIONS.md](CRITICAL_QUESTIONS.md); existing [E1–E8](EXPERIMENTS.md) are upstream research inputs, not executed integrated results.
**Non-goal:** new public API or claims of universal compiler soundness.

## Experimental decision being tested

**H1 (conditional, falsifiable):** for independently shipped `Range`/`Shape`/effect providers and two already-authored consumers, a selected-world evidence service can maintain *at least the same observed soundness and semantic applicability* as the strongest direct typed-C# baseline while requiring **materially fewer pairwise integration changes** across supplier-version and program-revision changes, at an acceptable measured time/memory cost.

**H0 / no-go:** direct typed interfaces and explicit adapters, or MLIR/LLVM analysis mechanisms within their actual scope, provide equal soundness, conservative behavior and maintainability with no relevant extra integration cost. In this case a general UT evidence kernel is **not justified**.

No arbitrary threshold is retrofitted after seeing outcomes. Prior to execution, freeze test corpus, mutation operators, observation semantics and the cost tradeoff important to the engineering user.

## Baselines: A/B/C/D and fairness

- **A — conventional C#:** independent `Shared.Contracts`, `ShapeAnalysis`, `RangeAnalysis`, `BoundsOptimization` binaries; a composition root supplies an `ISafeIndexQuery` facade. Support manually written typed adapters **with their LOC and ownership counted**. No global ontology required. This is the default winner on a tie.
- **B — LLVM:** pinned LLVM commit + explicit `AAManager` registration, `AAResults`, existing pass/AA queries and `PreservedAnalyses`; same safety property needs a sensible LLVM IR encoding. Do not claim it matches source-level heterogeneous AST/AIR automatically.
- **C — MLIR:** pinned MLIR commit + external op/type interfaces, `DataFlowSolver`, `AnalysisState` and an unchanged shared consumer transform. Bufferization is the strongest *separate analysis/transform* comparator; SafeIndex IR needs its own dialect/operation interface. Count custom MLIR dialect translation and registration effort.
- **D — UT research:** unchanged production `LanguageCompiler/LanguagePlan/LanguageRuntime` config owner plus optional program-local `SemanticQuery` prototype **only if** A/B/C cannot meet the same verified obligations. No global second planner, reflection magic, mandatory SSA or author-asserted proof.
- Run **the same semantic input families and oracles** where representations allow. Mark non-transferable parts `not applicable`; do not turn missing external support into a zero score.

Compare **A consumer unchanged**, **B producers unchanged**, **C no pairwise glue**, **D new information used by known hook**, **E new fact category**, **F cross-IR transport**, **G trust/invalidation safety**, **H complexity/cost** independently.

## Primary E1: real Read(a,i) bound-check removal

Observation model before implementing transformations:

1. Values on valid indexes; exception/trap or bounds failure on invalid indexes; order and existence of observable effects.
2. Selected language's signedness and overflow model; stability of array length; aliasing and writes; exact control-flow point and dominating preconditions.
3. Program/artifact revision, selected backend, semantic assumption set, provider implementation identity, trust policy and executor/runtime identity.
4. Optimization **allowed** only after an independently checked `SafeIndex(a,i,path,revision)` obligation. Missing or contradictory proof is **not safe**. A cheap route or optimistic provider cannot override legality.
5. Independent reference executes the unoptimized program and compiled candidate over boundary-focused and generated inputs; for safety-sensitive cases, inspect actual emitted IR/instructions to confirm the check was *really removed* when claimed.
6. Check equivalence of value, failure, trap and side-effect behavior; where bounded formal validation is possible, run it as a second oracle. Note LLVM [Alive2](https://github.com/AliveToolkit/alive2) works for its supported LLVM IR subset, not arbitrary UT frontend semantics.

### Mandatory scenario matrix

| Case | Change | Required outcome (design target) | Failure classification |
| --- | --- | --- | --- |
| S1 | Shape only; missing Range | `Unknown`, check retained | Unsound if elided |
| S2 | Independently add trusted Range with complete signedness/path evidence | Check elided only at proven-safe sites; old consumer and Shape untouched | Incomplete integration or missed valid optimization |
| S3 | Independently add higher precision Range | More proven-safe sites without old consumer edits | No precision gain or glue dependence |
| S4 | Out-of-range index, integer wrap, stale branch fact | Check retained and old exception behavior | **Safety-critical false positive** |
| S5 | Mutating alias/call changes array state | Revoke/recompute facts and retain check unless re-proven | **Safety-critical stale proof** |
| S6 | Positive and explicit negative/untrusted provider claims collide | Reject dangerous rewrite; diagnostic/provenance | **Safety-critical conflict bypass** |
| S7 | Swap selected executable provider implementation digest/backend/trust policy at identical data-level plan | Old cached conclusion not reused without revalidation | **Safety-critical false preserve** |
| S8 | AST→AIR/SSA one-to-many lowering or changed exception order | Require checked correspondence and preservation; otherwise Unknown | Unsafe representation transfer |
| S9 | Change registration order/add unrelated package | Deterministic selected plan and unchanged known result where domain semantics unchanged | Noninterference failure |
| S10 | Introduce genuinely new fact category absent from consumer API | Record necessary new hook/transform/registration explicitly; **no automatic uptake expected** | Overclaim if presented as automatic |

### Independent accuracy checks

- Freeze corpus seeds, valid/invalid index distribution, aliasing cases and exact expected observable semantics **before** measuring.
- Per case record normalized original result vs candidate result; compare all observation dimensions, not only return values.
- Keep safe missed optimizations separate from unsafe accepted optimizations.
- Inject fact provider bugs: knowingly wrong bounds, fabricated `Pure`, hidden dependency, contradictory evidence, stale or mismatched claimed digest; a self-asserted binary hash does not verify actual assembly bytes.
- Negative evidence is not inferred from absent evidence; all-universal dependency claims require validated completeness.

## Candidate alternative tests

**Alias refinement** (Q02; best LLVM comparator): unchanged LICM/GVN-style pass, two AA registrations, compare alias traces and before/after IR; deliberately reorder AA registration as order is documented significant. Query `ModRefInfo` under effectful calls.

**Effect-based code motion** (Q14/Q21; best semantics/exceptions illustration): move a computation only when independent memory effects and trap-order conditions are established; add a provider with more precise effects. Easier cross-IR than array length, but may conflate exception ordering with safety.

**Constant-propagation-derived reduction** (Q03; best dataflow comparator): run shared lattice/different provider precision, note `DataFlowSolver` already solves generic fixed-point and dependence tracking, so proof of "needs a new engine" becomes less plausible.

**Decision:** use real bounds elimination for safety and high discriminatory power; use alias refinement as the primary LLVM prior-art negative control. If source-level bounds IR semantics are not independent-oracle-ready, start with effect-code-motion for earlier incremental evidence rather than faking a completed E1.

## Equal-budget measurement plan

Record by task and phase (at fixed compiler/SDK versions):

- Consumer changed LOC and binary digest, previous producer changed LOC/digest, *every* pairwise/central adapter changed LOC, registries/config edits, extra package dependencies, compatibility breaks.
- Correct safe elimination count, false-safe count (**must be zero on frozen inputs**), conservatism (known safe sites not elided), invalidation misses, conflict handling and actionable diagnostics.
- Planning cost, provider materialization, fact-query latency, recheck latency under edits, cold compilation, cached compilation, first call, hot call, peak RSS, allocations, build size, startup and rebuild cost; do not mix phase scopes.
- Reproducibility: fixed seed and worker isolation; normalize errors, preserve raw traces, eliminate infrastructure failures separately; include two independently authored providers and one independently authored consumer as external holdouts.
- Report distributions, effect sizes and uncertainty intervals for measurements, not single cherry-picked timings. Evaluate memory/time at scales of 1, 3, 5 and 10 independent extensions.

```text
schema: stage2-semantic-composition-evidence/v1
caseId, scenario, variant(A|B|C|D), toolCommit, utCommit, sdk, osArch,
programSha256, providerBinarySha256, selectedPlanHash, backend, irRevision,
observationModel, trustPolicy, consumerSourceSha256, oldProducerSourceSha256,
changedConsumerLOC, changedProducerLOC, adapterLOC, registrationEdits,
decision{allow|deny|unknown|conflict}, oracle{pass|unsafe|inconclusive|not-applicable},
rawOutcome, emittedIROutcome, diagnostic, timingNs, allocationsBytes, seed,
artifactManifestSha256
```

Explicitly record `not-applicable` rather than inventing measurement coverage.

## Executed small C# control (NOT a UT integration experiment)

**Status:** actually run in the research task environment on 2026-10-23 with .NET SDK `10.0.301` (Linux x64).
**Versioned runnable source:** [controls/static-csharp-baseline/](controls/static-csharp-baseline/README.md) (five .NET projects, 10 decision assertions, exact `SOURCE_SHA256.txt`). The original research conversation also contains `ut_stage2_csharp_baseline_2026-10-23.tar.gz`; archive SHA-256:
`c795bab0e2ff7d0995bc530fc09a0d453381b331e7d68e2de7ac8e9074572021`. Its `SOURCE_SHA256.txt` lists exact source file hashes; captured `run.log` SHA-256 is `75ef05c2daa05e69028363a91aeee4484738d6be81af6ab333cd32e386c86137`. **Source is now preserved in this research repository; the project CI does not run this isolated control automatically.** Reproduce from `controls/static-csharp-baseline/` or the independently attached archive. Run `sha256sum -c SOURCE_SHA256.txt` on the checked-in copy to validate exact source bytes.

Structure: `Shared` typed `Query/Observation/IFactProvider/ISafeIndexQuery`; `Shape` and `Range` provider projects; consumer project `BoundsOptimization` calls the predeclared `ISafeIndexQuery`; `Host` wires them via `SimpleSafetyQuery`. The reference is intentionally hand-authored conventional C#, not an UT extension. Command from extracted archive root:

```sh
dotnet --version # expected: 10.0.301 for exact historical replay
dotnet run --project Host/Host.csproj -c Release --ignore-failed-sources
```

The observed output was exactly:

```text
shape-only-retains = False
independent-range-enables = True
higher-precision-range-substitution = True
invalid-index-retains = False
negative-index-retains = False
conflict-retains = False
mutation-retains = False
aliasing-retains = False
stale-revision-retains = False
changed-implementation-retains = False
PASS: 10 decision assertions; no UT runtime, LLVM, MLIR or machine-code optimizer invoked.
```

**Precisely what it establishes:** five independently compiled .NET projects can coordinate a pre-agreed query without changing the source code of a consumer or earlier provider; conservative decision logic can be expressed with plain typed interfaces and host-level registration.

**Precisely what it does NOT establish:** the compiled sample never rewrites actual `Read(a,i)` IR nor executes unchecked machine access. The value `ImplementationHash` is provided in the query/observations and is **not cryptographically bound to loaded binaries**; therefore the wrong-hash test only verifies comparison of a claimed string, not secure digest verification. `LengthStable` and `AliasingSafe` are supplied booleans, not independently proven facts; a colluding false-positive provider may authorize an unsafe decision. This toy cannot be counted as E1, same-budget comparison, hostile-plugin test, or semantic soundness proof.

The small control is useful because it weakens the necessity claim for a new core; it must **not** be used as quantitative evidence of superiority. A full E1 remains blocked on real UT integration and independently established semantics.

## Formal no-go and promotion rules

Immediate **fail/stop** for an unsafe accepted rewrite, incomplete/revoked fact accepted as true, unverifiable trust underpinning a safety-sensitive law, or a hidden second global planner. Do not average unsafe behavior into a numerical score.

Reject **generalization** when A can achieve equal soundness, reproducible integration and acceptable cost more simply; when C already provides the necessary semantics within domain constraints; when measured invalidation complexity defeats full recomputation; or when a second independent consumer does not exist.

Promote only in ascending reversible order: actual problem fixture → conventional typed C# baseline → IR-specific alternative → optional shared query contract → checked provenance/revalidation only where independently needed → finite derivation rules only with a proven transitive-use case. Public API and claims require separate, reviewed CI and third-party holdout evidence.

## Execution dependencies and reproducibility checklist

1. Pin all toolchain SHAs, SDK and environment, plus selected code owners.
2. Freeze source programs and observation rules; prepare an independent oracle before measuring.
3. Ensure at least one untouched old consumer binary and untouched old provider binary are retained across each addition.
4. Run test matrix A/B/C/D for applicable semantics; mark unsupported or unavailable tools as `not run`.
5. Confirm actual eliminated instruction separately from `CanElideCheck` flags; verify source→IR and IR→execution differences.
6. Hash and archive raw inputs/results, test status and counterexamples; separate clean, inconclusive and infrastructure categories.
7. Only then answer Q07/Q12/Q15/Q22 with stronger than `PARTIAL/UNTESTED` statuses.

**At review close:** A small C# control is the sole newly executed experiment. UT, LLVM and MLIR integrated counterparts are **not run**. No timing, defect-rate, or engineering-cost advantage has been measured.
