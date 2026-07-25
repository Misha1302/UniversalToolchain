# Wist2 language-authoring and PlanFuzz integration verification record

## Environment

- Validation date: 2026-07-25.
- Target environment: GitHub Actions Ubuntu 24.04, Linux x64.
- SDK policy: `UniversalToolchain/global.json` with .NET 10 feature-band roll-forward.
- Canonical command: `./build.sh --skip-docs`.
- Package dependency mode: repository `NuGet.config`; the optional repository-local feed is materialized as an empty directory in clean checkouts before restore.

The recursive manifest is verified independently from generated build outputs. `MANIFEST.sha256` covers every tracked source file except the manifest itself.

## Integrated contracts

The repository keeps the P0/P1 language-authoring contracts and adds PlanFuzz as a non-packable research layer:

- `build.sh`, `build.ps1` and validation workflows use the same `eng/test-projects.txt` and `eng/package-projects.txt` contracts;
- the canonical package matrix builds and validates all eight SDK/template packages plus the Wist facade;
- package validation checks filename/nuspec identity, package-family dependency versions, embedded Wist manifest identity and template contents;
- clean-room package smoke installs `ut-language`, generates a dotted-name project and runs an independently packaged cross-package language;
- authored runtime catalogs store factories and registrations rather than mutable component instances;
- `PerSession` is the safe default lifetime, while `SingletonStateless` requires an explicit stateless marker and rejects disposable instances;
- runtime sessions own per-session components and release synchronous/asynchronous resources in reverse construction order;
- typed and untyped artifact contracts never connect through wildcard compatibility;
- Wist compatibility artifacts carry explicit protocol identities while remaining isolated behind the Wist compatibility provider;
- manifest and lock schema v5 use `universaltoolchain-json-v1` canonicalization and SHA-256 over compact UTF-8 bytes without platform-dependent line endings;
- PlanFuzz core remains language-neutral, while Acme and Wist own their structured generators and execution adapters;
- PlanFuzz observations are typed and replayed in fresh worker processes with timeout, process-tree termination and bounded output capture;
- clean, confirmed, flaky, inconclusive and infrastructure outcomes remain distinct;
- confirmed violations require complete oracle evidence and a stable exact fingerprint across repeated attempts;
- known Wist regression cases are opt-in and are not injected into default discovery campaigns;
- normalized finding classes are triage groups, not unique-defect or root-cause identities.

The retained product boundary is unchanged: this remains a contribution/pass/route/runtime SDK rather than a declarative grammar, binder or type-system workbench. PlanFuzz is experimental research tooling and is not part of the public NuGet surface.

## Canonical integration gate

Command:

```bash ci-run=false
./build.sh --skip-docs
```

Observed result on the hardened PlanFuzz Phase 3a surface-evidence revision:

- both `UniversalToolchain/Wist.sln` and the configuration-complete `UniversalToolchain/PlanFuzz.sln` restored and built;
- build warnings: **0**;
- build errors: **0**;
- `samples/Acme.PricingLanguage` and `samples/Wist.RolloutScoring` restored and built through the canonical path;
- all test projects declared by `eng/test-projects.txt` executed;
- all projects declared by `eng/package-projects.txt` were packed and validated;
- template and external cross-package consumer smoke checks passed.

Build servers and MSBuild node reuse are explicitly disabled by the canonical scripts to prevent orphaned workers in constrained and CI environments.

## Regression tests

| Test project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| `Tests` | 483 | 0 | 0 |
| `UniversalToolchain.Modules.Tests` | 290 | 0 | 0 |
| `UniversalToolchain.Dialects.Tests` | 588 | 0 | 0 |
| `UniversalToolchain.LanguageSdk.Tests` | 53 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.Tests` | 41 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.IntegrationTests` | 10 | 0 | 0 |
| **Total** | **1,465** | **0** | **0** |

The PlanFuzz suites include dedicated coverage for:

- deterministic PRNG and testcase identity;
- observation and replay serialization compatibility;
- backend, route, plan, fallback and canonical-lock oracle behavior;
- strict replay confirmation in fresh processes;
- incomplete evidence remaining inconclusive rather than clean or flaky;
- different exact violation fingerprints remaining flaky rather than confirmed;
- Wist Level 0 backend/configuration fail-closed validation;
- default discovery generation remaining separate from the opt-in Wist regression corpus;
- Acme rejecting a Wist-only regression-corpus option;
- seeded-fault detection without counting the seeded implementation as a product defect;
- `0 * x` preserving backend parity without consuming part of the external-load sequence;
- `(0 * 1) - 1` preserving the `System.Int32` contract;
- `x + (-2)` retaining the external-load descriptor and completing the required SSA route.

