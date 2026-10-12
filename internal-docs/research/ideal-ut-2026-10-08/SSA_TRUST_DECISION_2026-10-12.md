# SSA trust-boundary research decision (2026-10-12)

**Status:** experimental, not accepted architecture; baseline `master@886bf241623e83566585bf2aec5b635fefe1675f`. See [frozen preregistration](SSA_TRUST_PREREG_2026-10-12.md), [count amendment](SSA_TRUST_PROTOCOL_AMENDMENT_2026-10-12.md) and [docs amendment](SSA_TRUST_DOC_SYNC_2026-10-12.md).

## Finding

`SsaManagedCallableAttribute` allows the author of a managed method to set `IsPure=true` and `TrustLevel=VerifiedPlugin`. `SsaManagedCallables` copies that metadata into a descriptor; `SsaDeadPureInstructionEliminationPass` uses the descriptor for dead-call elimination. The pre/post `SsaOptimizerPipeline` verification is structural. This is confirmed in `master` source, not merely in a research proposal.

The new [NUnit fixture](../../../UniversalToolchain/Tests/Ssa/SsaTrustBoundaryCharacterizationTests.cs) tests four cases: genuinely pure, unannotated effectful, unchecked effectful and effectful method declaring purity. In the latter case, the independent oracle invokes the CLR method and observes `(return=6, counter delta=1)`; real SSA dead-call elimination removes that unused call. For that narrow observation model, metadata-only trust permits a non-preserving IR change. A passing *characterization test* identifies this bad outcome; it does not mean the optimization is safe.

Linux [Actions run 38245989179](https://github.com/Misha1302/UniversalToolchain/actions/runs/38245989179) compiled and executed 504 Core tests, all passed; full CI rejected the unchanged manifest expectation of 500. The amendments document the exact count and current-doc projections. Final CI status belongs to its own newer commit.

**Limits:** this constructed SSA case does not prove that unreviewed third-party packages can reach this path through canonical `LanguagePlan`, nor demonstrate final emitted-backend behavior or independent N+1 extension authoring.

## Decision frontier

| Alternative | Benefit | Limit | Decision |
| --- | --- | --- | --- |
| A. Host-owned typed C# trust/approval interface | explicit authority with few mechanisms | must verify scope, digest and authoring cost | favored baseline |
| B. Existing metadata-driven optimization | implemented and structurally checked | user-asserted trust is not proof of semantics | needs explicit trust boundary |
| C. Scoped semantic evidence service | may enable future N+1, revocation, cross-IR transport | still needs verified premises; extra infrastructure | defer until E1 discriminates |
| D. Declarative handlers (PR #371) | representation decoupling | unrelated to truth of purity | parallel question |

Minimal obligation: `RemoveDeadCall => UnusedResult && StructuralValidity && HostAuthorizedEffects && PreservesObservedEffects`. Two methods may declare identical effects yet behave differently; descriptor inspection alone cannot distinguish them. No claim of a universal mathematical proof is made.

## Gated steps

1. Verify complete final-head CI, counts, links and architecture checks.
2. Trace actual Wist/LanguagePlan binding to SSA and determine who owns semantic trust.
3. Compare a strong direct C# host-policy baseline to the smallest UT authorization fix on equal workloads.
4. Run genuine independent E1/M2 only if multiple external authors need more than typed interfaces; reject FactStore if simpler code wins.
5. Continue non-Wist language-authoring tests as an independent product track.

Relevant alternatives: [MLIR interfaces](https://mlir.llvm.org/docs/Interfaces/), [LLVM analysis invalidation](https://llvm.org/docs/NewPassManager.html). This finding changes experimental priority, **not** the public SDK or roadmap status automatically.
