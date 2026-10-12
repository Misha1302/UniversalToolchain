# Pre-registration: SSA trust-claim versus effect preservation (2026-10-12)

**STATUS: FROZEN BEFORE TEST-CASE MUTATION.** Research choice: investigate the actual UT SSA optimization authority boundary **before** allocating architecture budget to generalized semantic evidence. This is an E7/S0-style characterization/negative-control slice, **not** completion of E1/E7, product vulnerability certification, proof of arbitrary plugin execution, or an approved architectural decision.

## Frozen evidence identity

- Current default `master` snapshot: `886bf241623e83566585bf2aec5b635fefe1675f`, validated via GitHub on 2026-10-12.
- Source of new research documentation: existing review PR #376 head `77ed7d496bbf98e3b7a8b2f40266f66df7ca225f`; experiment branch starts from that commit. PRs #370/#371/#376 are open/unmerged at freeze.
- Relevant production owners, inspected at `master`: `UniversalToolchain.Ssa.Abstractions/SsaManagedCallableAttribute.cs`, `SsaManagedCallables.cs`, `UniversalToolchain.Ssa.Optimization/SsaDeadPureInstructionEliminationPass.cs`, `SsaOptimizerPipeline.cs` and `UniversalToolchain.Semantics.Abstractions/SemanticTraits.cs`.
- Existing NUnit test analogues: `UniversalToolchain/Tests/Ssa/SsaOptimizationTests.cs` and `SsaSafeOptimizationPassTests.cs`. .NET project: `UniversalToolchain/Tests/Tests.csproj`, `net10.0`.
- Local Linux container lacks network access to github.com and had no dotnet executable at freeze (an SDK archive is available, but no verified full UT checkout); **execution is expected through GitHub CI on exact experiment commit**. An actual build outcome is **not** asserted here.

## Decision choice: independent alternatives

**H-A (strong ordinary C# baseline):** explicit host-owned trust allowlist / typed predicate `ITrustPolicy.IsApproved(MethodBase, digest, rule, scope)` before enabling a behavior-changing optimization; no shared fact store. Even if an extension attributes itself `VerifiedPlugin`, unapproved code retains the call. This is a valid solution to the *tested authorization question*, without claiming N+1.

**H-B (existing UT behavior):** declared `IsPure=true` and `TrustLevel=VerifiedPlugin` pass the optimizer's existing `HasObservableEffects` gate; pipeline structural verifier accepts the post-transform SSA. If the underlying method has observable side effects, this permits an **unsound** elimination within the tested IR boundary. It is not automatically a security exploit: host policy may be responsible for allowing only trusted declarations.

**H-C (scoped semantic evidence):** exact executable identity, scope and independently checked evidence might authorize transformations without relying on self-asserted `VerifiedPlugin`. This cannot be shown necessary from a single forged declaration. **Only design**, not an implemented third experiment; the H-A alternative must remain admissible.

**Differentiating result:** if H-B can eliminate a real effectful call while H-A conservatively retains the same underlying call under missing host authorization, authorization is the next research gate. If existing code blocks H-B at any essential stage, update the finding and do not add needless trust infrastructure. If H-B only works in manually constructed SSA, scope remains that SSA scenario (not automatic Wist frontend reachability).

## Test oracle and observation model

**Oracle independent of optimization implementation:** invoke the actual static managed method with input `5`, record `(return value, counter delta)`; the counter is an externally visible managed-static effect. Build SSA call referencing exactly that `MethodInfo` through `SsaManagedCallables.TryCreateMethod` and execution-scoped `SsaManagedCallableBinding`. In this fixture, the call result is dead but the original method's effect is not. The SSA function returns an unrelated constant; the transformation may remove the call **only if no externally observable effect is lost**. An IR diff omitting an effectful call makes observational equivalence impossible for that explicit effect model, even without executing a backend.

**Positive controls:** a genuinely pure annotated method with unused result is removed under approved trust; an effectful unannotated method and an effectful method with `UserProvidedUnchecked` remain. Same SSA validator, same lowering/shape for all cases.

**Negative/adversarial case:** effectful method falsely annotated `IsPure=true`, `Determinism.Deterministic`, `TrustLevel.VerifiedPlugin`. Compare `SsaDeadPureInstructionEliminationPass` outcome and `SsaOptimizerPipeline` structural acceptance against the independent method oracle. Capture the conflicting pair: `actual effect delta=1` versus `retained call=0`.

**Host-policy C# baseline:** fixed allowlist based on MethodInfo or executable identity refuses the unapproved method **even if its attribute says VerifiedPlugin**. For this scoped local experiment, implement a narrow method-level approval predicate/branch selecting no DCE for unapproved method; record that doing so need not create any global fact framework. Do not claim that this simple predicate handles all combinations of independent providers, proofs, or cross-IR reuse.

**Holdout strength:** cases are designed by this same research agent; no independent human/third-party blinded author. They are adversarial *in intent*, not independent external validation. Formal equivalence outside this simple modeled effect is not claimed.

## Frozen success/falsification gates

1. Compiler code, existing test classes and all older provider/consumer binaries are **not modified**. Mutation allowlist: a new isolated NUnit file under `UniversalToolchain/Tests/Ssa/`; new research files and README pointer within existing dossier.
2. At least four cases appear as test methods: truthful trusted pure, untrusted, unannotated and dishonest self-asserted trusted; a direct-policy conservative baseline must be compared.
3. Actual `SsaManagedCallables`, `SsaDeadPureInstructionEliminationPass` and `SsaOptimizerPipeline` are called. Do not reproduce their algorithms in a toy simulator.
4. Wrong structural acceptance and wrong semantic preservation are tracked separately. A valid SSA artifact is **not** a certificate of unchanged counter effects.
5. A test showing intentional unsound transformation may **PASS as a characterization**, yet the science records a `FAIL_SAFE` for the unconditional self-asserted trust model, not “passed means safe”.
6. No production fix or new general ontology. The expected decision is to prefer H-A until a scoped H-C architecture demonstrates incremental value beyond explicit host-verified policy.
7. Test run: `dotnet test UniversalToolchain/Tests/Tests.csproj -c Release --filter FullyQualifiedName~SsaTrustBoundaryCharacterizationTests`. Existing .NET CI / Architecture Boundaries / Docs Check as available.
8. If fixture fails to compile or cannot reach the intended code path, repair as new versioned protocol appendix and **retain this frozen pre-registration unmodified**; avoid silently moving the goalposts.
9. Additional cases (wrong digest, alias, backend, data races, program revision, ambiguity) are *not* evaluated by this local trust slice. They remain E1/E2/E7 open research questions. No benchmark is conducted; performance/authoring economics stay `UNKNOWN`.

## What would make the simpler architecture preferable?

If the narrow issue is only “self-declared trust must not authorize an optimization”, then a bounded host approval/purity contract solves it in a small direct C# predicate; the full FactStore is not justified. Evidence-query architecture is only reconsidered when an independent N+1 or cross-IR case cannot be solved with this simple policy at comparable whole-system cost.

## Historical and citation boundaries

Related [REVIEW_2026-10-12.md](REVIEW_2026-10-12.md), [FALSIFICATION_ADDENDUM](FALSIFICATION_ADDENDUM_2026-10-12.md), [CGO27 claim ledger](../../../CGO27/CLAIM_EVIDENCE_LEDGER.md) and [current architecture](../../../docs/CURRENT_ARCHITECTURE_STATUS.md). LLVM New Pass Manager documents analysis invalidation; MLIR Interfaces provide a mature interface-driven extension model. Neither solves trust of a forged managed method attribute automatically.