The language-SDK suite continues to cover cross-package route execution, exact package-manifest binding, pass ordering, feature/capability isolation, executor selection, lifecycle ownership, disposal, strict artifact compatibility and schema-v5 canonicalization.

## Package and consumer gate

The canonical matrix produced and verified **9** packages.

SDK/template family `0.3.0-alpha.1`:

- `UniversalToolchain.Language.Abstractions`;
- `UniversalToolchain.FeatureSdk`;
- `UniversalToolchain.LanguageSdk`;
- `UniversalToolchain.Runtime`;
- `UniversalToolchain.LanguageAuthoring`;
- `UniversalToolchain.Testing`;
- `UniversalToolchain.Templates`;
- `UniversalToolchain.Wist.LanguagePack`.

Facade:

- `UniversalToolchain.Wist` `0.1.0-alpha.1`.

Observed package checks:

- exact package set: **9/9**;
- Wist facade surface: **1 compile DLL, 64 runtime DLLs**, within the declared ceiling;
- Wist LanguagePack embedded descriptor: schema v5, `universaltoolchain-json-v1`, SHA-256, matching package ID/version;
- clean `dotnet new ut-language -n Contoso.RuleLanguage` restore/run result: `42`;
- clean external cross-package NuGet consumer result: `cross-package-consumer: 42`.

## Documentation and workflow gates

GitHub Actions separately enforces:

- documentation status, links, anchors, public/internal split and VitePress build;
- runnable Markdown Bash blocks;
- the Wist rollout sample output contract;
- upload of the canonical build log for actionable failures;
- equality between the checked-in recursive manifest and a freshly generated candidate.

The workflow-only manifest refresh path is explicit `workflow_dispatch`; no one-shot bootstrap trigger remains in the normal CI path.

## PlanFuzz evidence boundary

Verified:

- language-neutral core with Acme and Wist adapters;
- seven generic oracle families, including order-independent O-004 negative-surface preservation and structurally oriented O-005 extension noninterference;
- observation schema v4 with fail-closed surface/owner evidence contract v2 and typed trace completeness;
- fresh-process replay and campaign artifact manifests;
- strict evidence-completeness semantics;
- opt-in regression corpus;
- separate exact and class fingerprints;
- owner-layer fixes and direct regressions for the historical behaviors tracked by #302, #303 and #307;
- adapter-owned structured program reduction and generic plan-contract/variant pruning;
- fresh-process acceptance only when the original exact fingerprint is preserved;
- auditable rejection of clean, flaky, inconclusive and infrastructure candidates;
- test-owned runtime-provider seeded faults for canonical SF-005 and SF-011, confirmed through ordinary observed activation evidence in three fresh processes.

The preserved 25-case Wist pilot included the regression corpus. Its 21 violating cases and two normalized classes are retained as historical evidence and are not presented as clean discovery yield or as a root-cause count. Post-fix discovery-only and regression-inclusive smokes each completed three cases with three fresh-process attempts per case and reported only clean outcomes. An expanded discovery-only stability smoke completed 50 Acme cases with two attempts and 20 Wist cases with one attempt, also with only clean outcomes.

Not yet claimed:

- completion of Phase 3 lifecycle/session/concurrency schedules or schedule reduction;
- lifecycle and concurrency campaigns;
- equal-budget baseline superiority;
- a third external adapter;
- publication novelty or acceptance likelihood;
- stable 1.0 API compatibility, hostile-extension sandboxing, production workload certification or new performance superiority.

## Artifact gate

Before a release archive is produced, the tree must be cleaned of `bin`, `obj`, `artifacts`, generated VitePress output, `node_modules`, `.git`, IDE metadata, test outputs, caches and secret-like files. `CHANGELOG.md` remains excluded from this bundle family.

A recursive SHA-256 manifest is generated for every retained file except the manifest itself. Release archives must be extracted to a new directory and checked for manifest equality, forbidden paths and clean restore/build/run behavior before delivery.
