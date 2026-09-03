# UniversalToolchain planner/composition adversarial audit

Date: 2026-09-03

Baseline branch: `master`

Baseline commit: `7005371d6c30175dff4b0e9f906a26218b0ee54d`

Audit branch: `audit/planner-countertests-20260903`

This is the Phase A inventory plus the mandatory second-wave audit after the first countertests. It intentionally does **not** design or implement production fixes.

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
| UT-PLAN-004 | NOT_A_BUG | Among routes that satisfy the current mandatory-pass semantics, minimizing conversion cost should also minimize final `TotalCost`. | Compare two valid routes under the same selected pass set. | Every selected pass must be placed exactly once or planning fails `UTL2204`; therefore the selected pass-cost sum is constant across valid routes. The seed “route B without the selected pass” is not valid under the current contract. | — | HIGH | no red test |
| UT-PLAN-005 | CONFIRMED_BUG | Every non-negative cost accepted by the public descriptor API must be processed without overflow corruption or an uncaught compiler exception. | Direct cost `100`; dominated path `int.MaxValue + int.MaxValue`. | Route search adds `int` directly with no overflow guard; the huge path can wrap negative and become preferred. Final `LanguageArtifactRoute.TotalCost` recomputes through `Sum(int)` and can throw `OverflowException`; `Compile` does not translate this into a diagnostic. | CRITICAL | HIGH | `Planner_ShouldNotPreferOverflowedRoute_WhenCostsExceedIntRange` |
| UT-PLAN-006 | MISSING_VALIDATION | Explicit descriptor `Before`/`After` constraints that cannot be satisfied by route topology should fail during planning as diagnostics, not survive until `LanguagePlanVerifier` throws. | Pass on earlier contract A declares `After` pass on later contract B. | `AppendPassesForContract` filters ordering refs to candidates on the current contract, ignoring the cross-contract edge; verifier later evaluates both route indexes and rejects the order. | HIGH | HIGH | `Planner_ShouldReportImpossibleCrossContractPassOrder_AsPlanningFailure` |
| UT-PLAN-007 | ARCHITECTURAL_UNDERSPECIFICATION | Non-commutative equal-order passes need an explicit semantic order/equivalence contract. | Two passes on one contract, same `Order`, no `Before`/`After`, transforms `+1` and `*2`. | Ready set is ordered by `Order`, then lexical `ContributionId`; stable identity becomes execution priority. | MEDIUM | HIGH | `Planner_ShouldNotUseContributionIdAsSemanticOrderForEqualOrderPasses` |
| UT-PLAN-008 | TEST_COVERAGE_GAP | Package/registration permutations that preserve descriptors should preserve selected executable route and PlanHash. | Register frontend and execution packages in opposite order. | Registry/provider collections and descriptor snapshots are canonicalized/sorted. No defect found in code review; focused metamorphic coverage was missing. | MEDIUM | HIGH | `Planner_ShouldPreservePlan_WhenPackageRegistrationOrderChanges` |
| UT-PLAN-009 | DOCUMENTED_LIMITATION | Semantic state must be representable by author-selected artifact identities; framework need not infer dialect/version/normalization dimensions. | Same CLR type carrying two semantic states. | Contract = artifact kind + optional stable type/contract identity. Docs explicitly make the protocol identity author-controlled and recommend explicit identities. No API-impossible witness was found. | — | HIGH | none |
| UT-PLAN-010 | ARCHITECTURAL_UNDERSPECIFICATION | Selected non-pass contributions need an explicit “candidate alternative” vs “must execute” meaning. | Feature selects a conversion that the chosen route bypasses. | Non-pass selected transformations are treated as route alternatives; dependency/conflict selection does not imply execution. Current public contract does not state that every selected conversion must execute. | MEDIUM | MEDIUM | needs contract decision before assertion |
| UT-PLAN-011 | NOT_A_BUG | Capability/provider ambiguity should be fail-closed unless explicitly selected. | Two capability or runtime providers. | Resolver emits `UTL2002` / `UTL2302`; single-owner slot conflict emits `UTL2101`. | — | HIGH | control for UT-PLAN-002 |
| UT-PLAN-012 | DOCUMENTED_LIMITATION | Planning may precede runtime materialization, provided later binding validates exact provenance and implementations. | Descriptor-only plan, then missing component source. | Runtime assembler validates package/version/API/manifest/implementation instance plus exact transformer/executor presence. This later failure boundary is documented and intentional. | — | HIGH | existing binding tests sufficient |
| UT-PLAN-013 | NOT_A_BUG | Backend-specific routes may differ when declarations explicitly scope contributions by backend. | Pass supported only on backend A. | Route construction filters transformations by `SupportedBackends`; backend scope is documented contribution semantics. | — | HIGH | none without stronger cross-backend invariant |
| UT-PLAN-014 | NOT_A_BUG | PlanHash should be insensitive to irrelevant input ordering while including executable identities/contracts. | Permute order constraints/intrinsic directives. | `LanguageDefinition` canonicalizes these collections; canonicalizer sorts features/contributions/routes and serializes executable route steps. No collision/ordering defect was proven. | — | HIGH | registration-permutation control |
| UT-PLAN-015 | TEST_COVERAGE_GAP | Route tests should distinguish true semantic invariants from characterization of current chosen IDs/order. | Existing exact Wist route assertions versus equal-cost counterfactuals. | Existing tests verify canonical routes and declared pass ordering but no focused equal-cost ambiguity, overflow, or globally-valid-alternative witness was found. | HIGH | HIGH | adversarial countertest fixture |
| UT-PLAN-016 | SUSPICIOUS_NEEDS_COUNTEREXAMPLE | If downstream route feasibility is intended to disambiguate providers, current phase ordering cannot express that relation. | Two runtime providers, one unreachable input and one reachable. | Provider ambiguity is resolved/fails before route construction. Explicit selection may be intentional policy, so no bug claim. | LOW | MEDIUM | none yet |
| UT-PLAN-017 | TEST_COVERAGE_GAP | A strictly dominated/unreachable conversion alternative should not change the chosen route. | Add more-expensive or unreachable edge. | Graph algorithm should preserve the route; no focused metamorphic regression was found. | LOW | MEDIUM | optional green control |
| UT-PLAN-018 | CONFIRMED_BUG | Explicit definition-level `OrderContributionBefore/After` for executable passes must constrain the executable route, not only the presentation order of `LanguagePlan.Contributions`. | Same-contract passes `a-add` and `z-multiply`; definition says `z-multiply` before `a-add`. | `ApplyDefinitionOrder` records `z` before `a` in `plan.Contributions` and PlanHash/lock policy, but `AppendPassesForContract` ignores definition constraints and reorders by descriptor `Order` then ID, executing `a` before `z`. `LanguagePlanVerifier` does not check definition-level constraints against route steps. | CRITICAL | HIGH | `Planner_ShouldApplyDefinitionContributionOrder_ToExecutablePassRoute` |

