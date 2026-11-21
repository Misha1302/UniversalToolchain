---
title: Semantic core spike — verdict (PR-14)
navigation: hidden
status: Internal decision record for the UT-A026 5-day spike. Decides whether semantic core v1 proceeds.
description: The measured evidence behind the go/no-go decision for UniversalToolchain.Semantics.Core.
---

# Semantic core spike — verdict

Scope: the UT-A026 five-day disposable spike, shipped as `UniversalToolchain.Semantics.Core`. This record
answers the abort criterion the plan set and nothing more. It is not a release note and does not claim the
spike is a product feature.

## Verdict

**GO, with one narrowing.**

`Proved` and `Inconsistent` are distinguishable, the five required scenarios are executable tests, and the
one unsoundness found during review is fixed. Proceed to v1 (PR-15).

The narrowing: v1 must not inherit the spike's **single-scope, ground-fact** model as if it were the target
design. The spike answers over facts declared in one epoch at one scope; that is enough to prove the
mechanism and not enough to claim cross-module cooperation. `UT-A026` §4 already scopes v1 that way; this
record makes the boundary explicit because the spike's green tests could otherwise be read as more.

## The abort criterion, measured

The plan's gate was: *abort and redesign if `Proved` and `Inconsistent` cannot be distinguished.*

| Scenario (spec §5) | Required | Measured |
| --- | --- | --- |
| 1. Independent provider; separate consumer asks | `Proved` | `Proved` |
| 2. Nobody published the fact | `Unknown`, naming the predicate | `Unknown`, predicate named |
| 3. Two providers, contradictory ranges | `Inconsistent` | `Inconsistent` |
| 4. Fact from epoch N, query at epoch N+1 | `Unknown` | `Proved` → `Unknown` |
| 5. Rule chain deeper than the bound | `Unknown` + diagnostic | `Unknown` + `UTL2404` |

Distinguishability holds. `Inconsistent` is never collapsed into `Unknown`, and `SemanticAnswer.IsHardRefusal`
exists precisely so a consumer can tell "refuse" from "skip".

## Definition of Done, item by item

| DoD item | Status | Evidence |
| --- | --- | --- |
| four-state judgment + one predicate wired to a fixture optimizer | done | `DevirtualizationOptimizerTests` |
| the 5 scenarios are executable tests | done | `SemanticCoreQueryTests` |
| negative case (`Unknown`) end-to-end | done | `Query_MissingFact_IsUnknown_NotDisproved` |
| no reflection | done | no `System.Reflection` reference in the project |
| deterministic DAG | done | `Query_Fixpoint_DAGIdenticalUnderShuffledAxioms` |
| depth bound | done | `Query_DepthBound_ReturnsUnknown` |
| documented "what this is not" | done | `docs/CURRENT_ARCHITECTURE_STATUS.md` |

The fixture optimizer receives only `ISemanticQuery` and its project references the core alone, so "the
consumer does not import the provider's package" is a project-file fact, not a comment.

## The defect found while doing this review

A trust-precedence asymmetry made the core return an unsound `Proved`. It is fixed in the same PR series and
recorded in `semantic-trust-precedence.md`:

```
Range(v,3,9) @ Verified + Range(v,100,200) @ Unchecked
  ask Range(v,100,200) -> Proved          (before)
                       -> Unknown + UTL2407 (after)
```

This is the failure mode the plan named as the primary risk ("unsound `Proved`"), found by attacking the
spike rather than by reading it. It is one reason to proceed: the mechanism makes the defect expressible and
fixable in one place. It is also a warning for v1 — every new judgment path needs the same red-team treatment,
because the green suite did not catch it.

## What the spike does not establish

- **Not a proof of soundness.** The rules are monotone and ground; the fixpoint is bounded. Nothing is proved.
- **Not cross-module cooperation yet.** One scope, one epoch, declared facts. Provider isolation is
  demonstrated by a project file, not by hostile-package testing.
- **Not a planner.** `Core_ExposesNoSelectionSurface` fails if any exported member is named like a selection
  API. The single-planner law is a test here, not a review convention.
- **No negation-as-failure.** Absence is `Unknown`. `Disproved` requires an explicit negative assertion.
- **No aggregation, no recursion across scopes, no SMT, no timing or alias reasoning.**

## Consequence for the backlog

`docs/CURRENT_ARCHITECTURE_STATUS.md` says the spike is deleted if no production consumer appears within one
milestone. That clock is not expired (the spike landed 2026-11-20), so the choice is not "delete" yet. This
verdict starts the clock explicitly: **PR-15 must produce a production consumer, or the spike is removed** at
the next milestone boundary. Keeping a fixed-up spike alive without a consumer was the outcome the deletion
rule existed to prevent, and this record should not be used to relax it.
