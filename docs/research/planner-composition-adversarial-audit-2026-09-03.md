# UniversalToolchain planner/composition adversarial audit

Date: 2026-09-03

Baseline branch: `master`
Baseline commit: `7005371d6c30175dff4b0e9f906a26218b0ee54d`
Audit branch: `audit/planner-countertests-20260903`

Scope: planning/composition/runtime architecture only. This document intentionally does **not** propose or implement production fixes. It is the Phase A inventory that must exist before countertests are added.

## Baseline

The default branch is `master`; the audited commit is `7005371d6c30175dff4b0e9f906a26218b0ee54d` (`docs: add LangDev adversarial defense pack`). The relevant ownership map is:

- `UniversalToolchain.Language.Abstractions`: IDs, definitions, artifact contracts, runtime policy;
- `UniversalToolchain.FeatureSdk`: features, contributions, transformation metadata, package registry;
- `UniversalToolchain.LanguageSdk`: feature/contribution resolution, planner, routes, `LanguagePlan`, canonicalization, verifier;
- `UniversalToolchain.Runtime`: runtime materialization, transformer/executor lookup, execution;
- `UniversalToolchain.LanguageAuthoring`: typed authoring facade;
- `UniversalToolchain.LanguageSdk.Tests`: principal planning/runtime regression tests.

GitHub Actions baseline for the exact commit is not green. `.NET CI` run `33340972913` failed on both Linux and Windows. On Linux the canonical build/test step completed, then `Enforce canonical entrypoint result` failed; on Windows the canonical PowerShell build/test entrypoint failed. The uploaded canonical build log artifact expired on 2026-09-02, so the exact pre-existing inner failure cannot be reconstructed from the retained artifact. These failures must be kept separate from new expected-red countertests.

## End-to-end decision path

The audited lifecycle is:

`LanguageDefinition` -> selected features -> contribution dependency/capability closure -> slot/conflict/requirement policies -> definition order -> runtime-provider selection -> backend owner -> artifact conversion route -> pass insertion/order -> `LanguageArtifactRoute` / `LanguagePlan` -> PlanHash -> `LanguagePlanVerifier` -> runtime component materialization -> exact transformer/executor binding -> execution.

Important phase boundary: `LanguageCompiler.Compile` resolves features/contributions/runtime provider first and then invokes `LanguageArtifactRoutePhase.Build`; route planning does not feed constraints back into contribution/provider selection. `LanguageArtifactRoutePhase.Build` itself first computes a conversion-only route with `FindBestRoute`, then inserts all selected same-contract passes. Runtime materialization is later and is explicitly documented as not changing semantic selection.

## Findings

