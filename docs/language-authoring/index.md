---
title: External Language Authoring
description: Public alpha path for independent non-Wist language packages.
audience: external-language-author
status: current-alpha
lastVerifiedAgainst: language-authoring-p0-p1-hardening-2026-07-23.1
---

# External Language Authoring

The Language Authoring SDK is the generic framework surface for building a language that does not depend on Wist. A language author provides typed artifact kinds, package-owned features and contributions, runtime component factories, enabled backends and a runtime policy. The SDK resolves them into an immutable executable `LanguagePlan`.

## What is implemented

- `LanguagePackageBuilder` derives descriptor metadata and runtime component registrations from one typed source;
- `LanguageDefinitionBuilder` selects features, backends, entry artifacts, providers, overrides and policy;
- `LanguageCompiler` resolves dependencies, conflicts, slots, capabilities and artifact routes;
- `LanguageRuntime` validates provider identity and policy, creates a runtime session and executes the selected backend route;
- `LanguageContractSuite` provides a backend-parity test helper;
- `dotnet new ut-language` creates a standalone non-Wist project;
- `samples/Acme.PricingLanguage` demonstrates a parser, interpreter backend and compiled backend without a Wist reference.

## Read in this order

1. [Quickstart](/language-authoring/quickstart)
2. [Package and contribution model](/language-authoring/package-model)
3. [Planning and diagnostics](/language-authoring/contribution-planning)
4. [Typed artifact routing](/language-authoring/artifact-routing)
5. [Runtime lifecycle and policy](/language-authoring/runtime-lifecycle)
6. [Testing, template and release boundary](/language-authoring/testing-and-templates)
7. [Package versioning and migrations](/language-authoring/versioning-and-migrations)
8. [Deep architecture reference](/architecture/external-language-authoring-sdk)

## Current boundary

This is a low-level alpha framework API, not yet a complete language workbench. The author still owns syntax representation, parsing, binding, semantic model, transformations and backend execution logic. The SDK composes and validates these components; it does not generate them from a grammar or semantic specification.
