# UniversalToolchain planner/composition adversarial audit

Date: 2026-09-03  
Baseline branch: `master`  
Baseline commit: `7005371d6c30175dff4b0e9f906a26218b0ee54d`  
Audit branch: `audit/planner-countertests-20260903`

This is the Phase A inventory. It intentionally does **not** design or implement production fixes.

## Baseline

The repository default branch is `master`; all code evidence below was re-read at commit `7005371d6c30175dff4b0e9f906a26218b0ee54d` (`docs: add LangDev adversarial defense pack`). The principal ownership path is:

- `UniversalToolchain.Language.Abstractions`: definitions, stable IDs, artifact contracts, runtime policy;
- `UniversalToolchain.FeatureSdk`: package registry, feature/contribution descriptors, transformation metadata;
- `UniversalToolchain.LanguageSdk`: feature/contribution resolution, route/pass planning, `LanguagePlan`, PlanHash and verifier;
- `UniversalToolchain.Runtime`: exact runtime materialization and execution;
- `UniversalToolchain.LanguageAuthoring`: typed authoring facade;
- `UniversalToolchain.LanguageSdk.Tests`: main planning/runtime regression suite.

GitHub Actions baseline for this exact commit is already red. `.NET CI` run `33340972913` failed on Linux and Windows. Linux completed the canonical build/test step and then failed `Enforce canonical entrypoint result`; Windows failed the canonical PowerShell build/test step. The uploaded canonical build log expired on 2026-09-02, so its inner failure cannot now be reconstructed. New countertest failures must therefore be classified separately from this pre-existing CI state.

## End-to-end decision path

`LanguageDefinition / packages`
→ features
→ contribution dependency/capability closure
→ conflicts / slot policies / requirements
→ definition-level ordering
→ runtime-provider selection
→ backend ownership
→ conversion graph
→ conversion route selection
→ same-contract pass insertion/order
→ `LanguageArtifactRoute`
→ `LanguagePlan`
→ PlanHash
→ `LanguagePlanVerifier`
→ runtime component source validation
→ exact transformer/executor binding
→ execution.

`LanguageCompiler.Compile` resolves features/contributions/provider before route construction. `LanguageArtifactRoutePhase.Build` then chooses a conversion-only route with `FindBestRoute` and only afterwards inserts selected passes. Runtime materialization is later and is explicitly forbidden from changing the semantic selection captured in the plan.

## Findings