| ID | Class | Invariant / expected property | Minimal witness | Current behavior | Severity | Confidence | Countertest |
|---|---|---|---|---|---|---|---|
| UT-PLAN-001 | ARCHITECTURAL_UNDERSPECIFICATION | Route semantics should not depend on one scalar unless all competing preferences are commensurable in that domain. | Two legal conversion routes with the same scalar cost but different behavior. | `ArtifactTransformationDescriptor` exposes only non-negative `int Cost`; `FindBestRoute` treats it as the primary preference signal. No hard/soft, lexicographic, semantic-equivalence, required/forbidden-route, or policy dimension exists. | HIGH | HIGH | `Planner_ShouldNotResolveSemanticRouteAmbiguityByContributionIdAlone` |
| UT-PLAN-002 | INCONSISTENT_POLICY | If alternatives are semantically distinguishable and no preference is declared, deterministic enumeration must not silently become semantic policy. | `Source -> A -> T` and `Source -> B -> T`, equal cost. | Equal route cost is resolved by a concatenated lexical `ContributionId` signature. Capability ambiguity (`UTL2002`), runtime-provider ambiguity (`UTL2302`) and single-owner ambiguity (`UTL2101`) instead fail closed. | HIGH | HIGH | same equal-cost ambiguity witness |
| UT-PLAN-003 | ARCHITECTURAL_UNDERSPECIFICATION | Proposed safety invariant: a selected mandatory pass should participate in route feasibility; a locally cheapest route should not cause failure if another route yields a complete valid plan. | Cheap `Source -> T`; slightly dearer `Source -> M -> T`; selected pass `M -> M`. | `FindBestRoute` chooses conversion-only path first. `InsertPasses` then reports `UTL2204` because `M` is absent; planner does not retry the alternative route. Docs currently describe this behavior, so this is a model/global-planning gap rather than a violation of the documented algorithm. | HIGH | HIGH | `Planner_ShouldNotRejectGloballyValidRoute_WhenSelectedPassRequiresAlternativePath` |
| UT-PLAN-004 | INCONSISTENT_POLICY | If `LanguageArtifactRoute.TotalCost` is the route cost exposed by the final plan, its relation to the optimized cost domain must be explicit and non-contradictory. | Route A has cheaper conversions plus non-zero same-contract pass cost; Route B has dearer conversions but lower final total. | Search minimizes conversion costs before passes; `LanguageArtifactRoute.TotalCost` sums all final steps. Low-level descriptors allow non-zero pass cost while typed `AddPass` fixes pass cost to zero. Thus “minimum-cost route” and published `TotalCost` are not generally the same objective. | MEDIUM | HIGH | low-level descriptor witness; keep as design witness if assertion policy remains unspecified |
| UT-PLAN-005 | CONFIRMED_BUG | Every cost accepted by public descriptor construction must be handled without arithmetic corruption or an uncaught planner exception. | Direct route cost 100; alternate two-edge route with `int.MaxValue + int.MaxValue`. | `FindBestRoute` adds `int` costs directly. Overflow can wrap and make the expensive route appear cheaper. Construction of `LanguageArtifactRoute` later uses `Enumerable.Sum(int)`, which can throw `OverflowException`; `LanguageCompiler.Compile` does not convert this into a diagnostic. | CRITICAL | HIGH | `Planner_ShouldNotPreferOverflowedRoute_WhenCostsExceedIntRange` |
| UT-PLAN-006 | MISSING_VALIDATION | Explicit `Before`/`After` constraints that are impossible on the chosen route must be rejected during planning as diagnostics, not ignored until verifier construction throws. | Pass on earlier contract A declares `After` a selected pass on later contract B. | `AppendPassesForContract` only considers ordering references inside the current contract's candidate set, so the cross-contract edge is ignored during insertion. `LanguagePlanVerifier` later sees both steps and rejects the global order; the exception escapes `Compile`. | HIGH | HIGH | `Planner_ShouldReportImpossibleCrossContractPassOrder_AsPlanningFailure` |
| UT-PLAN-007 | ARCHITECTURAL_UNDERSPECIFICATION | Semantically non-commutative same-contract passes need an explicit ordering relation or an explicit declaration that ties are interchangeable. | Two same-contract passes, equal `Order`, no `Before`/`After`, non-commutative transforms. | Ready passes are ordered by `Order`, then lexical `ContributionId`. Determinism is achieved, but ID is also an undeclared execution priority. | MEDIUM | HIGH | `Planner_ShouldNotUseContributionIdAsSemanticOrderForEqualOrderPasses` (proposed safety invariant) |
| UT-PLAN-008 | TEST_COVERAGE_GAP | Registration/enumeration permutations that preserve the same descriptors should preserve route and hash. | Register identical packages in opposite order. | Registry APIs sort exposed packages/contributions/providers and descriptor collections snapshot deterministically. No defect found in code review; a metamorphic regression test is still useful. | MEDIUM | HIGH | `Planner_ShouldPreservePlan_WhenPackageRegistrationOrderChanges` |
| UT-PLAN-009 | DOCUMENTED_LIMITATION | Artifact compatibility needs a stable author-controlled semantic identity; the framework need not infer language/version/normalization dimensions automatically. | Same CLR type but different semantic states. | `LanguageArtifactContract` consists of artifact kind + optional stable type/contract identity. Docs explicitly assign stable protocol identity to authors and recommend distinct identities for public contracts. No concrete unsafe composition is proven when authors use the contract as designed. | MEDIUM | HIGH | none until a public-API witness demonstrates two semantically distinct states that cannot be represented |
| UT-PLAN-010 | INCONSISTENT_POLICY | Selected executable contributions should have an explicit “candidate” vs “must execute” semantic. | Feature selects multiple non-pass transformers; cheapest route bypasses one. | Route planning treats non-pass transformations as alternatives. Selection/dependency validity does not imply route reachability/use. Current API does not state that every selected conversion must execute, so this is not promoted to bug. | MEDIUM | MEDIUM | needs contract clarification before assertion |
| UT-PLAN-011 | NOT_A_BUG | Capability/provider ambiguity should not be resolved by registration order or lexical IDs. | Two capability providers / two runtime providers. | Resolver emits `UTL2002` / `UTL2302`; single-owner slot conflict emits `UTL2101`. Explicit `PreferCapabilityProvider`, `UseRuntimeProvider`, or slot replacement is required. | — | HIGH | existing behavior is the control for UT-PLAN-002 |
| UT-PLAN-012 | DOCUMENTED_LIMITATION | A valid plan may be materialized only when exact runtime component sources are supplied; planning itself need not have those future instances. | Build plan from descriptor registration, then omit transformer implementation source at runtime. | Runtime assembler validates exact package/version/manifest/implementation and transformer/executor presence and can reject materialization. Docs explicitly define this as a later materialization boundary, so planning-success/runtime-failure is not itself a planner bug. | MEDIUM | HIGH | existing runtime binding tests already cover representative failures |
| UT-PLAN-013 | NOT_A_BUG | Backend-specific composition may differ when declarations explicitly scope contributions to backends. | Pass supported only on backend A in a two-backend language. | Route construction filters transformations per backend using `SupportedBackends`; docs list backend scope as contribution semantics. No cross-backend semantic-equality invariant is declared. | — | HIGH | none without a stronger cross-backend language contract |
| UT-PLAN-014 | NOT_A_BUG | PlanHash should be stable under semantically irrelevant input ordering and change with selected executable identity/contracts. | Permute definition order constraints/intrinsic directives. | `LanguageDefinition` canonicalizes order constraints and intrinsic policy; PlanHash sorts features/contributions/backends and serializes route steps, IDs, contracts and costs. No same-hash/different-executable witness found. | — | HIGH | registration-permutation control only |
| UT-PLAN-015 | TEST_COVERAGE_GAP | Existing route assertions should distinguish semantic contract from implementation characterization. | Existing Wist tests assert exact contribution sequences. | Canonical Wist route tests verify exact current route IDs, and pass tests verify a declared `Before`; no adversarial equal-cost route, overflow, or globally-valid-alternative witness was found. | HIGH | HIGH | new focused countertest fixture |
| UT-PLAN-016 | SUSPICIOUS_NEEDS_COUNTEREXAMPLE | Provider selection and route feasibility are separate local decisions; if feasibility could safely disambiguate candidates, the architecture currently cannot express that global relation. | Two eligible runtime providers, one with unreachable input and one reachable. | Runtime provider selection happens before route construction and ambiguous providers fail closed. This may be intentional explicit-selection policy; no bug claim without a documented auto-feasibility preference. | LOW | MEDIUM | do not encode as assertion yet |
| UT-PLAN-017 | TEST_COVERAGE_GAP | Dominated/unreachable alternatives that do not participate in semantic selection should not perturb the executable route. | Add strictly more expensive conversion or unreachable transform. | Route search should leave selected route unchanged by cost/graph reasoning; there is no focused metamorphic countertest proving this invariant around lexical signatures and canonicalization. | MEDIUM | MEDIUM | add dominated-alternative green metamorphic test if fixture remains minimal |

