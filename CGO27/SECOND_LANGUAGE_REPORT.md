# CGO 2027 second-language selection report

Status: `IMPLEMENTED_PENDING_PROVIDER`.

## Decision

Select **TensorRules** as the second language package. It is implemented only through the public UniversalToolchain language-authoring SDK and contains no Wist or compiler-internal project references.

## Alternatives

| Candidate | Distinct semantic invariants | Dissimilarity from Wist | Fault quality | Public-SDK-only feasibility | Decision |
|---|---|---|---|---|---|
| Acme Pricing sample | arithmetic expression shape and interpreter/compiled parity | moderate | low: few independently meaningful fact invalidations | already demonstrated | rejected as too small for the research question |
| TensorRules | matrix inner extents, output shape, layout, backend capability and fact ownership | high | high: eight typed faults with wrong-result versus early-rejection behavior | direct typed artifact route | selected |
| Wist-derived second preset | mostly identical module and AIR invariants | low | high but not externally valid | feasible | rejected because it would not be a second language |

## Implemented route

`source -> TensorSyntax -> shape/type verified TensorPlan -> policy-controlled optimizer pass -> tensor interpreter`

The study freezes 12 cases:

- 2 valid examples;
- 2 intrinsically invalid examples;
- 8 fault operators;
- 4 policies, producing 48 observations.

The acceptance gate requires P2/P3 classification parity on all 12 cases and separately requires P3 to invoke semantic verification on clean valid boundaries while P2 does not.

## Claim boundary

TensorRules is model-authored. It must be described as a **second language package**, not an independently authored language. Provider-backed status is assigned only after the dedicated workflow builds the project with warnings as errors, runs the study and verifies its checksum manifest.