| ID | Class | Invariant / expected property | Minimal witness | Current behavior | Severity | Confidence | Countertest |
|---|---|---|---|---|---|---|---|
| UT-PLAN-001 | ARCHITECTURAL_UNDERSPECIFICATION | Route choice should not silently decide semantics when the model provides no semantic preference/equivalence relation. | Two equal-cost legal routes with different transformers. | One non-negative scalar `int Cost` is the only route preference domain. | HIGH | HIGH | `Planner_ShouldNotResolveSemanticRouteAmbiguityByContributionIdAlone` |
| UT-PLAN-002 | INCONSISTENT_POLICY | Unresolved semantic alternatives should follow an explicit ambiguity policy rather than a technical-name priority. | Equal-cost `Source -> Target` alternatives. | `FindBestRoute` resolves equal cost by lexical concatenated `ContributionId` signature; capability/runtime-provider/single-owner ambiguities instead fail with `UTL2002`/`UTL2302`/`UTL2101`. | HIGH | HIGH | same equal-cost witness |
| UT-PLAN-003 | ARCHITECTURAL_UNDERSPECIFICATION | Proposed safety invariant: selected mandatory pass feasibility should participate in route choice; a planner should not reject when another conversion route yields a complete valid plan. | Cheap `Source -> T`; dearer `Source -> M -> T`; selected pass `M -> M`. | Conversion path is fixed first; `InsertPasses` then emits `UTL2204` and never retries the alternative. Current docs explicitly describe conversion-first + `UTL2204`, so this is a global-planning gap, not a violation of the documented algorithm. | HIGH | HIGH | `Planner_ShouldNotRejectGloballyValidRoute_WhenSelectedPassRequiresAlternativePath` |
| UT-PLAN-004 | NOT_A_BUG | Among routes that satisfy the current mandatory-pass semantics, minimizing conversion cost should also minimize final `TotalCost`. | Compare two valid routes under the same selected pass set. | Every selected pass must be placed exactly once or planning fails `UTL2204`; therefore the sum of selected pass costs is constant across valid routes for one backend. The seed “route B without the selected pass” is not valid under the current contract. | — | HIGH | no red test; retain only a control if needed |
| UT-PLAN-005 | CONFIRMED_BUG | Every non-negative cost accepted by the public descriptor API must be processed without overflow corruption or an uncaught compiler exception. | Direct cost `100`; dominated two-edge path `int.MaxValue + int.MaxValue`. | Route search adds `int` directly with no overflow guard; the huge path can wrap negative and become preferred. Final `LanguageArtifactRoute.TotalCost` recomputes through `Sum(int)` and can throw `OverflowException`; `Compile` does not translate this into a diagnostic. | CRITICAL | HIGH | `Planner_ShouldNotPreferOverflowedRoute_WhenCostsExceedIntRange` |
| UT-PLAN-006 | MISSING_VALIDATION | Explicit `Before`/`After` constraints that cannot be satisfied by route topology should fail during planning as diagnostics, not survive until `LanguagePlanVerifier` throws. | Pass on earlier contract A declares `After` pass on later contract B. | `AppendPassesForContract` filters ordering refs to candidates on the current contract, ignoring cross-contract edge; verifier later evaluates both route indexes and rejects the order. | HIGH | HIGH | `Planner_ShouldReportImpossibleCrossContractPassOrder_AsPlanningFailure` |
| UT-PLAN-007 | ARCHITECTURAL_UNDERSPECIFICATION | Non-commutative equal-order passes need an explicit semantic order/equivalence contract. | Two passes on one contract, same `Order`, no `Before`/`After`, transforms `+1` and `*2`. | Ready set is ordered by `Order`, then lexical `ContributionId`; stable identity becomes execution priority. | MEDIUM | HIGH | `Planner_ShouldNotUseContributionIdAsSemanticOrderForEqualOrderPasses` (proposed safety invariant) |
| UT-PLAN-008 | TEST_COVERAGE_GAP | Package/registration permutations that preserve descriptors should preserve selected executable route and PlanHash. | Register frontend and execution packages in opposite order. | Registry/provider collections and descriptor snapshots are canonicalized/sorted. No code defect found; focused metamorphic coverage is still missing. | MEDIUM | HIGH | `Planner_ShouldPreservePlan_WhenPackageRegistrationOrderChanges` |
| UT-PLAN-009 | DOCUMENTED_LIMITATION | Semantic state must be representable by author-selected artifact identities; framework need not infer dialect/version/normalization dimensions. | Same CLR type carrying two semantic states. | Contract = artifact kind + optional stable type/contract identity. Docs explicitly make the protocol identity author-controlled and recommend explicit identities. No API-impossible witness was found. | — | HIGH | none |
| UT-PLAN-010 | ARCHITECTURAL_UNDERSPECIFICATION | Selected non-pass contributions need an explicit “candidate alternative” vs “must execute” meaning. | Feature selects a conversion that the chosen route bypasses. | Non-pass selected transformations are treated as route alternatives; dependency/conflict selection does not imply execution. Current public contract does not state that every selected conversion must execute. | MEDIUM | MEDIUM | needs contract decision before assertion |
| UT-PLAN-011 | NOT_A_BUG | Capability/provider ambiguity should be fail-closed unless explicitly selected. | Two capability or runtime providers. | Resolver already emits `UTL2002` / `UTL2302`; single-owner slot conflict emits `UTL2101`. | — | HIGH | serves as control for UT-PLAN-002 |
| UT-PLAN-012 | DOCUMENTED_LIMITATION | Planning may precede runtime materialization, provided later binding validates exact provenance and implementations. | Descriptor-only plan, then missing component source. | Runtime assembler validates package/version/API/manifest/implementation instance plus exact transformer/executor presence. This later failure boundary is documented and intentional. | — | HIGH | existing binding tests sufficient |
| UT-PLAN-013 | NOT_A_BUG | Backend-specific routes may differ when declarations explicitly scope contributions by backend. | Pass supported only on backend A. | `LanguageArtifactRoutePhase.Build` filters transformations by `SupportedBackends`; backend scope is documented contribution semantics. | — | HIGH | none without stronger cross-backend invariant |
| UT-PLAN-014 | NOT_A_BUG | PlanHash should be insensitive to irrelevant input ordering while including executable identities/contracts. | Permute order constraints/intrinsic directives. | `LanguageDefinition` canonicalizes these collections; canonicalizer sorts features/contributions/routes and serializes executable route steps. No collision/ordering defect found. | — | HIGH | registration-permutation control |
| UT-PLAN-015 | TEST_COVERAGE_GAP | Route tests should distinguish true semantic invariants from characterization of the current chosen IDs/order. | Existing exact Wist route assertions versus equal-cost counterfactuals. | Existing tests strongly verify canonical routes and declared pass ordering but no focused equal-cost ambiguity, overflow, or globally-valid-alternative witness was found. | HIGH | HIGH | new adversarial fixture |
| UT-PLAN-016 | SUSPICIOUS_NEEDS_COUNTEREXAMPLE | If downstream route feasibility is intended to disambiguate providers, the current phase ordering cannot express that relation. | Two runtime providers, one unreachable input and one reachable. | Provider ambiguity is resolved/fails before route construction. Explicit selection may be intentional policy, so no bug claim. | LOW | MEDIUM | none yet |
| UT-PLAN-017 | TEST_COVERAGE_GAP | A strictly dominated/unreachable conversion alternative should not change the chosen route. | Add more-expensive or unreachable edge. | Graph algorithm should preserve the route; no focused metamorphic regression was found. | LOW | MEDIUM | optional green control |