## Detailed evidence

### UT-PLAN-001 / UT-PLAN-002 — scalar cost and equal-cost ambiguity

`UniversalToolchain/UniversalToolchain.FeatureSdk/FeatureDescriptors.cs`, `ArtifactTransformationDescriptor` (baseline lines ~21-45) validates only `cost >= 0` and stores one `int Cost`.

`UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageArtifactRoutePhase.cs`, `FindBestRoute` (baseline lines ~158-214) maintains `RouteState(int Cost, string Signature, ...)`. Candidate selection is ordered by cost then lexical signature; signature concatenates contribution IDs. On equal cost, lexical signature explicitly replaces the previous candidate.

There is no route-level equivalent of `PreferCapabilityProvider` or `UseRuntimeProvider`, and no ambiguity diagnostic for equal-cost alternatives. In contrast, `LanguageContributionResolutionPhase.ResolveCapabilityProvider` emits `UTL2002`, `SelectRuntimeProvider` emits `UTL2302`, and `ApplySlotPolicies` emits `UTL2101` for unresolved ownership ambiguity.

Nearest coverage: canonical route tests assert chosen contribution sequences, but no focused test demonstrates that equal-cost semantic alternatives are either equivalent or explicitly selected.

### UT-PLAN-003 — conversion route fixed before selected passes

`LanguageArtifactRoutePhase.Build` (baseline lines ~8-76) filters `conversionEdges`, calls `FindBestRoute`, then calls `InsertPasses`. `InsertPasses` reports `UTL2204` for any selected pass whose contract is absent from that already-fixed route. There is no retry/backtracking with pass feasibility as a constraint.

