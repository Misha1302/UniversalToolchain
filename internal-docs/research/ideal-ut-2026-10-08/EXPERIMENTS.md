# Falsification program and independent controls

**No integrated UT E1–E8 experiment is newly executed by adding these documents.** Two supplied ZIPs contain earlier isolated experiments (one Python 8-test toy, one separate .NET 10 four-assembly 15-assertion model, and revised Python schema/fact-store spikes). Such results demonstrate wiring only, not UT integration or universal soundness.

## E1 — N+1 semantics, primary go/no-go

Create separately built **RangeAnalysis**, **ShapeAnalysis**, **BoundsOptimization**. Consumer depends only on versioned shared `SafeIndex(a,i,path,revision)` semantics, not concrete provider assemblies. Fixture is *real program* `Read(a,i)`; require `0 ≤ i < len(a)`, stable length/path/alias, signedness and overflow policy.
1. Shape-only ⇒ Unknown ⇒ retain check.
2. Add Range independently ⇒ selected proven-safe check may be removed; old consumer and producer remain untouched.
3. Add independent higher-precision Range provider ⇒ new safe opportunities with zero old-code edits.
4. Swap provider binary SHA, mutate array/write/path, change backend/policy ⇒ affected positive derivations revoked.
5. Explicit negative, conflicting, untrusted, incomplete or unknown evidence ⇒ fail closed.
6. Execute valid and invalid inputs against an independent reference.

Compare **A** direct C# interfaces/handwritten glue, **B** MLIR-style typed op interface, **C** scoped evidence. Same corpus, optimizer, environment, authoring budget. Metrics: old consumer modified LOC (target 0), previous producer edits (0), central registry source edits (0), code/adapter volume, false-safe elimination (0 required), false-negative conservatism, memory, compile time, explanation quality. A variant that is simpler and equally correct defeats C.

## E2 — cross-representation honesty

Typed AST/source anchors ↔ genuinely different AIR or SSA/CFG IDs, including one-to-many lowering and exceptions/side effects. Negative examples: checked→unchecked arithmetic, changed trap/ordering, stale origin map, aliasing changes. An AST mapping is **not** a proof; separate bounded checking is mandatory.

## E3 — legality before cost

Freeze the same configuration `LanguagePlan` for two programs with different required obligations. Demonstrate allowed local transformations differ without rerunning global package/provider selection. Fail if cheapest route bypasses mandatory legality.

## E4–E5 — language product

Independently authored second language family, not a Wist clone: bind, parse, diagnose, compile and edit; compare ordinary SDK, generated descriptors and existing language workbench. LSP preview must call existing planner and project results, not mutate hidden state.

## E6 — no unmeasured "zero cost"

Measure planning, runtime creation, compilation, first call, steady-state latency, allocations, binary/code size and rebuild/invalidations. Compare handwritten/static pipeline and cached `WistEngine.Compile<TDelegate>`; include configuration churn and compilation amortization. Do not mix parse/compile with hot-only numbers.

## E7 — untrusted evidence

False `Pure`, incorrect `Serializable`, missing complete dependency enumeration, contradictory polarity, stale implementation identity, invalid scopes and malicious plugin. Query authorization and trusted verifier boundaries must not rely on self-asserted claims; successful planning is not sandboxing.

## E8 — reproducibility

Pin UT SHA, .NET SDK, test seeds, exact oracle, target runtime/OS, commands, selected configurations, raw logs and counterexample minimization. Compare to holdout cases independently authored. Pass under selected assumptions != mathematical equivalence.

**Go/no-go:** false-safe results, pervasive integration patches to old packages, unverifiable trust, or equal outcomes with simpler C# interfaces kill generalization. A valid negative result is a useful final outcome.
