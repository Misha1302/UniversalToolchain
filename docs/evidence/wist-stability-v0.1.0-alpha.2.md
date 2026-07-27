---
title: Wist 0.1.0-alpha.2 Stability
description: Superseded public package contract.
statusReason: Superseded by Wist 0.1.0-alpha.3 boundary hardening.
navigation: hidden
---

# Wist 0.1.0-alpha.2 stability

`UniversalToolchain.Wist` `0.1.0-alpha.2` is superseded by `0.1.0-alpha.3`. It was suitable for controlled evaluation and prototypes. It is not a stable 1.0 contract.

## Reviewed first-contact surface

- `WistEngine`;
- `WistEngineOptions.FromPresetId(...)` and the exact nine-preset catalog;
- `Validate`, `Evaluate`, `Compile<TDelegate>` and `TryCompile<TDelegate>`;
- structured diagnostics;
- typed program metadata and optimization report;
- public API baseline in `UniversalToolchain.Wist/PublicAPI.Shipped.txt`.

## Relationship to the generic SDK

Backend-neutral artifact/session contracts and a generic external Language Authoring stack are implemented. They are separate low-level alpha packages, not part of the stable promise of `UniversalToolchain.Wist` `0.1.0-alpha.2`.

## Not promised

- hardened sandboxing;
- compatibility with every future alpha;
- universal near-C# performance;
- generic language-authoring API stability through the Wist package;
- process-security sandboxing: the isolated assembly-load boundary prevents accidental/runtime substitution but is not an OS sandbox.

## Hardening guarantees added in alpha.2

- one selected backend governs `Validate`, `Evaluate`, `Compile` and `TryCompile`;
- unsupported preset/backend combinations fail during `WistEngine.Create`;
- runtime implementation assemblies are root-authoritative and isolated in a collectible load context;
- the packaged preset set, managed assembly closure and runtime manifests are exact-gated;
- incompatible generation-1 runtime assemblies and hostile manifest/package mutations are rejected;
- all reviewed compatibility breaks are classified in `eng/wist-api-compatibility.csv`.
