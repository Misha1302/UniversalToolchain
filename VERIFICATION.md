# Wist2 verification and research-evidence record

## Environment and authority

- Record refreshed: 2026-08-12.
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
- deterministic language/package planning, schema-v6 locks and exact package-manifest binding;
- exact `(contribution, backend, input contract)` executor resolution;
- strict selected-module contract enforcement in the Wist composition path;
- production Bytecode metadata reading plus declared/observed emission verification;
- mandatory producer-module and source-node identity for every contract-annotated Bytecode emission;
- AIR structural, stack and backend-capability verification;
- compiler facts/effects and fail-closed routing of unresolved reverification requests;
- extension-provided compiler-fact verifier routes with conflict rejection;
- deterministic four-policy semantic-verification scheduling with structural and semantic AIR scopes kept distinct;
- explicit rejection of repeated module identities until occurrence-sensitive effects are modeled;
- `PerSession` ownership and explicit `SingletonStateless` lifecycle rules;
- primary-first preservation of runtime construction failures when cleanup also fails;
- typed separation of execution-only `LanguageRuntime` and artifact-capable `LanguageBuildRuntime`;
- active-lease tracking that ignores completed leases retained by flowed execution contexts;
- Wist expected-failure taxonomy distinct from infrastructure/internal faults;
- explicit Wist source-retention and diagnostic-exposure policies;
- explicit non-concurrent same-instance `WistEngine` contract;
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
| `Tests` | 524 | 0 | 0 |
| `UniversalToolchain.Modules.Tests` | 292 | 0 | 0 |
| `UniversalToolchain.Dialects.Tests` | 261 | 0 | 0 |
| `UniversalToolchain.LanguageSdk.Tests` | 161 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.Tests` | 41 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.IntegrationTests` | 10 | 0 | 0 |
| **Total** | **1,289** | **0** | **0** |

The hardening delta is 13 targeted tests: eight Wist facade failure/privacy/concurrency regressions and five runtime construction/capability regressions. The exact manifest is owned by `eng/test-counts.json`; provider-backed exact-head results are recorded against their commit/run identities rather than inferred from an older local TRX snapshot.

## Production-boundary contract experiment

The dedicated `Contract Experiment` workflow restores and builds the non-packable experiment against the exact current production API before collecting evidence.

### Compared modes

- **B0:** structural AIR and target-capability checks only.
- **B1:** B0 plus typed selection, ownership, Bytecode and facts/effects checks; unresolved reverification is recorded but not fail-closed.
- **B2:** B1 plus mandatory failure on unresolved reverification requests.

### Pre-remediation immutable baseline

The pre-remediation publication baseline is tied to master commit `92028b76b108822c5cdd41432721ac63c4e49b48`, workflow run `30542053093` and artifact ID `8759126014`, digest `sha256:e87ea19405cce64df8d76ea83fc3b02c5db5c7b83f435ee940d7ee2bb850209f`.

### Review-remediation master reproduction

Master Contract Experiment run `30585251945` executed on commit `2b0a4d1f0e255432daf0d5ddd485269b6490b67e`. Artifact `contract-experiment-2b0a4d1f0e255432daf0d5ddd485269b6490b67e` has ID `8776245456` and digest `sha256:ca1708b8054e63eb9fff0526f9113013a9569121890ff3b0ea19572e5c199961`. Independent extraction verified every entry in both internal checksum trees and both captured git-status files are empty.

The original frozen corpus remains unchanged: 40 primary instances representing 32 operator shapes, 10 challenge operators, three deterministic repetitions per instance/mode and 100 valid controls per mode.

| Set | B0 | B1 | B2 |
|---|---:|---:|---:|
| Primary operators detected | 12/32 | 28/32 | 32/32 |
| Challenge operators detected | 1/10 | 10/10 | 10/10 |
| Valid-control false positives | 0/100 | 0/100 | 0/100 |

On this frozen author-designed corpus, the paired B0-versus-B2 difference is 20/32 and exact McNemar is `p = 1.9073486328125e-06`; B1 versus B2 is four discordant operators and `p = 0.125`. These values describe this corpus and are not a population-level superiority claim.

The master artifact's five process-level timing replicates report isolated B2 boundary-kernel overhead of **46.0% median**, range **44.7%-57.1%**. Earlier identical functional runs reported materially different values. This confirms that the timing is an environment-sensitive verifier-kernel microbenchmark, not whole-compilation or application overhead and not a controlled pooled performance estimate.

### Post-freeze review holdouts

The same master artifact contains a separately checksummed four-operator holdout executable covering missing Bytecode producer identity, missing source-node identity, repeated pipeline occurrence and extension-provided verifier routing. The protocol and expected matrix were frozen before inspecting the workflow result. Cases remain outside the original primary and challenge denominators.

| Set | B0 | B1 | B2 |
|---|---:|---:|---:|
| Review-derived holdouts detected | 0/4 | 4/4 | 4/4 |
| Valid-control false positives | 0/20 | 0/20 | 0/20 |

These are post-freeze review-derived holdouts, not an externally authored or statistically representative unseen-fault sample. They provide bounded evidence against overfitting to the original corpus but do not establish external validity or general compiler correctness.

## Workflow contract

`eng/ci-required-workflows.json` is the single machine-readable owner for the code-acceptance workflow set and acceptable conclusions. `.github/workflows/ci-aggregate.yml` consumes that file; documentation must not maintain an independent required-workflow list as a semantic authority.

`CI aggregate` is fail-closed. For a required workflow, only `success` passes. `missing`, `failure`, `cancelled`, `timed_out`, `skipped` and `neutral` do not pass unless a future owner schema explicitly types a different contract and the checker is updated accordingly.

`Deploy documentation to GitHub Pages` is explicitly non-blocking for code acceptance. Documentation correctness is owned by `Docs Check`; deployment is a publication step. `CI contract check` validates the owner/aggregate consistency and kills mutants for required-workflow removal and fail-open conclusion drift.

Historical aggregate receipts remain evidence for the exact revisions that produced them; they do not override the current machine-readable owner.

## Package and release boundary

The current architecture/production-hardening candidate contains nine public package identities. Payload-bearing packages changed relative to merge #332 and therefore receive new monotonic prerelease identities instead of reusing the previous payload identity. Dependency metadata and template package references are treated as package payload, not ignored as incidental text.

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

The baseline-aware package gate verifies:

- monotonic version/content provenance for all package identities;
- project, filename and embedded `.nuspec` identity agreement;
- exact synchronization of active package matrices;
- Wist compile/runtime package-surface separation;
- exact public API delta classification;
- clean Wist consumer, template consumer and cross-package consumer;
- detached release-integrity manifest and mutation tests.

The package candidate is not published by verification. NuGet publication, merge and release remain separately authorized actions.

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
