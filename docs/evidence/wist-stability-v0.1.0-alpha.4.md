---
title: Wist 0.1.0-alpha.4 Stability
description: Public package contract after runtime-boundary and release-gate hardening.
---

# Wist 0.1.0-alpha.4 stability

`UniversalToolchain.Wist` `0.1.0-alpha.4` is suitable for controlled evaluation and prototypes. It is not a stable 1.0 contract.

## Reviewed first-contact surface

- `WistEngine`;
- the exact nine-preset catalog;
- `Validate`, `Evaluate`, `Compile<TDelegate>` and `TryCompile<TDelegate>`;
- stable CLR numeric arguments/results across isolated runtime contexts;
- structured diagnostics, typed metadata and optimization reports;
- exact public API baseline and classified deltas.

## Hardening guarantees added in alpha.3

- disposed engines release collectible runtime contexts, including one-shot evaluation paths;
- implementation-owned `RealNumberImpl` values do not escape through `object` results;
- CLR numeric parameters are adapted for NumbersModule-backed presets;
- trusted CLR interop converts through explicit user-defined numeric bridges on CIL and interpreter backends;
- canonical tests enforce exact TRX counts, outcomes and per-entry timeouts;
- packaged assemblies are byte-bound to compiler outputs and presets/manifests to reviewed SHA-256 values;
- semantic preset, assembly-identity, API-delta and test-contract mutants are rejected;
- the Wist LanguagePack contains package README metadata.

## Not promised

- OS/process sandboxing;
- compatibility with every future alpha;
- universal near-C# performance;
- stable generic language-authoring APIs through the Wist facade.

## Release identity

This artifact uses a new package version because its package closure changed. The canonical release gate requires the real previous package bundle and rejects version reuse.
