---
title: Current Verification
description: Verification record for the language-authoring hardening baseline and this documentation revision.
---

# Current verification

## Project baseline

The documentation revision starts from:

```text
Artifact: Wist2-language-authoring-p0-p1-hardening-2026-07-23.1(1).zip
```

The embedded `VERIFICATION.md` records a Linux x64 offline verification using .NET SDK 10.0.301 and local package sidecars.

## Recorded project result

```text
85 / 85 solution projects built
1,411 tests succeeded
0 failed
0 skipped
9 NuGet packages checked
clean template consumer smoke passed
cross-package package consumer smoke passed
```

Per-project test counts:

| Project | Passed |
|---|---:|
| `Tests` | 482 |
| `UniversalToolchain.Modules.Tests` | 288 |
| `UniversalToolchain.Dialects.Tests` | 588 |
| `UniversalToolchain.LanguageSdk.Tests` | 53 |

This page reports the supplied artifact's recorded verification. The root `VERIFICATION.md` is the detailed authority for commands, environment and package checks.

## Documentation revision checks

The revised documentation archive must pass from a clean extraction:

```bash ci-run=false
npm ci --no-audit --no-fund
npm run docs:status
npm run docs:links
npm run docs:build
```

The release handoff also validates recursive manifest integrity, archive path safety, generated-output exclusion and absence of production-code changes relative to the supplied baseline.
