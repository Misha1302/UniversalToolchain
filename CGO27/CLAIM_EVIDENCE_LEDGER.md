# CGO 2027 claim/evidence ledger

Every paper claim maps to a canonical evidence artifact and a bounded interpretation.

| ID | Candidate claim | Status | Evidence owner | Allowed wording |
|---|---|---|---|---|
| C1 | Structurally valid IR can still be invalid for the selected compiler system. | supported | System W source-to-result faults | “In the evaluated System W faults, structural-only and invalidation-only policies allowed a wrong result or later failure that semantic reverification rejected earlier.” |
| C2 | Selective reverification matches unconditional reverification on evaluated faults. | supported, corpus-bounded | boundary 42 historical shapes plus 2 demand cases; System W E2E 32 cases; System T 14 cases | “P2 and P3 had classification and first-boundary parity on the evaluated corpora.” |
| C3 | Typed invalidation obligations identify which semantic verifier should run and can persist to a later executable boundary. | supported, mechanism-bound | production orchestration/scheduler tests, deferred backend-input case | “The production observer carried a declared backend-input obligation from optimized AIR to the pre-backend boundary and discharged it through its canonical owner.” |
| C4 | Demand-driven recomputation is weaker than first-eligible-boundary discharge. | supported, matched-case bound | System W and System T queried/unqueried demand pairs | “P1D rejected only the queried member of each pair; P2 rejected both at their first eligible boundary.” |
| C5 | The mechanism applies beyond one language package. | supported, two-package bound | System W and System T | “The same policy distinction was evaluated in System W and a public-SDK System T package.” Do not call System T independent. |
| C6 | Selective reverification performs less verification work than always verify. | partially supported | 120 boundary controls; System T clean-boundary check | “P2 performed 120 verifier calls versus P3’s 160 on the evaluated controls.” Do not state whole-compilation speedup. |
| C7 | Historical defects provide independent policy-validation evidence. | exact reproduction supported; policy rate and independence unsupported | frozen 24-candidate accounting, exact-prefix replay receipt, and issue/fix provenance | “All three frozen pre-study defects reproduced on the exact pre-fix revision in 9/9 fresh-process attempts.” Do not call the set independent or report a historical P2 rate. |
| C8 | Selective reverification reduces whole-compilation time. | blocked | pinned-machine benchmark absent | No efficiency headline or speedup percentage. |
| C9 | Results generalize to independently authored faults. | blocked external | external blind corpus absent | State author-designed/model-authored limitation explicitly. |
| C10 | P2 and P3 are generally equivalent. | forbidden | finite corpora cannot prove general equivalence | Use only evaluated-corpus parity wording. |
| C11 | Historical denominators include demand cases. | forbidden | historical inputs and checksums immutable | Report demand cases separately under the new schema/protocol version. |
| C12 | The scheduler proves declared effects or verifier correctness. | forbidden | these are explicit theorem assumptions | State sound seeds, truthful/complete effects, and verifier soundness as assumptions. |

## Evidence identities

The source revision cannot embed the provider artifact identities created after that revision without a recursive commit. The exact final head, workflow-run set, artifact IDs, artifact digests, local download hashes, and visual/content audit results are therefore recorded in two non-recursive locations:

1. the draft PR description for the branch-backed provider receipt;
2. the delivered final readiness report accompanying the PDF and anonymous supplement.

All identities in either receipt must refer to one exact branch head. A green workflow name without checkout identity, steps, logs, and artifact inspection is insufficient.

## Evidence priority

1. raw JSONL and checksum manifests;
2. exact-head provider workflow/artifact receipt;
3. generated summaries and deterministic tables;
4. prose tables and paper text.

When prose conflicts with raw evidence, raw evidence wins and prose must be regenerated. No receipt may upgrade `BLOCKED_EXTERNAL` or `BLOCKED_PINNED_MACHINE` without new evidence and a full claim audit. Exact-prefix reproduction does not by itself authorize a historical P2-rate claim.
