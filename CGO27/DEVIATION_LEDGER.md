# CGO 2027 deviation ledger

## D1 — External blind corpus

- Planned: externally authored, frozen-before-execution corpus.
- Actual: deterministic author/freeze/import kit implemented; no external author corpus supplied.
- Classification: `BLOCKED_EXTERNAL`.
- Consequence: no external-validity or independent-author claim.

## D2 — Performance environment

- Planned: pinned CPU/RAM/kernel/.NET/governor/affinity environment with raw distributions.
- Actual: hosted CI and verifier-kernel smoke only; pinned-machine capture gate prepared.
- Classification: `BLOCKED_PINNED_MACHINE`.
- Consequence: no whole-compilation speedup or efficiency headline.

## D3 — Wist baseline runtime failure (resolved)

- Planned: all non-fault source cases accepted by every policy.
- Previous actual: case `P07` failed identically under all policies because NativeMath required identical operand CLR types.
- Resolution: deterministic binary numeric promotion now selects a common operand type; `P07` must execute as a valid control under all policies.
- Treatment: separate `baseline-runtime-failure`; excluded from targeted faults and valid-control counts; retained in raw evidence.

## D4 — Second-language authorship

- Planned: independent second language if available.
- Actual: TensorRules is model-authored through public SDK only and provider-verified.
- Treatment: label “second language package”; do not call it independently authored.

## D5 — CGO 2027 format

- Earlier planning text required rechecking official rules.
- Current official rule recorded for the paper workspace: ACM SIGPLAN format, Letter paper, 11 pages of text excluding references, second-round deadline 10 September 2026 AoE.
- Consequence: paper build must target the current 11-page rule, not an inherited page limit.
