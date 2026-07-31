# CGO 2027 completion workstate

Last updated: 2026-07-31.
Branch: `research/cgo27-selective-reverification`.
Draft PR: `#322` — CI/review boundary only; not merge-ready.
Repository baseline: `master` at `c73b418c6e72e8b92371753a3a7b4a9f7adaa5f1`.
Current branch head before this ledger update: `a25a041a11cfeaa0225e6d597f161018dbdce2a6`.
Code-equivalent verified baseline: `2b0a4d1f0e255432daf0d5ddd485269b6490b67e`; the four later master commits modify only three documentation files.

## Objective

Produce decision-grade, reproducible evidence for selective contract-guided reverification in an extensible compiler pipeline, with four explicit policies, telemetry, end-to-end cases, a genuinely independent second language, pinned performance evaluation, ablations, an anonymous CGO paper, and a clean artifact bundle.

## Authority boundary

Allowed: read repository; modify this branch; run build/tests/benchmarks; commit; open a PR; create anonymous paper and artifact bundles.
Not allowed without separate approval: direct push to `master`; merge; NuGet publication; conference submission; deletion or rewriting of historical evidence.

## Baseline observations

- Historical experiment implemented only `B0`, `B1`, and `B2`; it remains preserved as `Program.cs` plus `STUDY_PROTOCOL_V2.md`.
- Existing frozen primary corpus is 40 instances / 32 operator shapes; challenge set is 10 operators; valid controls are 100 per policy.
- Historical production-boundary result is B0/B1/B2 = 12/32, 28/32, 32/32 with zero false positives in 100 controls per mode.
- Existing performance evidence is an isolated verifier-kernel microbenchmark. It is not whole-compilation evidence and cannot support an end-to-end overhead claim.
- Existing challenge and review-holdout sets are author/reviewer designed; neither is an externally authored blind corpus.

## Current verified milestone

The active runner now exposes:

- `P0_STRUCTURAL`;
- `P1_INVALIDATION`;
- `P2_SELECTIVE` with production Bytecode/AIR verifier routing for requested obligations;
- `P3_ALWAYS` with unconditional applicable semantic verification on represented boundaries.

Local exact-package-boundary verification:

- build: 0 warnings, 0 errors;
- 1,000 schema-v3 raw records;
- primary: 12/32, 28/32, 32/32, 32/32;
- challenge: 1/10, 10/10, 10/10, 10/10;
- controls: 0/100 false positives for every policy;
- P2/P3 correctness parity on all 42 frozen operator shapes;
- malformed JSON evidence rejected;
- frozen mutation catalog byte-identical, SHA-256 `e830125293770b512e540a4ae3a003c407258916aea2d7f65d95b08cdadbb183`;
- every active GitHub source blob matched the locally tested Git blob SHA.

GitHub PR-head workflows started for `.NET CI`, `UniversalToolchain validation`, and `Contract Experiment`. Their results are not yet recorded as authority in this ledger.

## Completion ledger

| Result | Dependency | Observable done condition | Validation | Status |
|---|---|---|---|---|
| R1 Four policies | current runner, production policy semantics | one runner exposes P0/P1/P2/P3 with mechanically distinct behavior | focused policy invariants + all-corpus execution | VERIFIED_LOCAL; CI_RUNNING |
| R2 Telemetry | R1 | per-boundary verifier calls, invalidations, reverifications, timings, allocations and peak memory emitted in raw schema | schema validation + deterministic replay + malformed-input rejection | VERIFIED_LOCAL; CI_RUNNING |
| R3 Reference model/oracles | R1 | expected detection, diagnostic family, boundary and infra/flaky classifications encoded outside execution shortcuts | oracle validator + negative mutants | PARTIAL: raw oracle fields and validators exist; separate immutable oracle corpus pending |
| R4 Historical preservation | baseline manifests | existing denominators and source hashes remain unchanged | protected-region comparison + catalog hash | VERIFIED |
| R5 Independent fault protocol | public architecture packet | external author freezes 15-30 faults before first run | timestamped immutable archive + blind importer | EXTERNAL_BLOCKER |
| R6 End-to-end harness | R1-R3 | at least 30 source-to-result cases, including at least five wrong-result/late-failure/silent-acceptance cases | clean process replay in all policies | PENDING |
| R7 Second language | public SDK boundary | independent language compiles valid programs, rejects invalid shape/layout cases and adds at least eight faults | separate build/test + no-internals check | PENDING |
| R8 AlwaysVerify comparison | R1-R2 | P2 selective and P3 always compared on correctness and cost | paired raw results | FUNCTIONAL_LOCAL; decision-grade cost pending pinned benchmark |
| R9 Pinned benchmark | R1-R2, fixed machine | frozen environment/workload grid with raw distributions and CIs | environment identity + manifest | EXTERNAL_BLOCKER |
| R10 Ablations | R1-R3 | remove each material mechanism and measure impact | predeclared ablation matrix | PENDING |
| R11 Related work | stable implementation claims | current primary-source comparison | bibliography and claim audit | PENDING |
| R12 Paper | R1-R11 evidence | anonymous PDF/source satisfies format and claim boundaries | build, metadata, visual and anonymity checks | PENDING |
| R13 Artifact | all reproducible inputs | deterministic clean-unpack bundle | manifest + clean reproduction | PENDING |
| R14 Adversarial review | draft paper/artifact | strongest reviewer objections answered or claims weakened | one independent adversarial pass | IN_PROGRESS for runner milestone |

## Current next action

1. Record exact PR-head CI outcomes and repair any failures without weakening gates.
2. Separate the frozen oracle/reference-model representation from case execution.
3. Build the end-to-end source-to-result harness before making any whole-compilation or user-visible correctness claim.

## Stop condition

Stop only when all model-capable acceptance criteria are verified, human/hardware blockers are explicitly separated, and additional work would not change the submission verdict, evidence quality, risk, or next decision.
