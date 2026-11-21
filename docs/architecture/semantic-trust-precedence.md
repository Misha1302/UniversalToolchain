---
title: Semantic core — trust precedence is one rule
navigation: hidden
status: Internal maintainer architecture evidence for the semantic-core trust rule (UT-A026).
description: Why functional-predicate disagreement and positive/negative disagreement must be resolved and reported identically.
---

# Semantic core — trust precedence is one rule

Status: spike finding (UT-A026). Applies to `UniversalToolchain.Semantics.Core`.

## The rule

`SemanticTrust` exists so a consumer can prefer a stronger assertion over a weaker one. That is **one**
decision, and it must behave the same way however the disagreement is expressed:

- a positive assertion versus a negative assertion for the same proposition, or
- two different values for the same key of a functional predicate.

Two things are required of it in either shape:

1. **The stronger assertion wins.** The weaker one does not silently outvote it.
2. **The precedence is reported.** A consumer that receives a confident answer must be able to learn that a
   different provider asserted something else about the same subject. Silence turns a cross-provider
   configuration conflict into an invisible fact.

## The defect that motivated this note

The positive/negative path implemented both halves: it returned the winning judgment and emitted
`UTL2407` (`SemanticDiagnosticCodes.TrustPrecedence`).

The functional path implemented only the first half, and did it inconsistently:

| Knowledge base | Query | Result before |
| --- | --- | --- |
| `Range(v,3,9)` @ Verified, `Range(v,100,200)` @ Unchecked | `Range(v,3,9)` | `Proved`, **no diagnostic** |
| same | `Range(v,100,200)` | `Proved`, **no diagnostic** |

The second row is the serious one. `Range` is a *functional* predicate: one variable has one range. The
verified provider says the range is `[3,9]`; the unchecked provider says `[100,200]`. Asking about
`[100,200]` returned `Proved`, which tells a consumer that `[100,200]` holds while the stronger provider
explicitly says it does not. That is precisely the unsound-but-confident answer the four-valued judgment
exists to prevent — `Proved` is not "probably", it is "established".

## The fix

`TryResolveFunctional` applies one rule to both shapes:

| Situation | Judgment | Diagnostic |
| --- | --- | --- |
| two different values at the **highest** trust | `Inconsistent`, no proof | `UTL2403` |
| lower-trust value(s) lose to a unique highest-trust value | `Proved` for the winner | `UTL2407`, naming the losing values |
| the query is about a **losing** value | `Unknown`, no proof | `UTL2407` |
| no disagreement for this key | unchanged | none |

`Unknown` for a losing value is deliberate. It is not `Disproved` — the core has no negative proof for it —
and it is certainly not `Proved`. A consumer that acts on an answer will refuse, which is the correct
outcome: the knowledge base does not agree with itself about this value.

## What this note does not claim

- **No general fixpoint interaction is claimed.** The resolver runs over the computed closure, so it sees
  derived facts as well as declared ones; that is the intended reading, but it is not a proof of soundness
  for arbitrary rule sets.
- **Trust precedence is not required to be transitive across more than two levels in a specific order.** The
  comparison is by `SemanticTrust` ordinal; the current rule is "highest level wins, ties are contradictory".
- **`losing.Length == 0` in `TryResolveFunctional` is unreachable** given the closure's
  one-witness-per-proposition invariant. It is documented as a guard, not covered by a test, and a mutant
  injected there cannot be killed — see the comment at the site. `Query_Closure_KeepsOneWitnessPerProposition`
  pins the invariant that keeps it dead.
