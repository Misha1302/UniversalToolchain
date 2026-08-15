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

It restores and builds both `UniversalToolchain/Wist.sln` and `UniversalToolchain/PlanFuzz.sln`, builds runnable samples, executes the shared test manifest and runs architecture/documentation-status guards. `UniversalToolchain.LanguageSdk.Generic.Tests` is an independent Wist-free owner proof and is therefore built by its explicit `buildBeforeTest` test-contract entry on both Linux and Windows. Parallel project-graph traversal and shared compilation are the defaults; serial/no-build-server modes remain explicit diagnostics.

The documentation guard requires this table to mirror the repository's current exact test manifest even though the workflow receipt immediately below remains tied to the pinned snapshot commit.

| Project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| `Tests` | 500 | 0 | 0 |
| `UniversalToolchain.Modules.Tests` | 292 | 0 | 0 |
| `UniversalToolchain.Dialects.Tests` | 245 | 0 | 0 |
| `UniversalToolchain.LanguageSdk.Generic.Tests` | 45 | 0 | 0 |
| `UniversalToolchain.LanguageSdk.Tests` | 170 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.Tests` | 41 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.IntegrationTests` | 10 | 0 | 0 |
| **Total** | **1,303** | **0** | **0** |

The exact manifest belongs to `eng/test-counts.json`. The current candidate adds focused regressions for the real syntax/semantic/lowering boundary, stage-local lifetime, plan-owned lowering activation, phase-owned module ordering and hidden ownership edges. Forty-one generic implementation tests were moved from mixed Wist-owned suites into the Wist-free generic project rather than restoring forbidden friend edges; together with its four existing ownership tests the canonical generic suite now contains 45 tests. `.NET CI` run `31049607823` completed the canonical entrypoint for the pinned historical commit; that receipt corresponds only to its own earlier manifest. A current revision is green only when its own exact manifest is verified.

## Workflow set

Aggregate run `31049607752` completed successfully for commit `99d9c81aadf3b335524b5bd1e77533612cc2ed93`; that historical receipt used the workflow contract of that revision.

For current revisions, `eng/ci-required-workflows.json` is the canonical machine-readable owner for code-acceptance workflows and allowed conclusions. `.github/workflows/ci-aggregate.yml` consumes that owner instead of maintaining a second list. Required workflows are fail-closed: only `success` is accepted; missing, skipped, neutral, cancelled, timed-out and failed required runs do not pass. Documentation correctness is owned by `Docs Check`; `Deploy documentation to GitHub Pages` is explicitly non-blocking for code acceptance because deployment is a publication step rather than a correctness prerequisite.

The current owner is itself checked by `CI contract check` and negative mutants, including removal of a required workflow and fail-open `skipped` acceptance.

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

Current documentation also carries the typed runtime-capability, Wist failure-taxonomy, source-retention, same-engine concurrency and Wist phase-ownership contracts introduced by the hardening work.

## Package/release boundary

The current source tree carries new unpublished identities for the package payloads changed by architecture/production hardening. Generated dependency metadata and template package references are treated as package payload. The matrix below intentionally mirrors current project identities even though the workflow receipts elsewhere on this page remain pinned historical evidence.

<!-- package-matrix:begin -->
| Package ID | Version |
|---|---|
| `UniversalToolchain.Language.Abstractions` | `0.3.0-alpha.4` |
| `UniversalToolchain.FeatureSdk` | `0.3.0-alpha.4` |
| `UniversalToolchain.LanguageSdk` | `0.3.0-alpha.5` |
| `UniversalToolchain.Runtime` | `0.3.0-alpha.5` |
| `UniversalToolchain.LanguageAuthoring` | `0.3.0-alpha.5` |
| `UniversalToolchain.Testing` | `0.3.0-alpha.5` |
| `UniversalToolchain.Templates` | `0.3.0-alpha.5` |
| `UniversalToolchain.Wist.LanguagePack` | `0.3.0-alpha.6` |
| `UniversalToolchain.Wist` | `0.1.0-alpha.7` |
<!-- package-matrix:end -->

The published-package smoke is a different boundary: it installs `0.1.0-alpha.1` from NuGet.org in a clean temporary project. `0.1.0-alpha.7` is a source candidate and is not published by this hardening work.

The historical baseline-aware package replay for the pinned evidence snapshot used the exact `f13ad1310856e5618e1c3042c447ca543e0f3125` source archive and its deterministic reviewed package bundle. That historical replay passed version/content provenance, embedded metadata, API delta, package-surface, clean-consumer and integrity checks for its own package identities. The current hardening candidate requires and must record its own full baseline-bearing package verification before integration readiness can be claimed.

## PlanFuzz evidence boundary

Verified PlanFuzz behavior includes the language-neutral core, Acme and Wist adapters, seven oracle families, fresh-process strict replay, complete-evidence confirmation, separate inconclusive/flaky states, opt-in known regressions, distinct exact/class fingerprints and deterministic program/plan reduction guarded by exact-fingerprint replay.

The preserved Wist pilot included the regression corpus. Its violating-case count is not clean discovery yield, and its normalized classes are not unique-defect counts. Current bounded discovery and surface-oracle smokes are regression/stability evidence only.

Schedule reduction, lifecycle/concurrency campaigns, equal-budget baselines, a third adapter and publication novelty remain unverified future work.

The root `VERIFICATION.md` remains the detailed authority for commands, package boundaries, claim limits and artifact requirements.
