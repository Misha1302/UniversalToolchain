# CGO 2027 claim/evidence ledger

Every paper claim maps to a canonical evidence artifact and a bounded interpretation.

| ID | Candidate claim | Status | Evidence owner | Allowed wording |
|---|---|---|---|---|
| C1 | Structurally valid IR can still be invalid for the selected compiler system. | supported | Wist historical source-to-result faults | “In the evaluated Wist faults, structural-only and invalidation-only policies allowed a wrong result or later failure that semantic reverification rejected earlier.” |
| C2 | Selective reverification matches unconditional reverification on evaluated faults. | supported, corpus-bounded | Wist boundary 42 historical shapes plus 2 demand cases; Wist E2E 32 cases; TensorRules 14 cases | “P2 and P3 had classification parity on the evaluated corpora.” |
| C3 | Typed invalidation obligations identify which semantic verifier should run. | supported | production scheduler tests, boundary telemetry, Wist optimized-AIR faults | “The implementation routes declared invalidations to deterministic verifier owners and fails closed on unknown/conflicting routes.” |
| C4 | Demand-driven recomputation is weaker than first-eligible-boundary discharge. | supported, matched-case bound | Wist and TensorRules queried/unqueried demand pairs | “P1D rejected only the queried member of each pair; P2 rejected both at their first eligible boundary.” |
| C5 | The mechanism applies beyond one language package. | supported, two-package bound | Wist and TensorRules | “The same policy distinction was evaluated in Wist and a public-SDK TensorRules package.” |
| C6 | Selective reverification performs less verification work than always verify. | partially supported | 120 boundary controls; TensorRules clean-boundary check | “P2 performed 120 verifier calls versus P3’s 160 on the evaluated controls.” Do not state whole-compilation speedup. |
| C7 | Selective reverification reduces whole-compilation time. | blocked | pinned-machine benchmark absent | No efficiency headline or speedup percentage. |
| C8 | Results generalize to independently authored faults. | blocked external | external blind corpus absent | State author-designed/model-authored limitation explicitly. |
| C9 | P2 and P3 are generally equivalent. | forbidden | finite corpora cannot prove general equivalence | Use only evaluated-corpus parity wording. |
| C10 | Historical denominators include demand cases. | forbidden | historical v2/v1 inputs immutable | Report demand-v3/v2 cases separately. |

## Current evidence identities

- Current exact-head input evidence used for the repaired ablation analyzer: `372f03e41495d6454ac989e1ed9796fd5a854fd9`.
- Final provider run IDs, artifact IDs and digests must all refer to the same post-repair head before insertion here.

## Evidence priority

1. raw JSONL and checksum manifests;
2. provider workflow receipt and exact commit;
3. generated summaries;
4. prose tables and paper text.

When prose conflicts with raw evidence, raw evidence wins and prose must be regenerated.