## Code evidence

### Route selection, pass insertion, and ID tie-break

`UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageArtifactRoutePhase.cs` at baseline commit:

- `Build`, lines 8–76: selects backend owner/target; builds `conversionEdges` from non-pass transformations; calls `FindBestRoute`; only then calls `InsertPasses`.
- `InsertPasses`, approximately lines 79–113: walks the already-selected conversion steps and emits `UTL2204` if selected passes remain.
- `AppendPassesForContract`, approximately lines 114–158: computes only same-contract candidates; ordering dependencies are filtered through that local candidate set; ready passes are `OrderBy(Order).ThenBy(ContributionId)`.
- `FindBestRoute`, approximately lines 160–215: state is `(int Cost, string Signature, Steps)`; pending nodes and equal-cost candidates tie-break on lexical signature built from contribution IDs; cost accumulation is `currentState.Cost + edge.Transformation.Cost` with no overflow guard.

### Cost domain

`UniversalToolchain/UniversalToolchain.FeatureSdk/FeatureDescriptors.cs`, `ArtifactTransformationDescriptor`, lines ~21–45 accepts every `int cost >= 0`; there is no upper-bound or checked-sum contract. `LanguageFeatureBuilder.AddPass` in `UniversalToolchain.LanguageAuthoring` fixes typed authored passes to cost 0, but the lower-level descriptor remains public and allows non-zero pass cost.

`UniversalToolchain/UniversalToolchain.LanguageSdk/LanguagePlan.cs`, `LanguageArtifactRoute` computes `TotalCost = Steps.Sum(step => step.Cost)` after pass insertion.

Adversarial correction: this does **not** prove a base-vs-final minimum bug in the current mandatory-pass model. Any valid route must contain every selected pass exactly once, so pass-cost sum is constant among valid routes. The genuine failure is UT-PLAN-003: the conversion-minimum route can be infeasible for the selected pass set even when another valid route exists.

### Ambiguity policies

`UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageContributionResolutionPhase.cs`:

- `SelectRuntimeProvider` emits `UTL2301` for none and `UTL2302` for multiple candidates;
- `ResolveCapabilityProvider` emits `UTL2002` when multiple providers remain without `PreferCapabilityProvider`;
- `ApplySlotPolicies` emits `UTL2101` for unresolved single-owner/replacement ambiguity;
- definition-level topological ready ties use `Order` then ID only after constraints are validated.

This makes silent equal-cost route selection a real cross-subsystem policy inconsistency even though it is deterministic.

### Verifier boundary

`UniversalToolchain/UniversalToolchain.LanguageSdk/LanguagePlanVerifier.cs`, `ValidateRoute`, records indexes for every route step and enforces every selected step's `BeforeContributions` / `AfterContributions` whenever the referenced contribution is present. This is broader than the per-contract filtering done during pass insertion and creates the UT-PLAN-006 planner/verifier gap.

### Structural contract boundary

`UniversalToolchain/UniversalToolchain.Language.Abstractions/ArtifactContracts.cs` defines `LanguageArtifactContract` as stable artifact kind plus optional stable value/contract identity. `LanguageArtifactRoute.ContractsConnect` requires exact kind and identity compatibility. Architecture docs explicitly assign stable protocol identity to language/package authors. No claim is made that the framework infers normalization/version/validation states.

### Runtime materialization

`UniversalToolchain/UniversalToolchain.Runtime/LanguageRouteRuntimeAssembler.cs` states that component loading is materialization only and must not expand/change plan semantics. It validates exact package identity/version/API/manifest/implementation provenance and exact transformer/executor registrations. Planning-success/runtime-materialization-failure is therefore intentional when required runtime sources are not supplied.

### PlanHash/canonicalization

`LanguageDefinition` sorts/deduplicates contribution order constraints and canonicalizes intrinsic policy. `LanguagePlanCanonicalizer` sorts features, contributions and routes and writes each executable route step with contribution ID, source, target and cost. No semantically-different/same-hash or irrelevant-order/different-hash witness was proven in this audit.