## Detailed evidence

### UT-PLAN-001 / UT-PLAN-002 — scalar cost and equal-cost ambiguity

`UniversalToolchain/UniversalToolchain.FeatureSdk/FeatureDescriptors.cs`, `ArtifactTransformationDescriptor`, accepts only `cost >= 0` and stores one `int Cost`.

`UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageArtifactRoutePhase.cs`, `FindBestRoute`, maintains `RouteState(int Cost, string Signature, ...)`. Candidate selection is ordered by cost then lexical signature; signature concatenates contribution IDs. Equal cost therefore becomes a lexical `ContributionId` choice.

There is no route-level analogue of `PreferCapabilityProvider` or `UseRuntimeProvider`. In contrast, `LanguageContributionResolutionPhase.ResolveCapabilityProvider` emits `UTL2002`, `SelectRuntimeProvider` emits `UTL2302`, and `ApplySlotPolicies` emits `UTL2101` for unresolved ownership ambiguity.

### UT-PLAN-003 — conversion route fixed before selected passes

`LanguageArtifactRoutePhase.Build` filters `conversionEdges`, calls `FindBestRoute`, and only then calls `InsertPasses`. `InsertPasses` reports `UTL2204` for any selected pass whose contract is absent from that already-fixed route. There is no retry/backtracking with pass feasibility as a route constraint.

The architecture docs explicitly say conversions form the minimum-cost route and an unplaceable selected pass fails with `UTL2204`. Therefore current implementation matches the documented local algorithm. The counterexample is still architectural evidence that route planning is not a global constraint problem: a complete valid LanguagePlan may exist while the planner returns failure.

