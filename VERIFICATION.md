# Wist2 verification and research-evidence record

## Environment and authority

- Record refreshed: 2026-07-30.
- Target CI environment: GitHub Actions Ubuntu 24.04, Linux x64.
- SDK policy: `UniversalToolchain/global.json` with .NET 10 feature-band roll-forward.
- Ordinary integration command: `./build.sh --skip-docs --skip-pack`.
- Contract-study command: `CONTRACT_EXPERIMENT_REPLICATES=5 ./Tools/run-contract-experiment.sh artifacts/contract-experiment`.
- Review-holdout command: `bash ./Tools/run-contract-review-holdout.sh artifacts/contract-review-holdout`.
- Package dependency mode: repository `NuGet.config`; the optional repository-local feed is materialized as an empty directory before restore.

Source identity is owned by the Git commit. Every contract-study result tree carries its exact workflow commit, runner inputs, environment metadata and a per-file SHA-256 checksum index. Release packages use a separate detached integrity chain and require explicit previous-source and previous-package baseline artifacts; ordinary CI never fabricates or silently bypasses those inputs.

## Current integrated contracts

The repository implements and continuously verifies:

- exact, timeout-bounded test counts from `eng/test-counts.json`;
- deterministic language/package planning, schema-v5 locks and exact package-manifest binding;
- exact `(contribution, backend, input contract)` executor resolution;
- strict selected-module contract enforcement in the Wist composition path;
- production Bytecode metadata reading plus declared/observed emission verification;
- mandatory producer-module and source-node identity for every contract-annotated Bytecode emission;
- AIR structural, stack and backend-capability verification;
- compiler facts/effects and fail-closed routing of unresolved reverification requests;
- extension-provided compiler-fact verifier routes with conflict rejection;
- explicit rejection of repeated module identities until occurrence-sensitive effects are modeled;
- `PerSession` ownership and explicit `SingletonStateless` lifecycle rules;
- primary-first preservation of runtime construction failures when cleanup also fails;
- active-lease tracking that ignores completed leases retained by flowed execution contexts;
- interpreter/CIL parity on the shared supported surface;
- PlanFuzz fresh-process replay, evidence-complete classification and exact-fingerprint reduction;
- a separate production-boundary B0/B1/B2 contract experiment;
- a separately reported post-freeze review-derived holdout set.

The retained product boundary remains a contribution/pass/route/runtime SDK rather than a declarative grammar, binder or type-system workbench. PlanFuzz and the contract experiments are non-packable research layers and do not extend the public Wist NuGet surface.

## Ordinary build and test gate

```bash ci-run=false
./build.sh --skip-docs --skip-pack
```

This gate restores and builds `UniversalToolchain/Wist.sln`, `UniversalToolchain/PlanFuzz.sln` and the runnable samples, then executes the exact test manifest and architecture/documentation-status guards.

Parallel graph traversal and shared compilation are the default. `--jobs`, `--serial` and `--no-build-servers` provide explicit diagnostic fallbacks. The two solutions are still invoked sequentially because they share output directories.

### Regression-test contract

| Test project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| `Tests` | 512 | 0 | 0 |
| `UniversalToolchain.Modules.Tests` | 292 | 0 | 0 |
| `UniversalToolchain.Dialects.Tests` | 614 | 0 | 0 |
| `UniversalToolchain.LanguageSdk.Tests` | 82 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.Tests` | 41 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.IntegrationTests` | 10 | 0 | 0 |
| **Total** | **1,551** | **0** | **0** |

The manifest reflects the seven focused regressions added by the review remediation. These exact counts remain provisional evidence until the current branch and post-merge master runs complete; the gate fails on any drift.

## Production-boundary contract experiment

The dedicated `Contract Experiment` workflow restores and builds the non-packable experiment against the exact current production API before collecting evidence.

### Compared modes

- **B0:** structural AIR and target-capability checks only.
- **B1:** B0 plus typed selection, ownership, Bytecode and facts/effects checks; unresolved reverification is recorded but not fail-closed.
- **B2:** B1 plus mandatory failure on unresolved reverification requests.

### Immutable master baseline

The publication baseline is tied to master commit `92028b76b108822c5cdd41432721ac63c4e49b48`, workflow run `30542053093` and artifact `contract-experiment-92028b76b108822c5cdd41432721ac63c4e49b48` (artifact ID `8759126014`, digest `sha256:e87ea19405cce64df8d76ea83fc3b02c5db5c7b83f435ee940d7ee2bb850209f`). Its 36-file checksum index verifies successfully after extraction.

The run used 40 primary fault instances representing 32 independent operator shapes in five families, 10 post-freeze challenge operators, three deterministic repetitions per instance/mode and 100 valid controls per mode across five strata.

| Set | B0 | B1 | B2 |
|---|---:|---:|---:|
| Primary operators detected | 12/32 | 28/32 | 32/32 |
| Challenge operators detected | 1/10 | 10/10 | 10/10 |
| Valid-control false positives | 0/100 | 0/100 | 0/100 |

On this frozen author-designed corpus, the paired B0-versus-B2 difference is 20/32 and the exact McNemar value is `p = 1.9073486e-06`; B1 versus B2 is 4/32 discordant and `p = 0.125`. These values describe the frozen corpus and are not a population-level superiority claim. Across five process-level timing replicates, isolated B2 boundary-kernel overhead had a median of **27.8%**, with a range of **25.6%–31.6%**.

