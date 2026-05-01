---
title: Module Composition
description: Explain how modules combine into a language.
---

# Module Composition

Module composition is the mechanism that turns selected language features into a runnable DSL.

A module is not just a label in a dialect file. A real module may contribute lexer rules, parser nodes, AST visitors, bytecode/AIR lowering, optimizer support or backend behavior. A dialect selects the modules that are allowed to participate in a specific runtime host.

## When to read this page

Read this page after [Dialect Files](/build-dsls/dialect-files) and [Minimal DSL](/build-dsls/minimal-dsl) when you want to understand why adding or removing one module changes what the language can parse and execute.

## Goal

Understand how modules combine into a language surface and which mistakes break modularity.

## Composition model

A simplified composition flow looks like this:

```text
.wistdialect
  -> dialect compiler
  -> selected module aliases
  -> manifest/runtime resolution
  -> deterministic module order
  -> frontend/core/backend services
  -> execution host
```

The dialect file is the user-facing selection layer. Runtime manifests and DI registration resolve the selected aliases to implementation types. The final host should be deterministic: the same dialect and repository state should produce the same runtime composition.

## What a module can own

A module may own one or more of these responsibilities:

| Responsibility | Example |
|---|---|
| Lexemes | numeric literals, operators, keywords |
| Parser behavior | expression nodes, loop syntax, condition syntax |
| AST nodes | typed representation of a syntax construct |
| AST translation | conversion from AST to bytecode/AIR |
| Bytecode/AIR shape | semantic tags, stack operations, intermediate instructions |
| Optimizer support | arithmetic folding, native lowering, backend-specific transformations |
| Backend behavior | interpreter execution or CIL emission for the owned semantics |

The boundary matters. If arithmetic owns arithmetic operators, another module should not parse arithmetic syntax by scanning raw source text. If variables own variable binding, backend code should not silently reinterpret unrelated identifiers as runtime arguments.

## Minimal example

This dialect:

```text
dialect MinimalArithmetic
use Arithmetic,Numbers,Scopes,Whitespaces
backend interpreter
```

is a composition of four capabilities:

- `Numbers` provides numeric values.
- `Arithmetic` provides arithmetic operations.
- `Scopes` provides expression/block structure needed by the parser/runtime path.
- `Whitespaces` keeps lexical handling usable for normal source text.

It can run:

```wist
2 + 3 * 4
```

but it should not accept:

```wist
let x = 2
x + 1
```

unless `Identifier` and `Variables` are also selected.

## Dependency discipline

Some modules are meaningful only with related modules. For example:

- `Variables` usually requires `Identifier`.
- conditional syntax usually requires comparison/equality/boolean support, depending on the exact program shape;
- native/CIL-oriented optimizations require backend support;
- unsafe interop should not appear in a restricted user-facing formula DSL.

The documentation should not pretend that module composition is magic. A dialect author is responsible for selecting a coherent set of capabilities and testing it.

## Deterministic ordering

Composition must be deterministic because parser priority, AST visitors, optimizers and backend services can depend on order.

Good composition behavior:

- aliases resolve to the same implementation types every time;
- parser priorities are explicit;
- module ordering is either declared, derived deterministically or tested;
- tests fail when an unexpected module appears in the runtime surface.

Bad composition behavior:

- relying on reflection enumeration order;
- adding a hardcoded special case for a module instead of fixing composition;
- treating backend IDs or module aliases as magic strings scattered across unrelated files;
- using global mutable registries that change between test runs.

## Composition vs. security

A restricted dialect reduces the available language surface. That is useful, but it is not the same thing as process isolation.

Use restricted dialects to say:

```text
This formula language does not include variables, loops or unsafe interop.
```

Do not use restricted dialects alone to say:

```text
This is safe for arbitrary untrusted third-party code in the same process.
```

For untrusted third-party code, combine dialect restriction with process, environment and resource isolation.

## Common mistakes

- Adding a module because a test passes, without documenting what syntax it enables.
- Starting from `full-default` for a production formula DSL and forgetting to remove unnecessary features.
- Enabling an optimizer before the unoptimized path is correct.
- Depending on one backend while documenting the dialect as backend-neutral.
- Adding marker-only capabilities that imply behavior without implementation ownership.
- Hiding missing module dependencies behind fallback parser behavior.

## Practical checklist

Before accepting a dialect composition, verify:

- the dialect file lists only intended modules;
- omitted syntax is rejected;
- selected backends match the intended runtime modes;
- compiler and interpreter results match when both are enabled;
- unsafe interop is absent from user-authored restricted DSLs;
- optimizer support is tested separately from base semantics.

## Next

Continue with [Backend Selection](/build-dsls/backend-selection).
