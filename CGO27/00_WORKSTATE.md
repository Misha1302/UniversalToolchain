# CGO 2027 completion workstate

Last updated: 2026-07-31.
Branch: `research/cgo27-selective-reverification`.
Repository baseline: `master` at `c73b418c6e72e8b92371753a3a7b4a9f7adaa5f1`.
Code-equivalent verified baseline: `2b0a4d1f0e255432daf0d5ddd485269b6490b67e`; the four later commits modify only three documentation files.

## Objective

Produce decision-grade, reproducible evidence for selective contract-guided reverification in an extensible compiler pipeline, with four explicit policies, telemetry, end-to-end cases, a genuinely independent second language, pinned performance evaluation, ablations, an anonymous CGO paper, and a clean artifact bundle.

## Authority boundary

Allowed: read repository; modify this branch; run build/tests/benchmarks; commit; open a PR; create anonymous paper and artifact bundles.
Not allowed without separate approval: direct push to `master`; merge; NuGet publication; conference submission; deletion or rewriting of historical evidence.

## Baseline observations

- Existing experiment implements only `B0`, `B1`, and `B2`.
- Existing frozen primary corpus is 40 instances / 32 operator shapes; challenge set is 10 operators; valid controls are 100 per mode.
- Current production-boundary result is B0/B1/B2 = 12/32, 28/32, 32/32 with zero false positives in 100 controls per mode.
- Existing performance evidence is an isolated verifier-kernel microbenchmark. It is not whole-compilation evidence and cannot support an end-to-end overhead claim.
- Existing challenge and review-holdout sets are author/reviewer designed; neither is an externally authored blind corpus.

## Completion ledger

| Result | Dependency | Observable done condition | Validation | Status |
|---|---|---|---|---|
| R1 Four policies | current runner, production policy semantics | one runner exposes P0/P1/P2/P3 with mechanically distinct behavior | focused policy tests + all-corpus execution | IN_PROGRESS |
| R2 Telemetry | R1 | per-boundary verifier calls, invalidations, reverifications, timings, allocations and peak memory emitted in raw schema | schema validation + deterministic replay | PENDING |
| R3 Reference model/oracles | R1 | expected detection, diagnostic family, boundary and infra/flaky classifications encoded outside execution shortcuts | oracle validator + negative mutants | PENDING |
| R4 Historical preservation | baseline manifests | existing denominators and source hashes remain unchanged | protected-region hash comparison | ACTIVE |
| R5 Independent fault protocol | public architecture packet | external author freezes 15-30 faults before first run | timestamped immutable archive + blind importer | EXTERNAL_BLOCKER |
| R6 End-to-end harness | R1-R3 | at least 30 source-to-result cases, including at least five wrong-result/late-failure/silent-acceptance cases | clean process replay in all policies | PENDING |
| R7 Second language | public SDK boundary | independent language compiles valid programs, rejects invalid shape/layout cases and adds at least eight faults | separate build/test + no-internals check | PENDING |
| R8 AlwaysVerify comparison | R1-R2 | P2 selective and P3 always compared on correctness and cost | paired raw results | PENDING |
| R9 Pinned benchmark | R1-R2, fixed machine | frozen environment/workload grid with raw distributions and CIs | environment identity + manifest | EXTERNAL_BLOCKER |
| R10 Ablations | R1-R3 | remove each material mechanism and measure impact | predeclared ablation matrix | PENDING |
| R11 Related work | stable implementation claims | current primary-source comparison | bibliography and claim audit | PENDING |
| R12 Paper | R1-R11 evidence | anonymous PDF/source satisfies format and claim boundaries | build, metadata, visual and anonymity checks | PENDING |
| R13 Artifact | all reproducible inputs | deterministic clean-unpack bundle | manifest + clean reproduction | PENDING |
| R14 Adversarial review | draft paper/artifact | strongest reviewer objections answered or claims weakened | one independent adversarial pass | PENDING |

## Current next action

Implement and validate the four-policy execution model and telemetry schema without modifying the frozen historical corpus or its denominators.

## Stop condition

Stop only when all model-capable acceptance criteria are verified, human/hardware blockers are explicitly separated, and additional work would not change the submission verdict, evidence quality, risk, or next decision.
