# CGO 2027 second-language selection report

Status: `PROVIDER_BACKED_VALIDATED`.

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

The provider-backed acceptance gate confirms:

- build with warnings as errors;
- P2/P3 classification parity on all 12 cases;
- P3 semantic verification on clean valid boundaries while P2 omits unrequested verification;
- public-SDK dependency boundary;
- recursive checksum manifest.

## Provider receipt

- exact commit: `acc60612361f240d5bd24f148ea7fa6eb5e1f111`;
- workflow: `CGO27 TensorRules`, run `30661725387`;
- artifact ID: `8805405891`;
- artifact digest: `sha256:07fb7e7e9da11f8875a2bb58b291a01903756de2ccd9af85dc5117adc89dc404`;
- artifact summary: `VALIDATED`, 2 valid, 2 invalid, 8 faults, 48 observations, P2/P3 parity 12/12, Wist references 0.

## Claim boundary

TensorRules is model-authored. It must be described as a **second language package**, not an independently authored language.
