---
title: Testing a Module
description: Show module-level and parity tests.
---

# Testing a Module

A module is not complete when one example runs. It is complete when its syntax, composition boundary, runtime behavior and backend support are tested.

Module tests protect the project from silent architecture drift: hardcoded syntax, accidental full-profile dependencies, backend-only behavior and optimizer-specific semantics.

## When to read this page

Read this page before opening a PR that adds or changes a language feature module.

## Goal

Build a test set that proves the module works when selected and disappears when omitted.

## Test categories

A non-trivial module should usually have tests for:

| Test category | What it proves |
|---|---|
| Lexer/parser ownership | syntax is recognized by the owning module |
| Positive execution | a valid program runs |
| Negative syntax | malformed source fails deterministically |
| Dialect selection | the feature exists only when selected |
| Backend availability | unsupported modes fail explicitly |
| Backend parity | compiler and interpreter agree when both are supported |
| Optimizer safety | optimizers preserve behavior |
| Composition determinism | module order and runtime surface are stable |

Do not collapse all of these into one smoke test.

## Positive execution

Start with the smallest valid example.

For variables:

```wist
let x = 10
x
```

Expected result:

```text
10
```

Then add a realistic example:

```wist
let x = 5
x = x + 1
x
```

Expected result:

```text
6
```

The first test proves declaration and lookup. The second test proves mutation.

## Negative syntax

Every parser feature needs malformed input tests.

Examples:

```wist
let
```

```wist
while
```

```wist
if 2 == 2 (1)
```

The exact expected diagnostic may change while internals evolve. For high-level module tests, it is often enough to assert deterministic failure in both supported backends, unless a stable diagnostic contract already exists.

## Dialect selection tests

A module should be selectable and omittable through dialect composition.

If `Variables` is omitted, this should not behave like valid variable syntax:

```wist
let x = 1
x
```

If `Loops` is omitted, loop syntax should not be available. If `CSharpInterop` is omitted, interop expressions should not be available.

This prevents restricted dialects from accidentally growing into full Wist.

## Backend parity tests

When both interpreter and compiler are available, run the same source through both and compare observable results.

Useful parity cases:

```wist
2 + 3 * 4
```

```wist
let x = 5
x = x + 1
x
```

```wist
if 2 == 2 (1) else (2)
```

```wist
let sum = 0
let i = 1

while i <= 5 (
    sum = sum + i
    i = i + 1
)

sum
```

For loop modules, also include the parenthesized `while` condition form if both parser shapes are intended to remain valid:

```wist
while (i <= 5) (
    sum = sum + i
    i = i + 1
)
```

Choose examples that belong to the module and its documented dependencies.

## Backend availability tests

If a dialect exposes only `interpreter`, asking for `compiler` should fail.

If a dialect exposes only `cil`, asking for `interpreter` should fail.

Do not silently fall back to another backend. Silent fallback hides composition and backend-support errors.

## Optimizer safety tests

When a module interacts with optimizers, test behavior before and after optimization when possible.

The optimizer should not be required to make the feature correct. It may improve or normalize execution, but the base lowering must already represent valid semantics.

For backend-specific optimizers, also test that backend-specific intrinsics are not exposed to unsupported backends.

## Composition determinism tests

Module composition should be deterministic:

```text
same dialect
  -> same selected modules
  -> same ordering
  -> same runtime surface
```

Tests should catch unexpected modules, order changes that affect behavior and accidental inclusion of unsafe or unrelated features in restricted dialects.

## What not to rely on

Do not rely only on:

- the demo application;
- README examples;
- benchmark output;
- legacy debug log artifacts;
- one happy-path source file;
- compiler mode only;
- full-default dialect only.

A module that works only in a broad profile may still be incorrectly integrated.

Do not add tests that require `logs.txt` or the removed legacy log viewer. For
debuggability, prefer diagnostics, verifier output, parity tests and future
structured trace assertions once the trace contract exists.

## Suggested PR checklist

Before merging a module PR, verify:

- syntax is module-owned;
- parser priority is intentional;
- AST visitors self-filter;
- bytecode generation follows stack discipline;
- disabled dialects reject the feature;
- both backends are covered when supported;
- optional syntax variants are documented and tested when they are intentionally accepted;
- optimizer behavior is covered when relevant;
- restricted dialects remain restricted;
- no generic framework layer branches on concrete module names.

## Next

Continue with [Internals](/internals/) only when you need deeper details about the execution pipeline. For most feature work, the module authoring pages and tests should be enough for the first pass.