The architecture docs explicitly say conversions form the minimum-cost route and an unplaceable selected pass fails with `UTL2204`. Therefore the current implementation matches the documented local algorithm. The counterexample is still architectural evidence that route selection is not a global constraint problem: a complete valid LanguagePlan may exist while the planner returns failure.

Nearest coverage: the existing `UTL2204` regression validates failure for an unplaceable pass but does not add an alternative conversion route that would make the selected pass placeable.

### UT-PLAN-004 — base cost and final `TotalCost` are different domains

`LanguageArtifactRoutePhase.FindBestRoute` sees only non-pass transformations. `LanguageArtifactRoute` in `LanguagePlan.cs` computes `TotalCost = Steps.Sum(step => step.Cost)` after passes have been inserted. The low-level `ArtifactTransformationDescriptor` permits non-zero pass costs; the typed `LanguageFeatureBuilder.AddPass` hardcodes cost zero.

Thus a low-level legal descriptor can produce a final route whose `TotalCost` is not minimal among executable routes even though docs and release notes use the phrase “minimum-cost artifact routes”. This is a cost-model inconsistency; the audit does not choose whether the correct future model should optimize final cost, conversion-only cost, or a richer objective.

### UT-PLAN-005 — `int` overflow

Accepted costs span the whole non-negative `int` domain. `FindBestRoute` performs `currentState.Cost + edge.Transformation.Cost` into `int` with no overflow guard. This can wrap a huge path negative and make it lexicographically/cost-wise preferred. Final `LanguageArtifactRoute` construction recomputes the sum with `Enumerable.Sum(int)`, which has different overflow behavior and can throw. `LanguageCompiler.Compile` does not catch this arithmetic failure.

Minimal witness:

```text
Source --100----------------> Target
Source --int.MaxValue--> Mid --int.MaxValue--> Target
```

A legal, obviously dominated route must not become preferred because of arithmetic overflow.

### UT-PLAN-006 — cross-contract pass ordering validation gap

`AppendPassesForContract` builds `candidates` only for the current contract. Its `AfterContributions` and reverse `BeforeContributions` checks filter dependencies through `candidates.ContainsKey`, so ordering edges to selected passes at other route contracts are ignored during insertion.

`LanguagePlanVerifier.ValidateRoute`, however, constructs indexes for all route steps and checks every selected contribution's `BeforeContributions` / `AfterContributions` whenever the referenced step is present. Therefore an impossible cross-contract ordering can survive planning, then throw `LanguagePlanVerificationException` while constructing `LanguagePlan`. That is a missing planning validation / error-boundary defect.

### UT-PLAN-007 — pass tie uses ID as execution order

Within one artifact contract, `AppendPassesForContract` chooses ready passes by `Contribution.Order`, then `Contribution.Id` ordinal. `Before`/`After` can encode a partial order, but equal `Order` with no relation is executable and deterministic. For non-commutative passes, lexical ID therefore becomes semantic execution priority. This is not claimed as a current contract violation; it is a concrete underspecification witness.

### UT-PLAN-008 / UT-PLAN-014 — permutation and hash controls

The audit attempted to falsify permutation stability. `LanguagePackageRegistry` sorts exposed package/contribution collections and capability providers. Descriptor collection snapshots sort IDs. `LanguageDefinition` canonicalizes contribution order constraints and intrinsic policy. `LanguagePlanCanonicalizer` sorts features, contributions and routes, while route step order remains executable order. No PlanHash collision or irrelevant-order instability was proven in this pass.

### UT-PLAN-009 — structural vs semantic compatibility

`LanguageArtifactRoute.ContractsConnect` requires equal artifact kind and equal value-type identity (or both untyped). It does not infer dialect/version/normalization/validation state. However `LanguageArtifactContract.Kind` and explicit contract identity are author-controlled stable protocol identifiers, and docs recommend explicit identities. A package can represent those semantic states by different contracts. Without a witness where the public API makes the distinction impossible, this is not classified as a bug.

### UT-PLAN-012 — runtime availability

`LanguageRouteRuntimeAssembler` validates exact package identity/version/manifest/implementation instance and exact transformer/executor registrations after planning. Its own contract states component loading is materialization only and must not change semantic selection. Existing tests already cover manifest mismatch and missing runtime-provider source. The ability to construct a plan before future runtime sources are supplied is therefore intentional.

