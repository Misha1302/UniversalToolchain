---
title: Current Verification
description: Verification record for the integrated language-authoring and PlanFuzz research revision.
audience: maintainer-or-evaluator
status: current
lastVerifiedAgainst: planfuzz-phase0-phase1-integration
---

# Current verification

## Verified revision

The canonical GitHub Actions gate runs:

```bash ci-run=false
./build.sh --skip-docs
```

It restores and builds both `UniversalToolchain/Wist.sln` and the configuration-complete `UniversalToolchain/PlanFuzz.sln`, executes the shared test manifest, packs the canonical package matrix and runs clean consumer smokes.

## Recorded project result

```text
Release builds succeeded
0 build warnings
0 build errors
1,440 tests succeeded
0 failed
0 skipped
9 NuGet packages checked
clean template consumer smoke passed
cross-package package consumer smoke passed
```

Per-project test counts:

| Project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| `Tests` | 482 | 0 | 0 |
| `UniversalToolchain.Modules.Tests` | 288 | 0 | 0 |
| `UniversalToolchain.Dialects.Tests` | 588 | 0 | 0 |
| `UniversalToolchain.LanguageSdk.Tests` | 53 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.Tests` | 23 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.IntegrationTests` | 6 | 0 | 0 |
| **Total** | **1,440** | **0** | **0** |

The root `VERIFICATION.md` is the detailed authority for commands, environment, package checks and the PlanFuzz evidence boundary.

## Additional CI gates

The integrated revision must also pass:

```bash ci-run=false
npm ci --no-audit --no-fund
npm run docs:status
npm run docs:links
npm run docs:build
python3 .github/scripts/run-markdown-bash-blocks.py
```

GitHub Actions additionally verifies the Wist rollout sample and compares `MANIFEST.sha256` with a freshly generated manifest over all tracked source files except the manifest itself.

## Evidence boundary

Verified PlanFuzz behavior includes the language-neutral core, Acme and Wist adapters, five oracle families, fresh-process strict replay, complete-evidence confirmation, separate inconclusive/flaky states, opt-in known regressions and distinct exact/class fingerprints.

The preserved Wist pilot included the regression corpus. Its violating-case count is not a clean discovery-yield result, and its normalized classes are not unique-defect or root-cause counts.

Reduction, negative-surface/lifecycle campaigns, equal-budget baselines, a third adapter and publication novelty remain unverified future work.
