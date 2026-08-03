---
title: Testing, Templates and Release Boundary
description: Parity tests, template workflow and alpha maturity of external language authoring.
---

# Testing, templates and release boundary

## Backend parity

Use `UniversalToolchain.Testing.LanguageContractSuite` when two backends implement the same observable contract:

```csharp
var result = LanguageContractSuite.RequireParity(
    runtime,
    "12.5 * 3 - 2.5",
    new BackendId("interpreter"),
    new BackendId("compiled"));
```

The default comparer compares string representations. Supply a domain-specific comparer for floating-point tolerance, structured values or effects.

## Minimum test matrix

A language package should test:

1. successful package and definition compilation;
2. missing feature/contribution diagnostics;
3. dependency and ordering cycles;
4. single-owner slot conflicts and explicit replacement;
5. ambiguous and preferred capability providers;
6. typed artifact mismatch rejection;
7. one route per enabled backend;
8. exact executor selection;
9. runtime policy rejection;
10. `PerSession` isolation and disposal;
11. interpreter/compiled parity where both backends claim the same semantics;
12. clean package restore from only the produced NuGet artifacts.

The repository tests these contracts in `UniversalToolchain.LanguageSdk.Tests`, including external authoring, typed contracts, architecture regressions, lifecycle and canonicalization.

## Template

The canonical template package version for this alpha is `0.3.0-alpha.4`. The generic SDK family is currently documented as produced release artifacts rather than as a guaranteed NuGet.org publication.

The packable `UniversalToolchain.Templates` project provides `ut-language`.

```bash ci-run=false
dotnet new install ./artifacts/packages/UniversalToolchain.Templates.0.3.0-alpha.4.nupkg
dotnet new ut-language -n Example.Language
```

Template token replacement covers package, language, feature, contribution, runtime and artifact identifiers. The release smoke verifies that generated projects do not retain `Acme.Pricing` or Wist identifiers.

## Package family

The language-authoring release matrix includes separate packages for abstractions, feature metadata, planning, authoring, runtime, testing, templates and the Wist language pack. Use the smallest set that owns your contract; do not reference Wist packages from an independent language unless you intentionally adapt Wist.

## Alpha boundary

Implemented now:

- typed low-level authoring;
- deterministic planning and schema-v5 lock snapshot;
- cross-package runtime assembly;
- route execution and exact backend selection;
- runtime policy validation and component lifecycle;
- independent non-Wist sample and template.

Not implemented as a high-level product surface:

- grammar generation;
- parser/binder DSL;
- automatic operation/type-system authoring;
- IDE/editor integration;
- compatibility guarantees for the generic API across all future alphas;
- hardened execution of hostile third-party packages.
