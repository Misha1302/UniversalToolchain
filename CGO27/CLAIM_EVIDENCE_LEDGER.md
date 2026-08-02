# CGO 2027 claim/evidence ledger

Every paper claim must map to a canonical evidence artifact and a bounded interpretation.

| ID | Candidate claim | Status | Evidence owner | Allowed wording |
|---|---|---|---|---|
| C1 | Structurally valid IR can still be invalid for the selected compiler system. | supported | Wist source-to-result targeted faults | “In the evaluated Wist faults, structural-only and invalidation-only policies allowed a wrong result or later failure that semantic reverification rejected earlier.” |
| C2 | Selective reverification matches unconditional reverification on evaluated faults. | supported, corpus-bounded | Wist boundary 42 shapes; Wist e2e 30 cases; TensorRules 12 cases | “P2 and P3 had classification parity on the evaluated corpora.” |
| C3 | Typed invalidation obligations identify which semantic verifier should run. | supported | production scheduler tests, boundary telemetry, Wist optimized-AIR faults | “The implementation routes declared invalidations to deterministic verifier owners and fails closed on unknown/conflicting routes.” |
| C4 | The mechanism applies beyond one language package. | supported, two-package bound | Wist and TensorRules | “The same policy distinction was evaluated in Wist and a public-SDK TensorRules package.” |
| C5 | Selective reverification performs less verification work than always verify. | partially supported | per-rule invocation telemetry; TensorRules clean-boundary check | “P2 omitted unrequested verifier calls in evaluated clean boundaries.” Do not state whole-compilation speedup. |
| C6 | Selective reverification reduces whole-compilation time. | blocked | pinned-machine benchmark absent | No efficiency headline or percentage claim. |
| C7 | Results generalize to independently authored faults. | blocked external | external blind corpus absent | State author-designed/model-authored limitation explicitly. |
| C8 | P2 and P3 are generally equivalent. | forbidden | finite corpora cannot prove general equivalence | Use only evaluated-corpus parity wording. |
| C9 | Existing historical denominators were improved by renaming policies. | forbidden | historical v2 evidence immutable | Report historical and v3 studies separately. |

## Provider receipts

- Wist source-to-result: commit `acc60612361f240d5bd24f148ea7fa6eb5e1f111`, workflow `30661725052`, artifact `8805491648`, digest `sha256:f3f34c33e2595f95d061fddc9fae213818024831151334d73aa12db11ff3754b`.
- TensorRules: commit `acc60612361f240d5bd24f148ea7fa6eb5e1f111`, workflow `30661725387`, artifact `8805405891`, digest `sha256:07fb7e7e9da11f8875a2bb58b291a01903756de2ccd9af85dc5117adc89dc404`.

## Evidence priority

1. raw JSONL and checksum manifests;
2. provider workflow receipt and exact commit;
3. generated summaries;
4. prose tables and paper text.

When a prose table conflicts with raw evidence, raw evidence wins and the prose must be regenerated.
