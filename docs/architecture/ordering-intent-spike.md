---
title: Ordering intent spike
navigation: hidden
status: Internal maintainer evidence — spike verdict for UT-A055, not an implemented feature.
---

# Ordering intent spike (UT-A055)

**Verdict: GO for the production PR, with one scope decision that must be made first.** A cycle-free
result is achievable and cheaply so, but only because each contribution carries **exactly one** intent.
Relaxing that — which a real author will want — reintroduces cycles immediately. That trade-off is the
actual decision, and it is now measurable instead of arguable.

Nothing in this spike is referenced by the planner. `Tests/Spikes/OrderingIntent` is a fixture; no
production type was added, and no plan shape changed.

## The problem being tested

`08` A-1/A-3, executed: N independent optimizer packages at default `Order = 0` fail with `UTL2208` at
N ≥ 2. Distinct invented integer orders pass. Symmetric pairwise `Before(...)` produces `UTL2202`. Both
surviving strategies need **global** knowledge — integers are O(n) of it, edges are O(n²). A package
author therefore has to know about packages they do not own.

## What was built

A resolver over a closed union — `Explicit(int)`, `SemanticAnchor(id, Before|After)`,
`PhaseRelative(phase, First|Last)`, `Unordered` — producing a total order plus a per-step record of
*why* each step is where it is, and which declared policy broke each tie.

## Results, all measured

| Claim | Result |
|---|---|
| N independent passes at zero explicit orders compose | **resolved for N = 1..8** |
| Equal explicit orders stay an error | **`ExplicitConflict`**, as `UTL2208` does today |
| Two members declaring the *same* position agree | **resolved** — no invented edge between them |
| A new unrelated package does not reorder existing ones | **holds** |
| Resolution independent of input enumeration order | **holds over all 24 permutations of N = 4** |
| Cycle reachable under one-intent-per-contribution | **not reached in 11,110 enumerated shapes** |

The cycle number is the interesting one. It is **not** proof that the graph is acyclic by accident: it
follows from the edge rule. Each mechanism only creates an edge *from its own members to members that
declare nothing in that mechanism*, and explicit edges exist entirely inside the explicit set. So no
mechanism can demand the reverse of another, and a cycle cannot form. Confirmed structurally, and then
confirmed by enumeration.

## The finding that changes the plan

The same structural property that removes cycles also removes expressiveness:

> a contribution cannot declare **both** "anchor-After on `constant-folding`" **and** "last in the
> optimizers phase", because it carries one intent.

I tested the obvious fix — let a contribution carry a **set** of intents — in a separate model:

- it does express the combined shape, and produces the expected order;
- it also makes cycles **reachable**: 13,342 cyclic shapes in a small enumeration, including a genuine
  two-contribution cycle where one member is `anchor-Before` and another is `phase-Last`, each requiring
  the other.

So the choice for the production PR is not "which syntax is nicer". It is:

| Option | Cycles | Expressiveness |
|---|---|---|
| one intent per contribution | structurally impossible | combined intents inexpressible |
| set of intents | reachable, needs detection + a `UTL` diagnostic | combined intents expressible |

The one-intent model is the cheaper and safer start, and it still solves the reported problem (N
independent passes compose with no global knowledge — the actual A-1 failure). The set model is a
follow-up with its own cycle diagnostic. This is worth stating before anyone starts coding, because the
temptation will be to add the second intent "just for phases" and silently lose the acyclicity guarantee.

## What this spike deliberately did not do

- No production type, no planner change, no `LanguageContributionDescriptor` edit. Wiring the union into
  the planner is the production PR, and it needs the tie-break policy recorded in `PlanHash` to satisfy
  the determinism gate from PR-01.
- No migration of the 21 Wist module descriptors. They run on `Order` today and stay that way.
- The `Cycle` branch is kept in the resolver and is **not** covered by a reachable case under one intent.
  It is documented as a guard, not as a tested path — a test that cannot reach its branch is worse than
  no test, because it claims coverage. If the production PR adopts sets, that branch becomes load-bearing
  and must get a real case.

## Evidence

`UniversalToolchain/Tests/Spikes/OrderingIntent/` — 14 tests, all green, `Core 514 → 528`.
Mutation-checked while building: inverting edge direction and ordering same-position members against
each other both made real tests fail before being fixed, which is why the two rules above are stated as
measured rather than assumed.
