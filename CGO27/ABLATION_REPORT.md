# CGO 2027 ablation report

Status: `VALIDATED_INPUTS_PENDING_FINAL_EXACT_HEAD_PROVIDER_RECEIPT`.

## Mechanism isolation

The full protocol detects all eight predeclared counterexamples. Removing each mechanism independently loses its matching detection, while the eight matched controls remain accepted by both the full and ablated validators:

- full protocol: 8/8;
- corresponding single-mechanism ablation: 0/8;
- ablated-control false positives: 0/8.

These are necessity witnesses for the implemented checks, not prevalence estimates.

## Policy ablation

| Variant | Primary | Challenge | Historical Wist | Historical Tensor | Demand Wist/Tensor |
|---|---:|---:|---:|---:|---:|
| P0 no typed contracts | 12/32 | 1/10 | 0/5 | 0/8 | 0/2, 0/2 |
| P1 invalidation without discharge | 28/32 | 10/10 | 0/5 | 0/8 | 0/2, 0/2 |
| P1D demand-only discharge | 28/32 | 10/10 | 0/5 | 0/8 | 1/2, 1/2 |
| P2 first-eligible-boundary discharge | 32/32 | 10/10 | 5/5 | 8/8 | 2/2, 2/2 |

P2 and P3 retain classification parity on 42 historical boundary shapes plus the two demand cases, 32 Wist source cases, and 14 TensorRules cases.

## Verification work

On 120 boundary controls, P2 executes 120 verifier calls and P3 executes 160, a 25% reduction. The isolated threshold is met, but this is not pinned-machine or whole-compilation timing. The efficiency headline therefore remains forbidden and whole-compilation performance remains `BLOCKED_PINNED_MACHINE`.

## Evidence identity

The repaired schema-v3 analyzer was validated locally against the exact-head ablation artifact for `372f03e41495d6454ac989e1ed9796fd5a854fd9`. Final workflow run, artifact ID and digest will be recorded only after all repaired workflows succeed on one post-repair head.
