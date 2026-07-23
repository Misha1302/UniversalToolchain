---
title: Wist 0.1.0-alpha.1 Stability
description: Public package contract and relationship to the generic framework alpha.
---

# Wist 0.1.0-alpha.1 stability

`UniversalToolchain.Wist` `0.1.0-alpha.1` is suitable for controlled evaluation and prototypes. It is not a stable 1.0 contract.

## Reviewed first-contact surface

- `WistEngine`;
- `CreateRestrictedArithmetic` and `CreateFullNative`;
- `Validate`, `Evaluate`, `Compile<TDelegate>` and `TryCompile<TDelegate>`;
- structured diagnostics;
- typed program metadata and optimization report;
- public API baseline in `UniversalToolchain.Wist/PublicAPI.Shipped.txt`.

## Relationship to the generic SDK

Backend-neutral artifact/session contracts and a generic external Language Authoring stack are implemented. They are separate low-level alpha packages, not part of the stable promise of `UniversalToolchain.Wist` `0.1.0-alpha.1`.

## Not promised

- hardened sandboxing;
- compatibility with every future alpha;
- universal near-C# performance;
- generic language-authoring API stability through the Wist package;
- delegate validity after disposing every possible originating engine configuration.
