---
title: Maintainer and Release Guide
description: Keep public documentation, package evidence, manifests and clean artifacts synchronized.
audience: maintainer-release-engineer
status: current
lastVerifiedAgainst: wist-release-state-2026-08-12
---

# Maintainer and release guide

This page is the public entry point for release maintenance. Repository-only policies remain under `internal-docs/` and are not published by VitePress.

## Sources of truth

| Concern | Canonical owner |
|---|---|
| current architecture | `docs/CURRENT_ARCHITECTURE_STATUS.md` plus implementation/tests |
| public trust boundary | `docs/SECURITY.md` and `docs/limitations.md` |
| coding and architecture policy | `internal-docs/policies-and-reports/PROJECT_RULES.md` and `ARCHITECTURE_RULES.md` |
| documentation authority | `internal-docs/policies-and-reports/DOCUMENTATION_INDEX.md` and `DOCUMENTATION_RULES.md` |
| published/source Wist release state | `eng/documentation-release-state.json` plus `UniversalToolchain.Wist.csproj` |
| package matrix | `eng/package-projects.txt` and package project metadata |
| exact test matrix | `eng/test-counts.json` |
| pinned verified results | `VERIFICATION.md` and `docs/evidence/` |
| source-tree integrity | Git commit/tree identity |

`eng/documentation-release-state.json` distinguishes the package verified from NuGet.org from the newer source candidate. It binds the target framework, first-contact install pages, package README, published-package smoke workflow and active Wist stability record.

## Documentation change gate

Run from the repository root:

```bash
npm run docs:status
npm run docs:links
npm run docs:build
```

The checks cover:

- public/internal split;
- role-specific navigation targets;
- Markdown links and anchors;
- root README local links and release commands;
- repository paths in current public/maintainer documents;
- orphan public pages;
- required current pages and front matter;
- published/source version synchronization;
- active stability-record existence;
- contributor build commands that must not accidentally enter the release package gate;
- VitePress compilation.

The canonical build runs the established documentation-status mutants, while `Docs Check` also runs the release-state mutant suite. Together they reject source-version drift, published-install drift, missing stability evidence, unsafe source build commands and a workflow-local published-version literal.

A green documentation build does not validate every C# example. When a public snippet changes an API contract, build a clean consumer or add a focused executable test.

## Contributor build versus release packaging

Normal source validation must disable packaging unless the reviewed release inputs are available:

```bash ci-run=false
./build.sh --skip-docs --skip-pack
```

Release packaging is intentionally fail-closed. It requires both the reviewed previous source archive and previous package bundle:

```bash ci-run=false
./build.sh \
  --baseline-source-archive /path/to/previous-source.zip \
  --previous-package-bundle /path/to/previous-packages.tar.gz
```

Do not document `./build.sh`, `./build.sh --skip-docs` or `./build.ps1 -SkipDocs` as ordinary contributor commands. Without `--skip-pack`/`-SkipPack`, those commands enter the baseline-bearing release gate by design.

## Release evidence gate

Before updating verification claims:

1. record the exact source artifact or commit;
2. run the canonical build without weakening warnings or tests;
3. record each test-project total, failures and skips;
4. pack every project in `eng/package-projects.txt` using reviewed baseline inputs;
5. verify package IDs, versions and dependency closure;
6. build the clean Wist consumer;
7. build the clean cross-package Language SDK consumer;
8. install and run the `ut-language` template from produced packages;
9. run documentation and release-state checks;
10. verify detached package integrity from a clean unpack;
11. publish only the intended artifacts;
12. run the clean-room NuGet.org smoke for the exact published version.

A verification page must identify its exact commit/artifact and status as a pinned snapshot. A historical test count must not be presented as live HEAD state.

## Promoting a Wist candidate to published

The source project version and published version are allowed to differ. To promote a candidate:

1. publish the exact reviewed package artifact;
2. confirm the NuGet.org package identity;
3. update `publishedVersion` in `eng/documentation-release-state.json`;
4. run `.github/workflows/published-package-smoke.yml` against that value;
5. update wording only where the candidate/published distinction changed;
6. keep historical stability pages immutable;
7. run release-state mutants before merging.

Do not copy version literals into workflow `env`, README snippets or navigation independently. The release-state checker must remain the single synchronization gate.

## Public/internal movement

When moving a document between `docs/` and `internal-docs/`:

- update all Markdown links;
- update inline repository paths in current contributor/maintainer pages;
- update VitePress navigation;
- update `DOCUMENTATION_INDEX.md`;
- remove public search stubs unless a real redirect is required;
- run the orphan/path checker before packaging.

Reviews, proposals and talks must not appear in the public source tree merely because they are useful repository context.

## Package version synchronization

The unchanged public package identities remain at `0.3.0-alpha.4`. The architecture/production-hardening payloads use `UniversalToolchain.LanguageSdk` and `UniversalToolchain.Runtime` `0.3.0-alpha.5`, `UniversalToolchain.Wist.LanguagePack` `0.3.0-alpha.6`, and `UniversalToolchain.Wist` `0.1.0-alpha.7`. These are source-candidate identities, not a publication statement.

<!-- package-matrix:begin -->
| Package ID | Version |
|---|---|
| `UniversalToolchain.Language.Abstractions` | `0.3.0-alpha.4` |
| `UniversalToolchain.FeatureSdk` | `0.3.0-alpha.4` |
| `UniversalToolchain.LanguageSdk` | `0.3.0-alpha.5` |
| `UniversalToolchain.Runtime` | `0.3.0-alpha.5` |
| `UniversalToolchain.LanguageAuthoring` | `0.3.0-alpha.4` |
| `UniversalToolchain.Testing` | `0.3.0-alpha.4` |
| `UniversalToolchain.Templates` | `0.3.0-alpha.4` |
| `UniversalToolchain.Wist.LanguagePack` | `0.3.0-alpha.6` |
| `UniversalToolchain.Wist` | `0.1.0-alpha.7` |
<!-- package-matrix:end -->

For generic package migrations, follow [Package Versioning and Migrations](/language-authoring/versioning-and-migrations).

## Clean artifact checklist

- no `bin/`, `obj/`, `node_modules/`, caches, local feeds, logs or secrets;
- no generated documentation output unless explicitly part of the release;
- one top-level directory;
- safe relative archive paths;
- detached package-integrity manifest regenerated after all intended changes;
- manifest checked from a clean extraction;
- archive SHA-256 published beside the archive;
- final diff confirms unrelated production code was not changed by documentation-only work.
