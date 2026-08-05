---
title: Wist 0.1.0-alpha.6 Candidate Stability
description: Public source-candidate contract after deterministic runtime-boundary hardening.
audience: wist-application-developer
status: source-candidate
lastVerifiedAgainst: runtime-boundary-candidate-2026-08-03
---

# Wist 0.1.0-alpha.6 candidate stability

`UniversalToolchain.Wist` `0.1.0-alpha.6` is the current source/release candidate for controlled evaluation and prototypes. It is **not published on NuGet.org** and is not a stable 1.0 compatibility contract. The package currently exercised from NuGet.org remains `0.1.0-alpha.1`.

## Reviewed first-contact surface

- `WistEngine`;
- restricted arithmetic and broad native presets;
- `Validate`, `Evaluate`, `Compile<TDelegate>` and `TryCompile<TDelegate>`;
- stable CLR numeric arguments/results across isolated runtime contexts;
- structured diagnostics, typed metadata and optimization reports;
- exact public API baseline and classified deltas.

## Runtime-boundary changes in alpha.6

- process-history-dependent default-context sharing was removed;
- the host registers shared contract assemblies through an immutable snapshot;
- full assembly identity and fail-closed SHA-256 validation bind configured shared copies;
- implementation assemblies remain in collectible isolated load contexts unless explicitly registered;
- hidden application-assembly/default-context fallback is forbidden;
- preload-order, hostile same-name, concurrency, disposal and unload regressions cover the boundary;
- public results normalize implementation-owned numeric values to stable CLR categories;
- package metadata and active documentation are checked against project and package identities.

## Verification boundary

The candidate package matrix contains `UniversalToolchain.Wist` `0.1.0-alpha.6` and `UniversalToolchain.Wist.LanguagePack` `0.3.0-alpha.5`. The exact repository test contract is 1,597 passed, 0 failed and 0 skipped for the pinned verification baseline. Package publication remains a separate operation and was not performed by the candidate verification work.

See the pinned [verification snapshot](/evidence/current-verification) for commit/run identities and the [Maintainer and Release Guide](/evidence/maintainer-guide) for the baseline-bearing package gate.

## Not promised

- publication of `0.1.0-alpha.6` on NuGet.org;
- OS/process sandboxing;
- compatibility with every future alpha;
- binary compatibility with pre-hardening runtime assemblies;
- universal near-C# performance;
- complete SSA coverage or an SSA-native backend;
- stable generic language-authoring APIs through the Wist facade.

## Promotion rule

Do not describe this candidate as the published package until:

1. the exact package artifact passes the release gates;
2. the package is actually published;
3. the clean-room NuGet.org smoke succeeds for `0.1.0-alpha.6`;
4. `eng/documentation-release-state.json` promotes `publishedVersion`;
5. public installation commands and evidence links pass the release-state mutants.
