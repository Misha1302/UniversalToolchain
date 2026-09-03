# Planner/composition iterative repair — final finding status

Date: 2026-09-03

Baseline: `master@7005371d6c30175dff4b0e9f906a26218b0ee54d`

Repair branch: `repair/planner-iterative-20260903`

This document is the final disposition ledger for the independent planner/composition audit. It separates confirmed defects from policy decisions, test gaps, documented limitations, and non-bugs. A finding is not called fixed merely because a verifier catches it after plan construction: planner-owned failures must be diagnosed during planning.

## Adopted planning contract

The repair adopts Model C from `planner-routing-policy-decision-2026-09-03.md`:

1. semantic feasibility and mandatory constraints are hard constraints;
2. `ArtifactTransformationDescriptor.Cost` and contribution `Order` are explicit preferences only after feasibility;
3. semantically distinct equal-best choices without explicit policy fail closed;
4. `LanguageContributionId` is identity/canonicalization data, never a semantic tie-break policy;
5. selected same-contract passes are mandatory in their applicable backend scope;
6. selected non-pass transformations are route candidates rather than mandatory execution steps.

## Finding ledger

| Finding | Status | Witness / root cause | Repair / evidence | Remaining limitation |
|---|---|---|---|---|
| UT-PLAN-001 | FIXED | Two equal-cost executable conversion routes could be semantically different while the planner silently selected one. | `92ba1b3009d6386043c913bc13d3c9c05dc704b7` makes unresolved equal-best route alternatives fail with planning ambiguity. Frozen ambiguity countertest is green after repair. | Explicit route-preference API beyond existing cost/order policy is future design work. |
| UT-PLAN-002 | FIXED | Route search used a lexical contribution-ID signature to decide an equal-cost semantic choice. | `92ba1b3…` removes contribution ID from semantic route selection; IDs remain only for stable representation/diagnostics. Metamorphic controls cover irrelevant/dominated alternatives. | IDs still legitimately appear in canonical plans and lock files. |
| UT-PLAN-003 | FIXED | Cheapest conversion skeleton was selected before mandatory pass placement, so a globally feasible more-expensive route could be rejected. | `92ba1b3…` searches conversion topology together with mandatory-pass coverage and chooses among fully feasible route states. Frozen global-pass countertest and route controls are green. | Search-state growth is `NEEDS_MEASUREMENT`; no performance claim is made. |
| UT-PLAN-004 | NOT_A_BUG | Audit concern: an unplaceable selected pass caused planning failure. | Under the adopted contract, selected in-scope passes are mandatory, therefore an unplaceable pass correctly fails planning (`UTL2204`). | Authors must scope optional behavior by feature/backend selection rather than relying on silent pass omission. |
| UT-PLAN-005 | FIXED | Aggregate route cost was effectively constrained by `int`; valid multi-edge routes could wrap or throw during `Sum`. | `0d6d6e81e132d0b10f1d10798f3134953616a55b` changes aggregate `LanguageArtifactRoute.TotalCost` and search accumulation to `long`, preserving per-edge `int Cost`. Tests cover single `int.MaxValue`, overflow alternative, and a three-edge `int.MaxValue` chain. | Public `TotalCost` is an intentional source/API type migration from `int` to `long`. |
| UT-PLAN-006 | FIXED | Descriptor-level cross-contract `Before/After` could survive planning and first fail in `LanguagePlanVerifier`. | `a04ac506486a4f83c12a2031c4c15fdc000963e0` validates descriptor order against the executable route during planning and emits `UTL2206`; verifier remains defense-in-depth. | None known for the audited contract. |
| UT-PLAN-007 | FIXED | Ready passes with equal `Order` and no explicit dependency were ordered lexically by ID, changing observable semantics. | `92ba1b3…` treats equal-preference unordered mandatory passes as semantic ambiguity and fails closed. Frozen non-commutative pass-tie countertest is green. | Authors must provide explicit order when equal-order pass effects are not semantically interchangeable. |
| UT-PLAN-008 | TEST_GAP_CLOSED | Determinism under package-registration permutation needed an explicit regression witness. | Frozen registration-order permutation test plus additional authored-input permutation controls are present and green. | This proves the tested permutation dimensions, not arbitrary hostile extension behavior. |
| UT-PLAN-009 | DOCUMENTED_LIMITATION | Artifact-kind/contract identity is author-declared; the planner does not prove that two user-defined contracts mean the same semantics. | No production change: this is an authority boundary, not a routing bug. Typed contract compatibility remains the planner’s available evidence. | Semantic equivalence of independently authored artifact contracts requires a separate trust/type-contract design. |
| UT-PLAN-010 | POLICY_DECIDED | Audit found unclear ownership of whether every selected transformation must execute. | Model C explicitly distinguishes mandatory selected passes from candidate non-pass conversions; route search and diagnostics implement that distinction. | Future transformation kinds would need an explicit classification rather than inheriting behavior accidentally. |
| UT-PLAN-011 | NOT_A_BUG | Multiple eligible runtime providers can be semantically ambiguous. | Existing planner already fails closed with `UTL2302` and requires `UseRuntimeProvider`; repair tests preserve that policy. | No implicit provider ranking is introduced. |
| UT-PLAN-012 | DOCUMENTED_LIMITATION | Planning uses descriptors before runtime component instances are materialized. | No production change: exact package/manifest and runtime binding checks are a deliberate later boundary; planner cannot inspect arbitrary runtime behavior safely. | Descriptor/runtime dishonesty is a trust/supply-chain concern, not solved by route optimization. |
| UT-PLAN-013 | NOT_A_BUG | Backend-specific applicability could be confused with global contribution availability. | Existing `SupportedBackends` filtering is explicit and retained; repair route feasibility is evaluated per backend. | Authors remain responsible for correct backend scoping declarations. |
| UT-PLAN-014 | NOT_A_BUG | Stable IDs/canonical sorting were suspected of leaking into semantics. | Repair removes IDs from semantic tie-breaking while preserving canonicalization. Registration/input permutation tests show stable plan/hash behavior where semantics are unchanged. | Canonical representation necessarily contains stable identities. |
| UT-PLAN-015 | TEST_GAP_CLOSED | Original suite under-tested counterfactual and metamorphic planner invariants. | `6fc1bb43afcf480b9985846061778aaf9750489b`, `9171bbb3a38cf862acf107b58796e623992cc934`, and `a8244784cb3383f13b13fca74e38b66e9eb870b8` add dominated/unreachable alternatives, reverse ordering, observable runtime effects, contradiction, cost-boundary, permutation and irrelevant-input controls. | Broader fuzz/property exploration remains useful future work. |
| UT-PLAN-016 | POLICY_BOUNDARY_CONFIRMED | A reachable provider could tempt the planner to resolve provider ambiguity through route reachability, creating a hidden preference policy. | Added control proves provider ambiguity remains fail-closed and must be resolved explicitly; route reachability is not a runtime-provider selector. | A future provider-preference policy would require a public explicit contract and new tests. |
| UT-PLAN-017 | TEST_GAP_CLOSED | Planner invariance under dominated and unreachable conversion alternatives was insufficiently exercised. | Metamorphic controls added in `6fc1bb4…` and `a824478…` confirm irrelevant/dominated alternatives do not change an otherwise unambiguous plan. | Does not replace systematic graph-generation fuzzing. |
| UT-PLAN-018 | FIXED | Definition-level contribution order affected resolved `Contributions` but executable pass scheduling used an independent partial order, so route/runtime/PlanHash semantics could diverge. | `3dc56311926cd3522e5aa883e6896321444cd52d` merges definition-level ordering into executable scheduling and verifier checks; `UTL2205` reports impossible topology. Tests cover both directions, composed descriptor+definition constraints, contradiction, runtime observable result, PlanHash and canonical lock divergence. | None known for the audited ordering contract. |