## Mandatory hypothesis checklist result

1. Scalar cost: **confirmed underspecification** (UT-PLAN-001).
2. Equal-cost routes: **confirmed silent ID tie policy** (UT-PLAN-002).
3. Base-route vs final-route cost: **confirmed cost-domain inconsistency** (UT-PLAN-004).
4. Route vs mandatory/selected pass: **confirmed global-planning gap, documented local behavior** (UT-PLAN-003).
5. Optimizations/passes changing route validity: **confirmed via UT-PLAN-003**.
6. Local optimum vs global valid plan: **confirmed for route/pass composition; provider variants not promoted without stronger policy**.
7. Hidden ID/lexical policy: **confirmed in route ties and pass ties** (UT-PLAN-002, UT-PLAN-007).
8. Cost arithmetic boundaries: **confirmed bug** (UT-PLAN-005).
9. Structural vs semantic compatibility: **reviewed, no concrete API-impossible witness** (UT-PLAN-009).
10. Provider ambiguity vs route ambiguity: **confirmed inconsistent policy** (UT-PLAN-002 vs UT-PLAN-011).
11. Dependencies/conflicts vs route reachability: **selection does not mean execution for non-pass transforms; contract unclear** (UT-PLAN-010).
12. Pass ordering: **confirmed missing cross-contract validation + tie underspecification** (UT-PLAN-006, UT-PLAN-007).
13. Multi-backend consistency: **backend-specific divergence is explicitly modeled; no bug proven** (UT-PLAN-013).
14. Runtime availability vs planning validity: **intentional materialization boundary** (UT-PLAN-012).
15. PlanHash/canonicalization: **reviewed; no collision/irrelevant-order defect proven** (UT-PLAN-014).
16. Error instead of alternative solution: **confirmed route/pass witness** (UT-PLAN-003).
17. Tests blessing implementation: **coverage gap confirmed** (UT-PLAN-015).

## Highest-value countertests

Priority order:

1. **Cost overflow / dominated huge route** — legal input, concrete arithmetic defect, deterministic, very small witness.
2. **Required pass forces alternative route** — strongest global-planning counterexample; demonstrates failure despite an existing complete plan.
3. **Equal-cost semantic ambiguity** — isolates the hidden lexical-ID semantic policy and contrasts it with fail-closed provider ambiguity.
4. **Impossible cross-contract pass order** — exposes a planning/verifier boundary bug and uncaught exception path.
5. **Registration permutation** — green metamorphic control; proves the fixture is not accidentally registration-order-dependent.
6. **Equal-order non-commutative passes** — architectural underspecification witness; assertion must be labeled proposed safety invariant, not current documented contract.
7. **Base conversion cost vs final total cost** — useful design witness; use low-level descriptors because typed `AddPass` fixes pass cost to zero.
8. **Dominated alternative** — green metamorphic control if it adds distinct coverage after overflow/ambiguity fixtures.

## Adversarial refutation of high-severity findings

- **UT-PLAN-003**: strongest defense is the architecture doc explicitly specifying conversion-first minimum route and `UTL2204` for a selected pass absent from that route. Result: kept as `ARCHITECTURAL_UNDERSPECIFICATION`, not `CONFIRMED_BUG`.
- **UT-PLAN-002**: strongest defense is deterministic reproducibility. Result: determinism is real, but provider subsystems demonstrate that reproducibility and semantic justification are separate policies. Kept as `INCONSISTENT_POLICY`.
- **UT-PLAN-005**: no defense found. Descriptor construction accepts the values; arithmetic can change route ordering and escape `Compile`. Kept `CONFIRMED_BUG`.
- **UT-PLAN-006**: verifier proves the ordering relation is intended to be global over route steps, while insertion filters it locally. No documentation was found declaring such cross-contract constraints invalid at authoring time. Kept `MISSING_VALIDATION`.
- **UT-PLAN-009**: strongest defense succeeds: contract identity is explicitly author-controlled and documented. Downgraded to `DOCUMENTED_LIMITATION` / no bug.
- **UT-PLAN-012**: strongest defense succeeds: runtime materialization is explicitly later and exact-binding validation is deliberate. Kept `DOCUMENTED_LIMITATION`.

## Phase B guardrails

Countertests may be expected red. Assertions must not be weakened to match current behavior. No production implementation file may be modified. Test-fixture errors must be repaired before a failure is accepted as evidence. The final diff must contain only this research artifact and files under test projects/test-only support.