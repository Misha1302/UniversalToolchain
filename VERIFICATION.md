# Wist2 verification and research-evidence record

## Environment and authority

- Record refreshed: 2026-07-30.
- Target CI environment: GitHub Actions Ubuntu 24.04, Linux x64.
- SDK policy: `UniversalToolchain/global.json` with .NET 10 feature-band roll-forward.
- Ordinary integration command: `./build.sh --skip-docs --skip-pack`.
- Contract-study command: `CONTRACT_EXPERIMENT_REPLICATES=5 ./Tools/run-contract-experiment.sh artifacts/contract-experiment`.
- Review-holdout command: `bash ./Tools/run-contract-review-holdout.sh artifacts/contract-review-holdout`.
- Package dependency mode: repository `NuGet.config`; the optional repository-local feed is materialized as an empty directory before restore.

Source identity is owned by the Git commit. Each contract-study result tree carries its exact workflow commit, runner inputs, environment metadata and per-file SHA-256 checksum index. Release packages use a separate detached integrity chain and require explicit previous-source and previous-package baseline artifacts; ordinary CI never fabricates or silently bypasses those inputs.

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

The seven focused review-remediation regressions cover incomplete Bytecode emission identity, repeated pipeline occurrences, extension verifier routing, primary-first construction failure preservation and stale flowed operation leases. `.NET CI` run `30580457345` on branch head `77edfb05047c933665912015af6d242a3a9f7fed` completed successfully and recorded `TEST-CONTRACT COMPLETE passed=1551 entries=14`.

## Production-boundary contract experiment

The dedicated `Contract Experiment` workflow restores and builds the non-packable experiment against the exact current production API before collecting evidence.

### Compared modes

- **B0:** structural AIR and target-capability checks only.
- **B1:** B0 plus typed selection, ownership, Bytecode and facts/effects checks; unresolved reverification is recorded but not fail-closed.
- **B2:** B1 plus mandatory failure on unresolved reverification requests.

### Pre-remediation immutable baseline

The pre-remediation publication baseline is tied to master commit `92028b76b108822c5cdd41432721ac63c4e49b48`, workflow run `30542053093` and artifact ID `8759126014`, digest `sha256:e87ea19405cce64df8d76ea83fc3b02c5db5c7b83f435ee940d7ee2bb850209f`.

### Review-remediation reproduction

Contract workflow run `30580457769` executed for branch head `77edfb05047c933665912015af6d242a3a9f7fed` through PR merge ref `ea2b99b6edf1ea171f2c6932531d2870f5a2ec0d`. Artifact `contract-experiment-ea2b99b6edf1ea171f2c6932531d2870f5a2ec0d` has artifact ID `8774419595` and digest `sha256:57f90f0b4b359d7cf98e3549091c2fd7f1387b3fe90b0766faf2b6b40b1cba4f`. Both internal checksum trees verify after extraction and both captured git-status files are empty.

The original frozen corpus remains unchanged: 40 primary instances representing 32 operator shapes, 10 challenge operators, three deterministic repetitions per instance/mode and 100 valid controls per mode.

| Set | B0 | B1 | B2 |
|---|---:|---:|---:|
| Primary operators detected | 12/32 | 28/32 | 32/32 |
| Challenge operators detected | 1/10 | 10/10 | 10/10 |
| Valid-control false positives | 0/100 | 0/100 | 0/100 |

On this frozen author-designed corpus, the paired B0-versus-B2 difference is 20/32 and exact McNemar is `p = 1.9073486328125e-06`; B1 versus B2 is four discordant operators and `p = 0.125`. These values describe this corpus and are not a population-level superiority claim.

The current five process-level timing replicates report isolated B2 boundary-kernel overhead of **47.3% median**, range **44.4%–50.7%**. Earlier identical functional runs reported materially lower values. This confirms that the timing is an environment-sensitive verifier-kernel microbenchmark, not whole-compilation or application overhead and not a controlled pooled performance estimate.

### Post-freeze review holdouts

The same artifact contains a separately checksummed four-operator holdout executable covering missing Bytecode producer identity, missing source-node identity, repeated pipeline occurrence and extension-provided verifier routing. The protocol and expected matrix were frozen before inspecting the workflow result. Cases remain outside the original primary and challenge denominators.

| Set | B0 | B1 | B2 |
|---|---:|---:|---:|
| Review-derived holdouts detected | 0/4 | 4/4 | 4/4 |
| Valid-control false positives | 0/20 | 0/20 | 0/20 |

These are post-freeze review-derived holdouts, not an externally authored or statistically representative unseen-fault sample. They provide bounded evidence against overfitting to the original corpus but do not establish external validity or general compiler correctness.

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

For the immutable pre-remediation baseline commit `92028b76b108822c5cdd41432721ac63c4e49b48`, aggregate run `30542053062` completed successfully. On review-remediation branch head `77edfb05047c933665912015af6d242a3a9f7fed`, `.NET CI`, validation, docs, published-package smoke, rollout smoke, benchmark smoke, contract experiment and package-compatibility review all completed successfully. Final master authority still requires the post-merge aggregate.

## Package and release boundary

The review-remediation package matrix contains nine identities.

Core SDK/template family `0.3.0-alpha.3`:

- `UniversalToolchain.Language.Abstractions`;
- `UniversalToolchain.FeatureSdk`;
- `UniversalToolchain.LanguageSdk`;
- `UniversalToolchain.Runtime`;
- `UniversalToolchain.LanguageAuthoring`;
- `UniversalToolchain.Testing`;
- `UniversalToolchain.Templates`.

Additional packages:

- `UniversalToolchain.Wist.LanguagePack` `0.3.0-alpha.4`;
- `UniversalToolchain.Wist` `0.1.0-alpha.5`.

Package Compatibility Review run `30580457427` built deterministic previous source/package baselines from reviewed commit `3abd958fae087e93135e88460ae0c0a2328afad5`, bound their exact hashes into the compatibility inputs and ran:

```bash ci-run=false
./build.sh --skip-docs \
  --baseline-source-archive /path/to/reviewed-previous-source.zip \
  --previous-package-bundle /path/to/reviewed-previous-packages.zip
```

The run succeeded: 1,551 tests, monotonic version provenance for all nine package identities, exact Wist API delta `removed=0, added=0`, package matrix 9/9, clean facade consumer, incompatible-checkout rejection, template smoke, cross-package consumer and release-integrity mutants. Artifact `package-compatibility-review-ea2b99b6edf1ea171f2c6932531d2870f5a2ec0d` has artifact ID `8774539955`, digest `sha256:6cfa9d197548887ef8a80e701900d936c4364aa849e70e9c2b8ed420adfd3a7b`. Its 27-entry outer checksum manifest, both baseline hashes and release-integrity root verify after extraction. The release-integrity root is `7069c53758e1735ecf41813d65782dcd76f2d22f8e00e8c193bc42ee50a1457a` and covers ten current package artifacts, including the Wist symbols package.

The published-package smoke remains intentionally pinned to actually published `UniversalToolchain.Wist` `0.1.0-alpha.1`; the review-remediation package versions are validated candidates, not a claim that they were published to NuGet.org.

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