### UT-PLAN-004 — adversarial refutation of the seed base-vs-final-cost claim

`LanguageArtifactRoute` computes `TotalCost` after pass insertion, while `FindBestRoute` sees only non-pass conversions. At first glance that looks inconsistent. However every selected pass must be inserted exactly once or `UTL2204` fails the plan. For any two valid routes under one selected pass set, the pass-cost sum is therefore constant. Minimizing conversion cost consequently minimizes final total cost among valid routes.

The real defect class is UT-PLAN-003: the locally minimum conversion route can be invalid for the mandatory pass set while another conversion route is globally valid.

### UT-PLAN-005 — cost overflow

`ArtifactTransformationDescriptor` accepts the full non-negative `int` range. `FindBestRoute` performs `currentState.Cost + edge.Transformation.Cost` with no overflow guard. A path containing two `int.MaxValue` edges can wrap and appear cheaper than a direct cost-100 route. Later `LanguageArtifactRoute.TotalCost = Steps.Sum(...)` may throw `OverflowException`, and `LanguageCompiler.Compile` has no arithmetic diagnostic boundary.

### UT-PLAN-006 — cross-contract descriptor pass ordering gap

`AppendPassesForContract` builds candidates only for the current contract. Its `AfterContributions` and reverse `BeforeContributions` checks filter references through that candidate set, so cross-contract ordering edges are ignored during insertion.

`LanguagePlanVerifier.ValidateRoute`, however, indexes all route steps and enforces each contribution descriptor's `BeforeContributions` / `AfterContributions` whenever the referenced step is present. An impossible cross-contract order can therefore survive planner insertion and throw during `LanguagePlan` construction instead of producing a planning diagnostic.

### UT-PLAN-007 — equal-order pass ID fallback

Within one artifact contract, `AppendPassesForContract` chooses ready passes by `Contribution.Order` and then ordinal `Contribution.Id`. `Before`/`After` can express a partial order, but equal `Order` with no relation is executable. For non-commutative passes, lexical ID is an undeclared semantic priority.

### UT-PLAN-008 / UT-PLAN-014 — metamorphic and hash controls

The audit attempted to falsify permutation stability. `LanguagePackageRegistry` sorts exposed package/contribution collections and capability provider lists. Descriptor snapshots are canonicalized. `LanguageDefinition` sorts/deduplicates contribution order constraints and intrinsic policy. `LanguagePlanCanonicalizer` sorts features/contributions/routes while preserving route-step execution order. No PlanHash collision or irrelevant-order instability was proven.

### UT-PLAN-009 — structural vs semantic compatibility

`LanguageArtifactRoute.ContractsConnect` requires equal artifact kind and equal value-type identity (or both untyped). It does not infer dialect/version/normalization/validation state. However artifact kind and explicit contract identity are author-controlled stable protocol identifiers, and docs recommend explicit identities. A package can represent those semantic states with distinct contracts. Without a public-API witness where the distinction cannot be expressed, this is not a bug.

### UT-PLAN-012 — runtime availability boundary

`LanguageRouteRuntimeAssembler` validates exact package identity/version/API/manifest/implementation instance and exact transformer/executor registrations after planning. Its contract explicitly states that component loading is materialization only and must not alter plan semantics. Runtime materialization failure due to missing sources is therefore intentional.

### UT-PLAN-018 — definition order is not executable pass order

Second-wave search generalized the “technical order vs semantic order” problem beyond route tie-breaking.

`LanguageDefinitionBuilder.OrderContributionBefore/After` is a public semantic policy surface. `LanguageContributionResolutionPhase.ApplyDefinitionOrder` validates the constraint and topologically reorders the selected contributions. Existing `DefinitionPolicyPlanningTests.DefinitionOrder_IsResolvedByLanguageCompilerAndPreservedByPlan` verifies that `plan.Contributions` changes and PlanHash changes.

But route construction does not consume definition-level constraints. For same-contract passes, `AppendPassesForContract` independently uses descriptor `BeforeContributions`/`AfterContributions`, then `Order`, then ID. `LanguagePlanVerifier` validates descriptor ordering only; it never checks `LanguageDefinition.ContributionOrderConstraints` against route indexes.

Minimal witness:

```text
source -> artifact
artifact --pass a-add(+1)--> artifact
artifact --pass z-multiply(*2)--> artifact
definition: z-multiply BEFORE a-add
artifact -> backend executor
```

