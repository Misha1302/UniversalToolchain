---
title: Planner routing policy decision — 2026-09-03
navigation: hidden
status: Branch-scoped audit policy decision; not part of published user navigation.
---

# Planner routing policy decision — 2026-09-03

Baseline: `master@7005371d6c30175dff4b0e9f906a26218b0ee54d`

Working branch: `repair/planner-iterative-20260903`

Status: accepted repair contract for the iterative planner hardening task.

## Decision

Adopt a constrained form of **Model C: feasibility first, explicit preference second, fail closed on unresolved semantic ties**.

The planner contract is:

1. selected same-contract passes are mandatory executable contributions for every backend in their declared backend scope;
2. selected non-pass transformations are route candidates, not an implicit requirement that every selected conversion execute;
3. artifact-contract reachability, selected-pass placement, backend scope, descriptor `Before`/`After`, and definition `Before`/`After`/`Requires` are feasibility constraints;
4. definition `Requires(source, target)` keeps the already implemented contribution-resolution meaning: `target` precedes `source` when both are executable in the same ordered domain;
5. among fully feasible conversion routes, minimize the mathematical sum of declared conversion costs using widened aggregate arithmetic;
6. if two semantically distinct fully feasible routes have the same minimum cost and no explicit policy distinguishes them, planning fails closed rather than using `ContributionId` as semantic priority;
7. same-contract pass scheduling uses the combined explicit partial order first and `Contribution.Order` as the declared priority among ready passes;
8. if multiple unrelated ready passes have the same minimum `Order`, planning fails closed rather than using `ContributionId` as execution priority;
9. `ContributionId` remains valid for stable identity, canonical serialization, diagnostics and deterministic presentation, but not for resolving an otherwise semantic execution ambiguity.

No new route-preference or equivalence API is introduced in this repair. If real packages need equal-cost semantic alternatives, that is the trigger for an explicit policy surface rather than another technical tie-break.

## Models considered

| Model | Correctness / explainability | Compatibility | Complexity | Verdict |
|---|---|---|---|---|
| A. Current technical tie-break: feasible → min cost / `Order` → `ContributionId` | Reproducible but silently turns names into semantics; conflicts with fail-closed provider policy | Highest | Lowest | Rejected |
| B. Fail on every unresolved alternative | Safe for ties but does not solve the globally-valid-route problem when mandatory passes require a non-locally-cheapest path | Moderate | Low | Insufficient alone |
| C. Feasibility → explicit preference → fail-closed unresolved tie | Keeps semantic constraints authoritative, preserves cost/`Order` as declared preferences, removes ID semantics | Moderate; may expose previously silent ambiguity | Moderate | Chosen |

## Why this follows existing authority boundaries

The public API already distinguishes explicit semantic policy from stable identity:

- capability ambiguity requires `PreferCapabilityProvider`;
- runtime-provider ambiguity fails closed unless explicitly selected;
- single-owner slot ambiguity fails closed unless explicitly replaced;
- definition-level `Before`/`After`/`Requires` is a public semantic surface;
- pass `Order` is already declared contribution metadata;
- artifact transformation `Cost` is already declared route preference metadata.

Therefore lexical `ContributionId` is weaker evidence than those explicit policies and must not override or complete them semantically.

## Global route feasibility

The old algorithm fixed a minimum-cost conversion skeleton and inserted mandatory passes afterwards. That can reject a language even when a more expensive conversion skeleton contains all mandatory pass contracts.

The repair search state is therefore conceptually:

`(current artifact contract, mandatory passes whose contract has been visited)`.

A target is acceptable only when the backend target is reached and every selected backend-applicable pass has a placement contract on the route. Cost remains the optimization criterion among these feasible states.

Ordering is validated after pass insertion and remains a hard feasibility condition. If future evidence shows that alternative conversion topology must also be searched to satisfy cross-contract ordering, extend the same feasibility state rather than reintroducing a post-hoc technical tie-break.

## Ambiguity diagnostics

This repair reserves planner diagnostics for two newly explicit ambiguity classes:

- equal-cost fully feasible conversion routes with distinct executable contribution sequences;
- equal-`Order`, unrelated ready passes after all explicit partial-order constraints are applied.

Exact diagnostic codes are implementation details of this branch; the semantic rule is the stable part.

## Performance boundary

No performance claim is made. Augmenting route-search state by mandatory-pass coverage can increase planning work as the number of selected passes grows. Current task priority is correctness. Any optimization, caching or alternative solver requires measured evidence from the existing benchmark infrastructure before being claimed or adopted.

## Finding disposition enabled by this decision

- `UT-PLAN-001` / `UT-PLAN-002`: resolve by fail-closed equal-cost route ambiguity.
- `UT-PLAN-003`: resolve by making mandatory-pass placement part of route feasibility.
- `UT-PLAN-007`: resolve by fail-closed equal-`Order` unrelated pass ambiguity.
- `UT-PLAN-010`: reclassify the public contract explicitly: selected non-pass transformations are candidates; selected passes are mandatory within backend scope.
- `UT-PLAN-016`: provider ambiguity remains an earlier explicit-policy boundary; downstream route reachability does not silently choose a provider. This is consistent policy, not a planner bug without a stronger public contract.
