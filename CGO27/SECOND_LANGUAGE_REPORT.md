# CGO 2027 second-language selection report

Status: `VALIDATED_PENDING_FINAL_EXACT_HEAD_RECEIPT`.

## Decision

Select **TensorRules** as the second language package. It is implemented through the public UniversalToolchain language-authoring SDK and contains no Wist or compiler-internal project references.

## Alternatives

| Candidate | Distinct semantic invariants | Dissimilarity from Wist | Fault quality | Public-SDK-only feasibility | Decision |
|---|---|---|---|---|---|
| Acme Pricing sample | arithmetic expression shape and interpreter/compiled parity | moderate | low: few independently meaningful fact invalidations | already demonstrated | rejected as too small for the research question |
| TensorRules | matrix inner extents, output shape, layout, backend capability and fact ownership | high | high: eight historical typed faults plus a matched queried/unqueried demand pair | direct typed artifact route | selected |
| Wist-derived second preset | mostly identical module and AIR invariants | low | high but not externally valid | feasible | rejected because it would not be a second language |

## Implemented route

`source -> TensorSyntax -> shape/type verified TensorPlan -> policy-controlled optimizer pass -> tensor interpreter`

The versioned study contains 14 cases:

- 2 valid examples;
- 2 intrinsically invalid examples;
- 8 historical semantic fault operators;
- 2 matched demand cases;
- 5 policies, producing 70 observations.

The acceptance gate requires:

- build with warnings as errors;
- P2/P3 classification parity on all 14 cases;
- P1D rejection of the queried demand case but not the unqueried case;
- P2/P3 rejection of both demand cases;
- P3 semantic verification on clean valid boundaries while P2 omits unrequested verification;
- public-SDK dependency boundary;
- recursive checksum manifest.

## Current validated evidence

The exact-head input artifact for `372f03e41495d6454ac989e1ed9796fd5a854fd9` records `VALIDATED`, 2 valid, 2 invalid, 10 faults, 70 observations, P2/P3 parity 14/14, and zero Wist references. A final provider receipt will be inserted only after the repaired exact-head workflow completes.

## Claim boundary

TensorRules is model-authored. It is a **second language package**, not an independently authored language.
