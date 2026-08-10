---
title: Wist Compiler Module Authoring
description: Introduce language module development.
audience: wist-compiler-contributor
status: current-wist-specific
lastVerifiedAgainst: language-authoring-p0-p1-hardening-2026-07-23.1
---

# Wist compiler module authoring

> **Status:** this is the supported Wist-specific extension path for the shipped reference language and its compiler modules. It is not deprecated. New independent non-Wist languages should start with [External Language Authoring](/language-authoring/) instead of inheriting Wist frontend contracts.


A module is the normal way to add syntax or semantics to the existing Wist frontend/compiler pipeline. This section gives the lifecycle for module authors without pretending that every extension point is already fully formalized.

## Recommended path

If you are new to module authoring, do not start from the reference pages. Use this order:

1. [Choose an Extension Type](/write-modules/choose-extension-type) — decide whether you need a module, a dialect, an optimizer, an intrinsic, or backend work.
2. [Create Your First Module](/write-modules/create-your-first-module) — build the small `TextualAddition` module from syntax idea to tests.
3. [Module Contracts](/reference/module-contracts) — make component identity, capabilities and verification contracts explicit.
4. [Frontend Module](/write-modules/frontend-module) — read the contract shape after seeing the end-to-end example.
5. [Testing a Module](/write-modules/testing-module) — expand the tutorial tests into a full module test matrix.

## When to read this section

Read this when existing modules are not enough and you need to add syntax, AST translation, bytecode/AIR behavior or backend support.

## Goal

Understand the expected path from syntax idea to tested language feature.

## Prerequisites

- Read [Mental Model](/start/mental-model).
- Read [Dialect Files](/build-dsls/dialect-files).
- Build the repository and run relevant tests before changing behavior.
- Review `docs/guides/module-authoring.md` and `docs/reference/module-contracts.md` before implementing a real module.

## Lifecycle

### 1. Define the feature boundary

A module should own one language feature or one tightly scoped set of related features. Examples of current runtime-visible modules include:

- `Numbers`
- `Arithmetic`
- `TextualAddition`
- `Identifier`
- `Variables`
- `Scopes`
- `Conditions`
- `ComparisonConditions`
- `BooleanConditions`
- `Equality`
- `Loops`
- `Labels`
- `Comments`
- `SemicolonAsNewLine`
- `CSharpInterop`
- `FunctionCalls`
- `SafeMathFunctions`
- `NativeTypes`

Do not add marker-only capabilities that imply runtime behavior without owning syntax and semantics.

### 2. Add lexer ownership

If the feature introduces syntax, the module should register lexemes through its frontend module implementation. The implementation should implement `IFrontendCoreModule`; its external alias/id is owned by the typed language package/catalog rather than a reflection attribute.

Example pattern:

```csharp
[DialectComponentContract("FrontendModule", "Arithmetic")]
[AutoRegisterService]
public class ArithmeticModuleImpl : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer) => lexer.AddLexemes(...);
}
```

Token names are part of the contract. Avoid magic strings scattered across unrelated files.

### 3. Add parser extension

If the feature changes syntax structure, register node creators through `InitParser`. Parser priority matters. Current modules use explicit priority values to control precedence and transformation order.

A parser extension must not mutate unrelated ancestor nodes or recover syntax by scanning raw source text. Syntax ownership belongs to lexer, parser, AST nodes, visitors or syntax-specific extractors built from parser output.

### 4. Add AST nodes and AST translation

A module usually adds AST visitors through `InitAstTranslator`. Visitors must self-filter correctly because the translator can give multiple visitors a chance to act on nodes.

The important rule is stack and ownership discipline: visitors should emit only the semantics they own and should not accidentally double-emit operations for another module.

### 5. Lower into bytecode/AIR

Bytecode/AIR is the intermediate layer used by backends and optimizers. If your feature creates new bytecode tags, new AIR shapes or new stack behavior, document the contract and add tests.

Do not rely on undocumented tag strings without a producer/consumer test.

### 6. Support backend behavior

A feature is not complete until the intended backend paths understand it. For language semantics, prefer tests that run the same source through both interpreter and CIL modes when both are available.

If a feature is intentionally backend-specific, document that limitation in the module reference and dialect examples.

### 7. Add dialect visibility

The module must be selectable through dialect files. This usually requires module alias/export metadata and manifest visibility. After that, a dialect can include it:

```text
use MyFeature,Numbers,Scopes,Whitespaces
```

Register the feature/contribution explicitly in the owning language package. Do not create runtime-discovery JSON as a substitute for typed registration.

### 8. Test the module

At minimum, add:

- a positive test for the feature;
- a negative test for invalid syntax or missing dependencies;
- parity tests when both compiler and interpreter modes are supported;
- dialect composition tests proving the module is available only when selected.

## Expected result

A finished module has a clear syntax owner, parser behavior, AST-to-bytecode/AIR lowering, backend behavior, dialect visibility and tests. A reader should be able to understand which dialect directive enables the feature and which backend paths support it.

## What happened internally

Module authoring touches several stages of the pipeline:

```text
module metadata
  → lexer registration
  → parser node creators
  → AST visitors
  → bytecode/AIR emission
  → optimizer/backend support
  → dialect/runtime selection
```

This is why module work requires both implementation tests and architecture discipline.

## Common mistakes

- Adding syntax recognition through raw-source string checks instead of parser/AST ownership.
- Choosing parser priorities by trial and error without tests.
- Adding compiler behavior but forgetting interpreter behavior, or the reverse.
- Depending on module ordering without documenting and testing it.
- Using global mutable state for feature coordination.
- Treating capabilities as behavior activation.
- Reintroducing rule declarations before the AST-owned rule declaration rewrite.
- Reintroducing runtime-discovery metadata instead of typed package/contribution registration.

## Next

Continue with [Choose an Extension Type](/write-modules/choose-extension-type), [Create Your First Module](/write-modules/create-your-first-module), [Module Authoring Guide](/guides/module-authoring), and [Module Contracts](/reference/module-contracts).
