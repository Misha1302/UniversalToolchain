---
title: Current Verification
description: Current build, test, workflow and bounded research-evidence record.
audience: maintainer-or-evaluator
status: current
lastVerifiedAgainst: f13ad131-plus-runtime-boundary-candidate
---

# Current verification

## Ordinary integration gate

The canonical non-release GitHub Actions gate runs:

```bash ci-run=false
./build.sh --skip-docs --skip-pack
```

It restores and builds both `UniversalToolchain/Wist.sln` and the configuration-complete `UniversalToolchain/PlanFuzz.sln`, builds the runnable samples, executes the shared test manifest and runs architecture/documentation-status guards. Parallel project-graph traversal and shared compilation are the defaults; serial and no-build-server modes remain explicit diagnostic options.

| Project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| `Tests` | 555 | 0 | 0 |
| `UniversalToolchain.Modules.Tests` | 305 | 0 | 0 |
| `UniversalToolchain.Dialects.Tests` | 639 | 0 | 0 |
| `UniversalToolchain.LanguageSdk.Tests` | 82 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.Tests` | 41 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.IntegrationTests` | 10 | 0 | 0 |
| **Total** | **1,632** | **0** | **0** |

The CGO27 hardening branch retains the 1,597-test runtime-boundary baseline and adds 35 obligation-lifecycle, scheduler, demand-recomputation, numeric-promotion and P07 regression cases. The exact manifest belongs to `eng/test-counts.json`; the 1,632 count is locally confirmed from fresh TRX results and remains pending provider-backed CI confirmation.

## Workflow set

Every `master` revision must start and complete `.NET CI`, `UniversalToolchain validation`, `Docs Check`, documentation deployment, published-package smoke, rollout sample smoke, benchmark smoke and the contract experiment. `CI aggregate` waits for the complete set and publishes the `ci/aggregate` commit status. Master-push triggers are unconditional, preventing a false aggregate timeout caused by a required path-filtered workflow never starting.

Review-remediation master commit `2b0a4d1f0e255432daf0d5ddd485269b6490b67e` completed aggregate run `30585251873` successfully. The exact successful push runs were: Docs Check `30585251865`, published-package smoke `30585251928`, documentation deployment `30585251875`, Contract Experiment `30585251945`, rollout smoke `30585251930`, Benchmark Smoke `30585251904`, UniversalToolchain validation `30585251890`, and `.NET CI` `30585251901`.

## Production-boundary contract study

The non-packable experiment compares:

- **B0:** structural AIR and target-capability checks;
- **B1:** typed selection, ownership, Bytecode and facts/effects in addition to B0;
- **B2:** B1 with fail-closed unresolved reverification.

Master Contract Experiment run `30585251945` executed on commit `2b0a4d1f0e255432daf0d5ddd485269b6490b67e`. Artifact `contract-experiment-2b0a4d1f0e255432daf0d5ddd485269b6490b67e` has ID `8776245456` and digest `sha256:ca1708b8054e63eb9fff0526f9113013a9569121890ff3b0ea19572e5c199961`. Independent extraction verified every entry in both the main-study and holdout checksum trees; both captured git-status files are empty.

| Set | B0 | B1 | B2 |
|---|---:|---:|---:|
| Primary detections | 12/32 | 28/32 | 32/32 |
| Challenge detections | 1/10 | 10/10 | 10/10 |
| Control false positives | 0/100 | 0/100 | 0/100 |

On this frozen author-designed corpus, B0 versus B2 differs on 20/32 operators and exact McNemar is `p = 1.9073486328125e-06`; B1 versus B2 has four discordant operators and `p = 0.125`. These values describe the fixed corpus and do not establish population-level superiority.

The master artifact's isolated B2 verifier-kernel microbenchmark is 46.0% median across five process replicates, range 44.7%-57.1%. Earlier functionally identical runs produced materially different values. This timing is environment-sensitive and is not whole-compilation overhead or a controlled pooled performance estimate.

## Post-freeze review holdouts

The master artifact also contains a four-case post-freeze review-derived holdout set: missing Bytecode producer identity, missing source-node identity, repeated pipeline occurrence and extension-provided verifier routing. Its protocol and expected matrix were frozen before result inspection.

| Set | B0 | B1 | B2 |
|---|---:|---:|---:|
| Review-derived holdouts | 0/4 | 4/4 | 4/4 |
| Valid-control false positives | 0/20 | 0/20 | 0/20 |

These holdouts remain separate from the original primary and challenge denominators. They are bounded evidence against overfitting to the earlier corpus, but they are not independently authored, statistically representative or sufficient for a general unseen-fault claim.

## Documentation gates

The integrated revision also runs:

```bash ci-run=false
npm ci --no-audit --no-fund
npm run docs:status
npm run docs:links
npm run docs:build
python3 .github/scripts/run-markdown-bash-blocks.py
```

Master Docs Check run `30585251865` and the Markdown command smoke in `.NET CI` run `30585251901` completed successfully.

## Package/release boundary

The deterministic runtime-boundary package matrix contains seven SDK/template packages at `0.3.0-alpha.4`, `UniversalToolchain.Wist.LanguagePack` at `0.3.0-alpha.5`, and `UniversalToolchain.Wist` at `0.1.0-alpha.6`. The published-package smoke remains intentionally pinned to actually published facade `0.1.0-alpha.1`.

<!-- package-matrix:begin -->
| Package ID | Version |
|---|---|
| `UniversalToolchain.Language.Abstractions` | `0.3.0-alpha.4` |
| `UniversalToolchain.FeatureSdk` | `0.3.0-alpha.4` |
| `UniversalToolchain.LanguageSdk` | `0.3.0-alpha.4` |
| `UniversalToolchain.Runtime` | `0.3.0-alpha.4` |
| `UniversalToolchain.LanguageAuthoring` | `0.3.0-alpha.4` |
| `UniversalToolchain.Testing` | `0.3.0-alpha.4` |
| `UniversalToolchain.Templates` | `0.3.0-alpha.4` |
| `UniversalToolchain.Wist.LanguagePack` | `0.3.0-alpha.5` |
| `UniversalToolchain.Wist` | `0.1.0-alpha.6` |
<!-- package-matrix:end -->

The local baseline-aware package gate was replayed against the exact `f13ad1310856e5618e1c3042c447ca543e0f3125` source archive and its deterministic reviewed package bundle. It passed version/content provenance for 9/9 package identities, embedded metadata and active-document synchronization, exact Wist API delta classification, package-surface checks, clean Wist/template/cross-package consumers and detached release-integrity mutation checks.

This evidence applies to the local candidate tree and generated package bundle. GitHub Actions and aggregate status remain independent provider-backed confirmation; the packages were not published to NuGet.org.

## PlanFuzz evidence boundary

Verified PlanFuzz behavior includes the language-neutral core, Acme and Wist adapters, seven oracle families, fresh-process strict replay, complete-evidence confirmation, separate inconclusive/flaky states, opt-in known regressions, distinct exact/class fingerprints and deterministic program/plan reduction guarded by exact-fingerprint replay.

The preserved Wist pilot included the regression corpus. Its violating-case count is not clean discovery yield, and its normalized classes are not unique-defect counts. Current bounded discovery and surface-oracle smokes are regression/stability evidence only.

Schedule reduction, lifecycle/concurrency campaigns, equal-budget baselines, a third adapter and publication novelty remain unverified future work.

The root `VERIFICATION.md` is the detailed authority for commands, package boundaries, claim limits and artifact requirements.