## Existing-test blind spots

Nearest existing coverage includes:

- exact Wist canonical route sequence assertions;
- same-artifact pass execution with an explicit `Before` relation;
- an `UTL2204` unplaceable-pass test;
- capability conflict/alternative executor tests;
- runtime manifest/source binding rejection tests;
- canonicalization/typed-contract tests.

What these do **not** isolate is the counterfactual state space: equal-cost alternative routes, selected pass with an alternative feasible conversion route, cost overflow, impossible cross-contract pass order, and registration-order metamorphism. Exact current route assertions can therefore characterize a tie-break without proving why that route is semantically required.

## Mandatory checklist result

1. Scalar cost — **underspecified** (UT-PLAN-001).
2. Equal-cost route ambiguity — **confirmed silent ID tie** (UT-PLAN-002).
3. Base-route vs final-route cost — **refuted as a bug under current mandatory-pass semantics** (UT-PLAN-004).
4. Route vs mandatory/selected passes — **global-planning gap confirmed** (UT-PLAN-003).
5. Optimizations/passes changing route validity — **confirmed via UT-PLAN-003**.
6. Local optimum vs global valid plan — **confirmed for route/pass phase**.
7. Hidden ID/lexical policy — **confirmed for route ties and equal-order passes** (UT-PLAN-002, UT-PLAN-007).
8. Cost arithmetic boundaries — **confirmed bug** (UT-PLAN-005).
9. Structural vs semantic compatibility — **reviewed; no API-impossible witness** (UT-PLAN-009).
10. Provider ambiguity vs route ambiguity — **confirmed inconsistent policy** (UT-PLAN-002 vs UT-PLAN-011).
11. Dependency/conflict vs route reachability — **candidate-vs-mandatory semantics unclear** (UT-PLAN-010).
12. Pass ordering — **confirmed validation gap + underspecified equal-order fallback** (UT-PLAN-006/007).
13. Multi-backend consistency — **explicit backend scope; no bug proven** (UT-PLAN-013).
14. Runtime availability vs planning validity — **intentional materialization boundary** (UT-PLAN-012).
15. PlanHash/canonicalization — **no defect proven in reviewed permutations** (UT-PLAN-014).
16. Error instead of alternative solution — **confirmed route/pass witness** (UT-PLAN-003).
17. Tests blessing implementation — **coverage gap confirmed** (UT-PLAN-015).

## Highest-value countertests

1. `Planner_ShouldNotPreferOverflowedRoute_WhenCostsExceedIntRange` — concrete legal-input arithmetic defect.
2. `Planner_ShouldNotRejectGloballyValidRoute_WhenSelectedPassRequiresAlternativePath` — global-valid-plan counterexample.
3. `Planner_ShouldNotResolveSemanticRouteAmbiguityByContributionIdAlone` — hidden semantic tie policy.
4. `Planner_ShouldReportImpossibleCrossContractPassOrder_AsPlanningFailure` — planner/verifier error-boundary defect.
5. `Planner_ShouldPreservePlan_WhenPackageRegistrationOrderChanges` — green metamorphic control.
6. `Planner_ShouldNotUseContributionIdAsSemanticOrderForEqualOrderPasses` — proposed safety invariant / design-ambiguity witness.

A base-vs-final-cost RED test is deliberately **not** in this list after adversarial refutation: with the current rule that all selected passes must be placed exactly once, its proposed cheaper route without the pass is not a valid alternative.

## Adversarial refutation of high-severity findings

- **UT-PLAN-003:** docs explicitly specify conversion-first route + `UTL2204`; retained as `ARCHITECTURAL_UNDERSPECIFICATION`, not implementation bug.
- **UT-PLAN-002:** determinism is a valid defense but not semantic justification; fail-closed provider policies make the inconsistency concrete.
- **UT-PLAN-004:** refutation succeeds; downgraded to `NOT_A_BUG` for the current mandatory-pass model.
- **UT-PLAN-005:** no defense found; legal public costs can overflow planner arithmetic.
- **UT-PLAN-006:** verifier itself demonstrates that `Before`/`After` is intended to constrain global route-step order when both contributions are present; local insertion fails to validate impossible cross-contract constraints.
- **UT-PLAN-009:** refutation succeeds because semantic protocol identity is explicitly author-controlled.
- **UT-PLAN-012:** refutation succeeds because later exact materialization is documented architecture.

## Phase B guardrails

Countertests may be intentionally red. Do not weaken assertions to match current behavior. If a test fails for fixture/compile reasons, fix only the test. Do not modify production. Final diff must contain only this audit artifact and test-project/test-only files.