## RED → GREEN evidence

The frozen test-only repair baseline `e554006985b5db551e810dbc494f3a6ba12a6cae` produced 174 LanguageSdk tests with 168 passing and 6 failing. The failures were the expected counterexamples for aggregate-cost overflow, globally feasible mandatory-pass routing, equal-cost route ambiguity, impossible cross-contract descriptor order, equal-order pass ambiguity, and definition-order/executable-route divergence.

After the production repair, `92ba1b3009d6386043c913bc13d3c9c05dc704b7` produced 176/176 passing LanguageSdk tests; the workflow remained red only because the exact-count manifest still intentionally expected 167. After all required regression/metamorphic controls were added, Linux and Windows both independently observed 184/184 passing LanguageSdk tests before `eng/test-counts.json` was reconciled. The final observed manifest is 1,323 passing tests across 15 canonical entries.

## Canonical CI boundary

The untouched baseline `7005371…` already had a non-test documentation-status failure: `docs/talks: internal material must live under internal-docs/`. Its canonical test contract was 1,306/1,306 green and hardening mutants were green before that pre-existing docs-status rule failed.

The planner repair does not modify `docs/talks`, the documentation-status checker, or workflow definitions. Therefore that failure is recorded as pre-existing repository hygiene debt, not a planner regression and not part of this repair scope. Census-derived verification documentation is updated to the newly observed 1,323-test manifest.

## Verification policy

No benchmark/performance result is inferred from correctness tests. The route-search state is richer because it tracks mandatory-pass coverage, so planner performance and state-space behavior remain `NEEDS_MEASUREMENT` before any performance claim.

No merge, release, package publication, or tag is performed by this repair branch.
