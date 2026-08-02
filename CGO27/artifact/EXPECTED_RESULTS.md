# Expected quick-check results

- Boundary primary: P0 12/32, P1 28/32, P2 32/32, P3 32/32.
- Boundary challenge: P0 1/10, P1/P2/P3 10/10.
- Boundary valid controls rejected: 0/100 for every policy.
- Wist source-to-result: 30 cases, 240 raw records, five targeted faults, 25 valid controls, no baseline runtime failure, P2/P3 parity 30/30.
- TensorRules: two valid, two invalid, eight fault cases, 48 observations, P2/P3 parity 12/12.
- Ablation: P2 control verifier-call reduction 14.3%, below the 25% efficiency-headline threshold.