A documentation-only descendant, commit `9b6aa223592f768a6e4abc12b298bdf59bb57d4a`, reran the unchanged experiment in workflow `30569244273`. Artifact `contract-experiment-9b6aa223592f768a6e4abc12b298bdf59bb57d4a` (artifact ID `8770101865`, digest `sha256:0669f05b53080b05a93dffe4cd33a3418807270ae7295f8fa9313999a5719019`) has a separately verified 36-file checksum index and reproduces every detection, control and McNemar result above. Its five timing replicates produced a median of **26.4%** and a range of **23.8%–33.5%**. Across the ten replicates from both workflow executions, the descriptive median is **27.7%** and the full range is **23.8%–33.5%**.

These are author-designed, single-framework, production-boundary results. They do not establish general compiler correctness, end-to-end source-to-execution detection, externally authored unseen-fault effectiveness or external validity across unrelated runtimes. The timing number is an environment-sensitive verifier-kernel microbenchmark, not whole-compilation or application overhead; the cross-run summary is descriptive rather than a controlled pooled performance estimate.

### Post-freeze review holdouts

A separate four-operator holdout executable covers missing Bytecode producer identity, missing source-node identity, repeated pipeline occurrence and extension-provided verifier routing. The protocol was frozen after those findings were obtained from a later adversarial review and before the workflow result was inspected. The cases stay outside the original primary and challenge denominators. They are review-derived holdouts, not an externally authored or statistically representative unseen-fault sample.

The expected matrix is B0 `0/4`, B1 `4/4`, B2 `4/4`, with `0/20` false positives per mode on valid controls. Exact run and artifact identities are added only after a successful workflow artifact is independently inspected.

## Workflow contract

Every `master` revision is required to start and complete:

- `.NET CI`;
- `UniversalToolchain validation`;
- `Docs Check`;
- `Deploy documentation to GitHub Pages`;
- `Published Wist package smoke`;
- `Wist Rollout Sample Smoke`;
- `Benchmark Smoke`;
- `Contract Experiment`.

`CI aggregate` waits for this complete workflow set and publishes the `ci/aggregate` commit status. Path-filtered pull-request checks remain narrow where appropriate; master-push checks are unconditional so the aggregate cannot wait for a workflow that was never eligible to start.

For the immutable publication baseline commit `92028b76b108822c5cdd41432721ac63c4e49b48`, aggregate run `30542053062` completed successfully after all eight required workflows reported success.

The documentation-only descendant commit `9b6aa223592f768a6e4abc12b298bdf59bb57d4a` also completed all eight required workflows in aggregate run `30569244318`; `.NET CI` run `30569244264` recorded `TEST-CONTRACT COMPLETE passed=1544 entries=14`.

## Package and release boundary

The package matrix contains nine projects:

Core SDK/template family `0.3.0-alpha.2`:

- `UniversalToolchain.Language.Abstractions`;
- `UniversalToolchain.FeatureSdk`;
- `UniversalToolchain.LanguageSdk`;
- `UniversalToolchain.Runtime`;
- `UniversalToolchain.LanguageAuthoring`;
- `UniversalToolchain.Testing`;
- `UniversalToolchain.Templates`.

Additional packages:

- `UniversalToolchain.Wist.LanguagePack` `0.3.0-alpha.3`;
- `UniversalToolchain.Wist` `0.1.0-alpha.4`.

A full package/release run must supply both:

```bash ci-run=false
./build.sh --skip-docs \
  --baseline-source-archive /path/to/reviewed-previous-source.zip \
  --previous-package-bundle /path/to/reviewed-previous-packages.tar.gz
```

Without those reviewed baseline identities, packaging intentionally fails closed. The ordinary green CI result therefore proves build/test/docs/smoke/research gates, not a newly regenerated release-compatibility decision. The previously recorded package gate produced 9/9 packages, validated exact nuspec/package identities and passed template and cross-package consumer smokes; it must not be relabelled as an exact rerun for a newer commit.

The published-package smoke is intentionally pinned to the actually published `UniversalToolchain.Wist` `0.1.0-alpha.1`. It verifies the external published baseline, not the current source package version.

## PlanFuzz evidence boundary

Verified PlanFuzz behavior includes:

- language-neutral core with Acme and Wist adapters;
- seven generic oracle families;
- schema-v4 fail-closed surface/owner evidence;
- fresh-process replay with per-file artifact checksums;
- strict clean/confirmed/flaky/inconclusive/infrastructure separation;
- opt-in historical regression corpus;
- separate exact and class fingerprints;
- structured program/plan reduction accepted only after exact-fingerprint replay;
- canonical SF-005 and SF-011 seeded faults observed through ordinary runtime activation evidence.

The historical 25-case Wist pilot included the regression corpus and is not a clean discovery-yield result. Its normalized classes are triage groups, not unique defects. Current post-fix bounded discovery smokes are regression/stability evidence only.

Not yet claimed:

- lifecycle/session/concurrency schedule generation or reduction;
- equal-budget PlanFuzz superiority;
- a third external adapter;
- publication novelty or acceptance likelihood;
- stable 1.0 compatibility, hostile-extension sandboxing or production-workload certification.

## Artifact cleanliness

Release archives must exclude `bin`, `obj`, `artifacts`, generated VitePress output, `node_modules`, `.git`, IDE metadata, test outputs, caches and secret-like files. Every retained release file must be covered by a per-file SHA-256 checksum index, followed by clean extraction and independent restore/build/run verification.
