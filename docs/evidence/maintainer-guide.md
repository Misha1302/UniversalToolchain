---
title: Maintainer and Release Guide
description: Keep public documentation, package evidence, manifests and clean artifacts synchronized.
audience: maintainer-release-engineer
status: current
lastVerifiedAgainst: language-authoring-p0-p1-hardening-2026-07-23.1
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
| package matrix | `eng/package-projects.txt` |
| test-project matrix | `eng/test-projects.txt` |
| verified results | `VERIFICATION.md` and `docs/evidence/` |
| recursive artifact integrity | `MANIFEST.sha256` |

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
- repository paths in current public/maintainer documents;
- orphan public pages;
- required current pages and front matter;
- VitePress compilation.

A green build does not validate every C# example. When a public snippet changes an API contract, build a clean consumer or add a focused executable test.

## Release evidence gate

Before updating current verification claims:

1. record the exact source artifact or commit;
2. run the canonical build without weakening warnings or tests;
3. record each test-project total, failures and skips;
4. pack every project in `eng/package-projects.txt`;
5. verify package IDs, versions and dependency closure;
6. build the clean Wist consumer;
7. build the clean cross-package Language SDK consumer;
8. install and run the `ut-language` template from the produced package;
9. run documentation checks;
10. verify `MANIFEST.sha256` from a clean unpack.

Update `VERIFICATION.md` and [Current Verification](/evidence/current-verification) together. A historical test count must not be labeled current after the tree changes.

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

The generic SDK/template family currently uses `0.3.0-alpha.1`; the Wist facade uses `0.1.0-alpha.1`. Do not duplicate version strings in additional tutorials without updating the documentation status checker or deriving them from one build metadata source.

For generic package migrations, follow [Package Versioning and Migrations](/language-authoring/versioning-and-migrations).

## Clean artifact checklist

- no `bin/`, `obj/`, `node_modules/`, caches, local feeds, logs or secrets;
- no generated documentation output unless explicitly part of the release;
- one top-level directory;
- safe relative archive paths;
- recursive manifest regenerated after all intended changes;
- manifest checked from a clean extraction;
- archive SHA-256 published beside the archive;
- final diff confirms that unrelated production code was not changed by documentation-only work.
