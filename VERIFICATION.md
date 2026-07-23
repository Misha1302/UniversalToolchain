# Wist2 P0/P1 language-authoring hardening verification record

## Environment

- Validation date: 2026-07-23.
- Target environment: Linux x64.
- SDK used: .NET SDK 10.0.301 from the supplied offline sidecar.
- Repository SDK policy: 10.0.103 with `rollForward: latestFeature` and prerelease enabled.
- Source baseline: `Wist2-language-authoring-composition-hardening-2026-07-22.1.zip`.
- Package dependency mode: supplied local NuGet sidecars only; `NUGET_CERT_REVOCATION_MODE=offline` was used solely for isolated offline verification.

The baseline recursive manifest was checked before modification. The final handoff regenerates the manifest after generated outputs are removed and verifies it again from a clean extraction.

## Corrected P0/P1 contracts

This revision closes the P0/P1 findings from the post-hardening review:

- `build.sh`, `build.ps1` and both validation workflows use the same `eng/test-projects.txt` and `eng/package-projects.txt` contracts;
- the canonical test matrix contains all four test projects, including `UniversalToolchain.LanguageSdk.Tests`;
- the canonical package matrix builds and validates all eight SDK/template packages plus the Wist facade;
- package validation checks filename/nuspec identity, package-family dependency versions, embedded Wist manifest identity and template contents;
- clean-room package smoke installs `ut-language`, generates a dotted-name project and runs an independently packaged cross-package language;
- authored runtime catalogs store factories and registrations rather than mutable component instances;
- `PerSession` is the safe default lifetime, while `SingletonStateless` requires an explicit stateless marker and rejects disposable instances;
- runtime sessions own per-session components and release synchronous/asynchronous resources in reverse construction order;
- disposal is idempotent, waits for in-flight operations and rejects execution after disposal;
- per-session factories that reuse an instance across sessions fail closed;
- typed and untyped artifact contracts never connect through wildcard compatibility;
- the generic route runtime rejects legacy untyped executable routes before session construction;
- Wist compatibility artifacts carry explicit protocol identities while remaining isolated behind the Wist compatibility provider;
- manifest and lock schema v5 use `universaltoolchain-json-v1` canonicalization and SHA-256 over compact UTF-8 bytes without platform-dependent line endings;
- SDK/template package versions are `0.3.0-alpha.1`; the existing Wist facade remains `0.1.0-alpha.1`.

The retained boundary is unchanged: this remains a contribution/pass/route/runtime SDK rather than a declarative grammar, binder or type-system workbench.

## Canonical release gate

Command:

```bash ci-run=false
./build.sh --skip-docs
```

Observed result on the final source revision:

- solution restore succeeded from local package sources;
- documentation sample restore succeeded;
- **85/85 solution projects built**;
- build warnings: **0**;
- build errors: **0**;
- `samples/Wist.RolloutScoring` built separately with **0 warnings / 0 errors**;
- all test projects declared by `eng/test-projects.txt` executed;
- all projects declared by `eng/package-projects.txt` were packed and validated;
- template and external cross-package consumer smoke checks passed.

Build servers and MSBuild node reuse are explicitly disabled by the canonical scripts to prevent orphaned workers in constrained and CI environments.

## Regression tests

| Test project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| `Tests` | 482 | 0 | 0 |
| `UniversalToolchain.Modules.Tests` | 288 | 0 | 0 |
| `UniversalToolchain.Dialects.Tests` | 588 | 0 | 0 |
| `UniversalToolchain.LanguageSdk.Tests` | 53 | 0 | 0 |
| **Total** | **1,411** | **0** | **0** |

The language-SDK suite includes dedicated coverage for:

- cross-package route execution and exact package-manifest binding;
- same-artifact pass ordering and feature/capability isolation;
- exact executor selection and Wist compatibility fail-closed behavior;
- per-session mutable-state isolation;
- synchronous and asynchronous disposal ownership;
- idempotent disposal and execution-after-dispose rejection;
- reused per-session component rejection;
- explicit stateless-singleton requirements;
- strict typed/untyped route compatibility;
- schema-v5 canonical manifest and lock serialization;
- shared canonical test/package manifests.

## Package and consumer gate

The canonical matrix produced and verified **9** packages:

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

## Baseline documentation checks (supplied archive)

The following results belong to the supplied `2026-07-23.1` language-authoring hardening baseline before the documentation split described below.

- documentation status checker passed for **158 Markdown files**;
- all **55 Markdown bash blocks** executed successfully or were explicitly marked non-CI;
- independent non-Wist sample output: `35.0:35.0`;
- Wist rollout sample produced the documented scoring output;
- architecture documentation now records component lifetimes, disposal ownership, strict legacy-contract migration and schema-v5 canonical hashing.

## Final artifact gate

Before packaging, the release tree is cleaned of:

- `bin`, `obj`, `artifacts`, generated `.vitepress/dist` and `node_modules`;
- `.git`, IDE metadata, test outputs and caches;
- secret-like files;
- stale `MANIFEST.sha256`;
- `CHANGELOG.md`, which is excluded from this bundle family.

A recursive SHA-256 manifest is generated for every retained file except the manifest itself. The ZIP is then extracted to a new directory and checked for manifest equality, forbidden paths and an offline restore/build/run of the independent non-Wist sample.

## Evidence boundary

Verified: canonical build, all 1,411 tests, nine-package matrix, Wist package surface, template consumer, external cross-package NuGet consumer, runtime lifecycle isolation/disposal, strict typed contracts, schema-v5 canonicalization, documentation status and Markdown command execution.

Not claimed: stable 1.0 API compatibility, arbitrary grammar/binder/type-system authoring, hostile-extension sandboxing, production workload certification or new performance superiority.


## Documentation hardening delta (2026-07-23.2)

The documentation-only revision separates public developer documentation from internal reviews/proposals and adds a task-oriented Language Authoring route. No `.cs`, `.csproj`, `.sln`, `.props` or `.targets` file differs from the supplied baseline.

Independently executed in the revision workspace:

- `Tools/check_documentation_status.py`: **107 public Markdown files, 46 internal Markdown files**;
- `Tools/check_documentation_links.py`: public links, anchors, assets, sidebar targets and public/internal split passed;
- Python checker compilation, workflow YAML parsing and `build.sh` syntax validation passed;
- `samples/Acme.PricingLanguage` restored from the offline package cache, built with **0 warnings / 0 errors**, and produced `35.0:35.0`;
- VitePress configuration passed TypeScript syntax analysis; only the unavailable `vitepress` module remained unresolved in that isolated syntax check.

The VitePress HTML build was not rerun in the verification container because both the internal npm gateway and direct npm registry access were unavailable (`503` / DNS `EAI_AGAIN`). The checked-in CI gate remains `npm run docs:check`; it must run in an environment with the lockfile dependencies available before release publication.
