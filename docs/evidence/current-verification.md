---
title: Current Verification
description: Current build, test, workflow and bounded research-evidence record.
audience: maintainer-or-evaluator
status: current
lastVerifiedAgainst: review-remediation-pr-320-pending-exact-run
---

# Current verification

## Ordinary integration gate

The canonical non-release GitHub Actions gate runs:

```bash ci-run=false
./build.sh --skip-docs --skip-pack
```

It restores and builds both `UniversalToolchain/Wist.sln` and the configuration-complete `UniversalToolchain/PlanFuzz.sln`, builds the runnable samples, executes the shared test manifest and runs architecture/documentation-status guards. Parallel project-graph traversal and shared compilation are the defaults; serial and no-build-server modes remain explicit diagnostic options.

The review-remediation manifest expects:

```text
Release builds succeeded
0 build warnings
0 build errors
1,551 tests succeeded
0 failed
0 skipped
```

| Project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| `Tests` | 512 | 0 | 0 |
| `UniversalToolchain.Modules.Tests` | 292 | 0 | 0 |
| `UniversalToolchain.Dialects.Tests` | 614 | 0 | 0 |
| `UniversalToolchain.LanguageSdk.Tests` | 82 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.Tests` | 41 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.IntegrationTests` | 10 | 0 | 0 |
| **Total** | **1,551** | **0** | **0** |

The seven added regressions cover incomplete Bytecode emission identity, repeated pipeline occurrences, extension verifier routing, primary-first construction failure preservation and stale flowed operation leases. The exact count becomes current authority only after the PR and post-merge master run complete; any drift fails the gate.

## Complete master workflow set

Every `master` revision must start and complete `.NET CI`, `UniversalToolchain validation`, `Docs Check`, documentation deployment, published-package smoke, rollout sample smoke, benchmark smoke and the contract experiment. `CI aggregate` waits for the complete set and publishes the `ci/aggregate` commit status. Master-push triggers are unconditional, preventing a false aggregate timeout caused by a required path-filtered workflow never starting.

Master commit `92028b76b108822c5cdd41432721ac63c4e49b48` is the immutable pre-remediation publication baseline. Aggregate run `30542053062` completed successfully after all eight required workflows reported success.

Documentation-only descendant commit `9b6aa223592f768a6e4abc12b298bdf59bb57d4a` also completed all eight required workflows in aggregate run `30569244318`. Its `.NET CI` run `30569244264` repeated the exact 1,544-test pre-remediation contract.

## Production-boundary contract study

The non-packable experiment compares:

- **B0:** structural AIR and target-capability checks;
- **B1:** typed selection, ownership, Bytecode and facts/effects in addition to B0;
- **B2:** B1 with fail-closed unresolved reverification.

The immutable pre-remediation baseline is workflow run `30542053093`, artifact ID `8759126014`, digest `sha256:e87ea19405cce64df8d76ea83fc3b02c5db5c7b83f435ee940d7ee2bb850209f`. Its checksum index covers 36 files and verifies after extraction.

The run used 32 primary operator shapes, 10 post-freeze challenge operators, three repetitions per instance/mode and 100 valid controls per mode across five boundary families.

| Set | B0 | B1 | B2 |
|---|---:|---:|---:|
| Primary detections | 12/32 | 28/32 | 32/32 |
| Challenge detections | 1/10 | 10/10 | 10/10 |
| Control false positives | 0/100 | 0/100 | 0/100 |

On this frozen author-designed corpus, B0 versus B2 differs on 20/32 operators and has exact McNemar `p = 1.9073486e-06`; B1 versus B2 has four discordant operators and `p = 0.125`. These values describe the fixed corpus and do not establish population-level superiority. The isolated B2 verifier-kernel overhead was 27.8% median across five process replicates, range 25.6%–31.6%.

The unchanged experiment was repeated on documentation-only descendant `9b6aa223592f768a6e4abc12b298bdf59bb57d4a` in workflow `30569244273`. Artifact ID `8770101865`, digest `sha256:0669f05b53080b05a93dffe4cd33a3418807270ae7295f8fa9313999a5719019`, reproduced all functional and statistical results; its five timing replicates gave 26.4% median and 23.8%–33.5% range. The ten replicates across both workflows have a descriptive 27.7% median and 23.8%–33.5% full range.

This is an author-designed production-boundary experiment, not an externally authored unseen-fault study or an end-to-end whole-compiler benchmark. Raw JSONL, runner inputs, environment records, analysis and a per-file checksum index are archived by the workflow for each exact checked-out commit. The verifier-kernel timing is environment-sensitive and is not whole-compilation overhead or a controlled pooled performance estimate.

## Post-freeze review holdouts

A separate non-packable executable evaluates four later review-derived cases: missing Bytecode producer identity, missing source-node identity, repeated pipeline occurrence and extension-provided verifier routing. Its protocol and expected matrix were frozen before inspecting its workflow result. It uses three repetitions and 20 valid controls per mode, with expected results B0 `0/4`, B1 `4/4`, B2 `4/4`, and no control false positives.

These holdouts remain separate from the original primary and challenge denominators. They are evidence against overfitting to the earlier corpus, but they are not independently authored, statistically representative or sufficient for a general unseen-fault claim. Exact run and artifact identity will replace this pending statement after successful inspection.

## Documentation gates

The integrated revision also runs:

```bash ci-run=false
npm ci --no-audit --no-fund
npm run docs:status
npm run docs:links
npm run docs:build
python3 .github/scripts/run-markdown-bash-blocks.py
```

## Package/release boundary

Ordinary CI uses `--skip-pack` because the release package gate intentionally requires a reviewed previous source archive and previous package bundle. A full package decision must provide those exact inputs; absence is a hard failure, not an implicit compatibility pass.

The package matrix currently declares seven SDK/template packages at `0.3.0-alpha.2`, `UniversalToolchain.Wist.LanguagePack` at `0.3.0-alpha.3`, and `UniversalToolchain.Wist` at `0.1.0-alpha.4`. The published-package smoke remains intentionally pinned to the actually published facade `0.1.0-alpha.1`.

The last recorded full package gate produced and checked 9/9 packages and passed template and cross-package consumer smokes. It remains historical package evidence until rerun with reviewed baseline artifacts for the exact newer commit.

## PlanFuzz evidence boundary

Verified PlanFuzz behavior includes the language-neutral core, Acme and Wist adapters, seven oracle families, fresh-process strict replay, complete-evidence confirmation, separate inconclusive/flaky states, opt-in known regressions, distinct exact/class fingerprints and deterministic program/plan reduction guarded by exact-fingerprint replay.

The preserved Wist pilot included the regression corpus. Its violating-case count is not clean discovery yield, and its normalized classes are not unique-defect counts. Current bounded discovery and surface-oracle smokes are regression/stability evidence only.

Schedule reduction, lifecycle/concurrency campaigns, equal-budget baselines, a third adapter and publication novelty remain unverified future work.

The root `VERIFICATION.md` is the detailed authority for commands, package boundaries, claim limits and artifact requirements.
