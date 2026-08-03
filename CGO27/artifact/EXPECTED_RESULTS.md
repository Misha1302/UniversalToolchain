# Expected quick-check results

- Boundary primary: P0 12/32, P1 28/32, P1D 28/32, P2 32/32, P3 32/32.
- Boundary challenge: P0 1/10, P1/P1D/P2/P3 10/10.
- Boundary demand pair: P0/P1 0/2, P1D 1/2, P2/P3 2/2.
- Boundary valid controls rejected: 0/120 for every policy.
- Wist source-to-result: 32 cases and 320 raw records; the historical 30-case denominator is preserved and two demand-v3 cases are reported separately. Seven targeted faults, 25 valid controls, P07 accepted by all policies, P2/P3 parity 32/32.
- TensorRules: 14 cases, 10 fault cases, 70 observations, P2/P3 parity 14/14. P1D rejects the queried demand case and not the unqueried case; P2/P3 reject both.
- Mechanism ablation: full protocol 8/8, single-mechanism ablations 0/8, matched-control false positives 0/8.
- Verifier work: P2 120 calls versus P3 160 on 120 controls, a 25% reduction. This is isolated work-count evidence, not whole-compilation timing and not an efficiency headline.