The plan-level selected contribution order is `z, a`; the executable pass route is `a, z`. For input `1`, these orders produce `3` vs `4`, so this is not serialization-only drift.

## Existing-test blind spots

Nearest existing coverage includes:

- exact Wist canonical route sequence assertions;
- same-artifact pass execution with descriptor-level explicit `Before`;
- an `UTL2204` unplaceable-pass test;
- capability conflict/alternative executor tests;
- runtime manifest/source binding rejection tests;
- canonicalization/typed-contract tests;
- definition-level order tests that assert `LanguagePlan.Contributions` and PlanHash but not executable route order.

Missing counterfactual state space included equal-cost alternative routes, selected pass with an alternative feasible conversion route, cost overflow, impossible cross-contract descriptor ordering, definition-level order versus runtime route order, and registration-order metamorphism.

## Mandatory hypothesis checklist result

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
12. Pass ordering — **confirmed descriptor validation gap, ID fallback underspecification, and definition-order execution bug** (UT-PLAN-006/007/018).
13. Multi-backend consistency — **explicit backend scope; no bug proven** (UT-PLAN-013).
14. Runtime availability vs planning validity — **intentional materialization boundary** (UT-PLAN-012).
15. PlanHash/canonicalization — **no generic canonicalization defect proven; UT-PLAN-018 instead shows hash/plan policy can disagree with route execution**.
16. Error instead of alternative solution — **confirmed route/pass witness** (UT-PLAN-003).
17. Tests blessing implementation — **coverage gap confirmed** (UT-PLAN-015).

## Highest-value countertests

1. `Planner_ShouldApplyDefinitionContributionOrder_ToExecutablePassRoute` — explicit public policy is recorded but not executed.
2. `Planner_ShouldNotPreferOverflowedRoute_WhenCostsExceedIntRange` — legal-input arithmetic defect.
3. `Planner_ShouldNotRejectGloballyValidRoute_WhenSelectedPassRequiresAlternativePath` — global-valid-plan counterexample.
4. `Planner_ShouldNotResolveSemanticRouteAmbiguityByContributionIdAlone` — hidden semantic tie policy.
5. `Planner_ShouldReportImpossibleCrossContractPassOrder_AsPlanningFailure` — planner/verifier error-boundary defect.
6. `Planner_ShouldPreservePlan_WhenPackageRegistrationOrderChanges` — green metamorphic control.
7. `Planner_ShouldNotUseContributionIdAsSemanticOrderForEqualOrderPasses` — proposed safety invariant/design-ambiguity witness.

A base-vs-final-cost RED test is deliberately absent after adversarial refutation: a route omitting a selected pass is not a valid route in the current model.

## Adversarial refutation of high-severity findings

- **UT-PLAN-003:** docs explicitly specify conversion-first route + `UTL2204`; retained as `ARCHITECTURAL_UNDERSPECIFICATION`, not `CONFIRMED_BUG`.
- **UT-PLAN-002:** determinism is a real property but not semantic justification; fail-closed provider policies make the cross-subsystem inconsistency concrete.
- **UT-PLAN-004:** refutation succeeds; downgraded to `NOT_A_BUG` for the current mandatory-pass model.
- **UT-PLAN-005:** no defense found; legal public costs can overflow planner arithmetic.
- **UT-PLAN-006:** verifier itself treats descriptor `Before`/`After` as a route-wide constraint whenever both steps exist; local insertion fails to validate impossible cross-contract constraints.
- **UT-PLAN-009:** refutation succeeds because semantic protocol identity is explicitly author-controlled.
- **UT-PLAN-012:** refutation succeeds because later exact materialization is documented architecture.
- **UT-PLAN-018:** strongest defense is that definition-level order might be intended to order only the selected-contribution list. That interpretation conflicts with the public names `OrderContributionBefore/After`, the policy's inclusion in plan identity/lock, and the absence of a separate “display order” concept. The concrete non-commutative pass witness makes executable divergence observable. Retained `CONFIRMED_BUG`.

## Phase B guardrails

Countertests may be intentionally red. Assertions must not be weakened to match current behavior. If a test fails for fixture/compile reasons, repair only the test. Production implementation must remain untouched. Final diff must contain only this audit artifact and test-project/test-only files.