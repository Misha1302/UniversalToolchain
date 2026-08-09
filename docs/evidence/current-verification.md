---
title: Verification Snapshot
description: Pinned build, test, workflow and bounded research-evidence record for a named repository baseline.
audience: maintainer-or-evaluator
status: pinned-snapshot
lastVerifiedAgainst: 99d9c81aadf3b335524b5bd1e77533612cc2ed93
snapshotCommit: 99d9c81aadf3b335524b5bd1e77533612cc2ed93
---

# Verification snapshot

This page is a pinned evidence record, not a live claim about whichever commit is currently at `master`. The code-bearing baseline is commit `99d9c81aadf3b335524b5bd1e77533612cc2ed93`. Later documentation-only changes must pass their own CI, but they do not silently rewrite the runtime evidence below.

## Ordinary integration gate

The canonical non-release GitHub Actions gate runs:

```bash ci-run=false
./build.sh --skip-docs --skip-pack
```

It restores and builds both `UniversalToolchain/Wist.sln` and `UniversalToolchain/PlanFuzz.sln`, builds runnable samples, executes the shared test manifest and runs architecture/documentation-status guards. Parallel project-graph traversal and shared compilation are the defaults; serial/no-build-server modes remain explicit diagnostics.

The documentation guard requires this table to mirror the repository's current exact test manifest even though the workflow receipt immediately below remains tied to the pinned snapshot commit.

| Project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| `Tests` | 540 | 0 | 0 |
| `UniversalToolchain.Modules.Tests` | 292 | 0 | 0 |
| `UniversalToolchain.Dialects.Tests` | 373 | 0 | 0 |
| `UniversalToolchain.LanguageSdk.Tests` | 156 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.Tests` | 41 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.IntegrationTests` | 10 | 0 | 0 |
| **Total** | **1,412** | **0** | **0** |

The exact manifest belongs to `eng/test-counts.json`. `.NET CI` run `31049607823` completed the canonical entrypoint for the pinned commit. Because the entrypoint enforces the manifest associated with its own revision, a stale or partial test total cannot satisfy that gate.

## Workflow set

Aggregate run `31049607752` completed successfully for commit `99d9c81aadf3b335524b5bd1e77533612cc2ed93`. The required push runs were:

| Workflow | Run | Result |
|---|---:|---|
| Docs Check | `31049609290` | success |
| Published Wist package smoke | `31049608876` | success |
| Documentation deployment | `31049608971` | success |
| Contract Experiment | `31049608582` | success |
| Wist Rollout Sample Smoke | `31049608089` | success |
| Benchmark Smoke | `31049608154` | success |
| UniversalToolchain validation | `31049608038` | success |
| .NET CI | `31049607823` | success |

`ci/aggregate` waits for the complete required workflow set and fails when a required workflow is missing, active beyond the deadline or completes with a non-acceptable conclusion.

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

The isolated B2 verifier-kernel microbenchmark was 46.0% median across five process replicates, range 44.7%-57.1%. This timing is environment-sensitive and is not whole-compilation overhead or a controlled pooled performance estimate.

## Post-freeze review holdouts

The artifact contains a four-case post-freeze review-derived holdout set: missing Bytecode producer identity, missing source-node identity, repeated pipeline occurrence and extension-provided verifier routing. Its protocol and expected matrix were frozen before result inspection.

| Set | B0 | B1 | B2 |
|---|---:|---:|---:|
| Review-derived holdouts | 0/4 | 4/4 | 4/4 |
| Valid-control false positives | 0/20 | 0/20 | 0/20 |

These holdouts remain separate from the original denominators. They are bounded evidence against overfitting to the earlier corpus, not an independently authored or statistically representative population sample.

## Documentation gates

The pinned revision ran:

```bash ci-run=false
npm ci --no-audit --no-fund
npm run docs:status
npm run docs:links
npm run docs:build
python3 .github/scripts/run-markdown-bash-blocks.py
```

The documentation-only remediation following this snapshot adds a release-state contract so root/package README, first-contact install pages, published-package smoke and the active stability document cannot drift independently.

## Package/release boundary

The deterministic runtime-boundary package matrix contains seven SDK/template packages at `0.3.0-alpha.4`, `UniversalToolchain.Wist.LanguagePack` at `0.3.0-alpha.5`, and the `UniversalToolchain.Wist` source candidate at `0.1.0-alpha.6`.

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

The published-package smoke is a different boundary: it installs `0.1.0-alpha.1` from NuGet.org in a clean temporary project. `0.1.0-alpha.6` was not published by the candidate verification work.

The local baseline-aware package gate was replayed against the exact `f13ad1310856e5618e1c3042c447ca543e0f3125` source archive and its deterministic reviewed package bundle. It passed version/content provenance for 9/9 package identities, embedded metadata and active-document synchronization, exact Wist API delta classification, package-surface checks, clean Wist/template/cross-package consumers and detached release-integrity mutation checks.

## PlanFuzz evidence boundary

Verified PlanFuzz behavior includes the language-neutral core, Acme and Wist adapters, seven oracle families, fresh-process strict replay, complete-evidence confirmation, separate inconclusive/flaky states, opt-in known regressions, distinct exact/class fingerprints and deterministic program/plan reduction guarded by exact-fingerprint replay.

The preserved Wist pilot included the regression corpus. Its violating-case count is not clean discovery yield, and its normalized classes are not unique-defect counts. Current bounded discovery and surface-oracle smokes are regression/stability evidence only.

Schedule reduction, lifecycle/concurrency campaigns, equal-budget baselines, a third adapter and publication novelty remain unverified future work.

The root `VERIFICATION.md` remains the detailed authority for commands, package boundaries, claim limits and artifact requirements.
