---
title: Current Verification
description: Current build, test, workflow and bounded research-evidence record.
audience: maintainer-or-evaluator
status: current
lastVerifiedAgainst: review-remediation-head-77edfb05
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
| `Tests` | 512 | 0 | 0 |
| `UniversalToolchain.Modules.Tests` | 292 | 0 | 0 |
| `UniversalToolchain.Dialects.Tests` | 614 | 0 | 0 |
| `UniversalToolchain.LanguageSdk.Tests` | 82 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.Tests` | 41 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.IntegrationTests` | 10 | 0 | 0 |
| **Total** | **1,551** | **0** | **0** |

The seven added regressions cover incomplete Bytecode emission identity, repeated pipeline occurrences, extension verifier routing, primary-first construction failure preservation and stale flowed operation leases. `.NET CI` run `30580457345` on review-remediation head `77edfb05047c933665912015af6d242a3a9f7fed` completed successfully with `TEST-CONTRACT COMPLETE passed=1551 entries=14`.

## Workflow set

Every `master` revision must start and complete `.NET CI`, `UniversalToolchain validation`, `Docs Check`, documentation deployment, published-package smoke, rollout sample smoke, benchmark smoke and the contract experiment. `CI aggregate` waits for the complete set and publishes the `ci/aggregate` commit status. Master-push triggers are unconditional, preventing a false aggregate timeout caused by a required path-filtered workflow never starting.

Master commit `92028b76b108822c5cdd41432721ac63c4e49b48` is the immutable pre-remediation publication baseline. Aggregate run `30542053062` completed successfully after all eight required workflows reported success.

On review-remediation branch head `77edfb05047c933665912015af6d242a3a9f7fed`, `.NET CI`, validation, Docs Check, published-package smoke, rollout smoke, benchmark smoke, Contract Experiment and Package Compatibility Review all completed successfully. Final master authority is assigned only after the post-merge aggregate.

## Production-boundary contract study

The non-packable experiment compares:

- **B0:** structural AIR and target-capability checks;
- **B1:** typed selection, ownership, Bytecode and facts/effects in addition to B0;
- **B2:** B1 with fail-closed unresolved reverification.

Contract workflow run `30580457769` executed through PR merge ref `ea2b99b6edf1ea171f2c6932531d2870f5a2ec0d` for branch head `77edfb05047c933665912015af6d242a3a9f7fed`. Artifact ID `8774419595`, digest `sha256:57f90f0b4b359d7cf98e3549091c2fd7f1387b3fe90b0766faf2b6b40b1cba4f`, contains separately verified main-study and holdout checksum trees; both captured git-status files are empty.

| Set | B0 | B1 | B2 |
|---|---:|---:|---:|
| Primary detections | 12/32 | 28/32 | 32/32 |
| Challenge detections | 1/10 | 10/10 | 10/10 |
| Control false positives | 0/100 | 0/100 | 0/100 |

On this frozen author-designed corpus, B0 versus B2 differs on 20/32 operators and exact McNemar is `p = 1.9073486328125e-06`; B1 versus B2 has four discordant operators and `p = 0.125`. These values describe the fixed corpus and do not establish population-level superiority.

The current isolated B2 verifier-kernel microbenchmark is 47.3% median across five process replicates, range 44.4%–50.7%. Earlier functionally identical runs produced materially lower values. This timing is environment-sensitive and is not whole-compilation overhead or a controlled pooled performance estimate.

## Post-freeze review holdouts

The artifact also contains a four-case post-freeze review-derived holdout set: missing Bytecode producer identity, missing source-node identity, repeated pipeline occurrence and extension-provided verifier routing. Its protocol and expected matrix were frozen before result inspection.

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

Docs Check run `30580457714` completed successfully on the same review-remediation head.

## Package/release boundary

The review-remediation package matrix contains seven SDK/template packages at `0.3.0-alpha.3`, `UniversalToolchain.Wist.LanguagePack` at `0.3.0-alpha.4`, and `UniversalToolchain.Wist` at `0.1.0-alpha.5`. The published-package smoke remains intentionally pinned to actually published facade `0.1.0-alpha.1`.

Package Compatibility Review run `30580457427` deterministically reconstructed reviewed previous source and package baselines from commit `3abd958fae087e93135e88460ae0c0a2328afad5`, bound their exact hashes into the compatibility inputs and executed the canonical baseline-bearing package gate. It passed 1,551 tests, monotonic version provenance, exact Wist API delta `removed=0, added=0`, 9/9 package validation, clean facade and cross-package consumers, template creation/run and release-integrity mutation checks.

Artifact ID `8774539955`, digest `sha256:6cfa9d197548887ef8a80e701900d936c4364aa849e70e9c2b8ed420adfd3a7b`, has a verified 27-entry outer checksum manifest. Both baseline artifact hashes verify, and release-integrity root `7069c53758e1735ecf41813d65782dcd76f2d22f8e00e8c193bc42ee50a1457a` covers ten current package artifacts, including the Wist symbols package.

The candidate package versions are validated but were not published to NuGet.org by this workflow.

## PlanFuzz evidence boundary

Verified PlanFuzz behavior includes the language-neutral core, Acme and Wist adapters, seven oracle families, fresh-process strict replay, complete-evidence confirmation, separate inconclusive/flaky states, opt-in known regressions, distinct exact/class fingerprints and deterministic program/plan reduction guarded by exact-fingerprint replay.

The preserved Wist pilot included the regression corpus. Its violating-case count is not clean discovery yield, and its normalized classes are not unique-defect counts. Current bounded discovery and surface-oracle smokes are regression/stability evidence only.

Schedule reduction, lifecycle/concurrency campaigns, equal-budget baselines, a third adapter and publication novelty remain unverified future work.

The root `VERIFICATION.md` is the detailed authority for commands, package boundaries, claim limits and artifact requirements.
