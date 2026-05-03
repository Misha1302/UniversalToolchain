---
title: Embedding in .NET
description: Show how a host application uses a composed Wist runtime instead of shelling out to the CLI.
---

# Embedding in .NET

UniversalToolchain is intended to be embedded into .NET applications. The CLI is useful for learning and smoke checks, but production-style DSL usage normally runs through a host API.

## When to read this page

Read this after [Minimal DSL](/build-dsls/minimal-dsl) when you want to run formulas or restricted scripts from application code.

## Goal

Create a Wist runtime facade, select a shipped dialect preset, pass source text and named inputs, choose a backend mode and read the result.

## Minimal host shape

A host application should treat the DSL runtime as a composed execution surface:

```text
select dialect or preset
  -> create runtime facade
  -> pass source and declared inputs
  -> choose requested mode
  -> receive result or explicit failure
```

The facade is an entry point into the same dialect/runtime composition model. It is not a second implementation of Wist.

## Example: restricted pricing expression

```csharp
using UniversalToolchain.Dialects.Wist.Facade;

using var runtime = WistRuntimeFacadeBuilder
    .CreateDefault()
    .WithShippedDialectPreset("pricing-restricted")
    .Build();

var result = runtime.Run(
    "price + fee * 2",
    new Dictionary<string, object?>
    {
        ["price"] = 100,
        ["fee"] = 5
    },
    mode: "compiler");
```

Expected numeric result:

```text
110
```

The exact result wrapper shape depends on the current facade API. The important contract is that the host selects a dialect, supplies inputs explicitly and requests a backend mode intentionally.

## Why inputs are explicit

Runtime inputs such as `price` and `fee` are host-provided bindings. They are not the same concept as local variables declared by the Wist program.

A correct runtime path must keep these concerns separate:

```text
external binding: provided by the host
local variable: declared and mutated by the program
```

This boundary matters for formula DSLs because a host parameter should not accidentally overwrite an unrelated local variable.

## Choosing a dialect for embedding

Use the narrowest dialect that supports the business case.

| Scenario | Recommended dialect shape |
|---|---|
| Pricing/scoring formula | arithmetic, identifiers, selected variable/input support, no interop |
| Workflow-like script | variables, conditions and loops only if needed |
| Demonstrating Wist | broad reference profile |
| Untrusted user-authored code | narrow dialect plus process/resource isolation outside Wist |

Do not embed `full-default` only because it is convenient. A broad profile exposes more syntax and runtime behavior than a restricted formula surface usually needs.

## Backend mode selection

`mode: "compiler"` requests the CIL-backed compiler path when the selected dialect exposes CIL. `mode: "interpreter"` requests the interpreter path when exposed.

When both modes are available, add parity tests for important formulas. When only one mode is available, test that the unsupported mode fails explicitly.

## What to test in a host application

A host using Wist should test:

- valid formula execution;
- missing input behavior;
- wrong input type behavior;
- syntax rejected when the owning module is omitted;
- backend availability failures;
- compiler/interpreter parity when both modes are exposed;
- interop rejection for restricted formula dialects.

## Security boundary

A restricted dialect is a composition constraint. It limits which modules, syntax and runtime components are selected. It is not a hardened process sandbox.

For arbitrary untrusted third-party code, combine dialect restriction with external isolation, resource limits and host-level policy.

## Next

Continue with [Backend Selection](/build-dsls/backend-selection), [Testing a DSL](/build-dsls/testing-dsl) and [Backend Contracts](/reference/backend-contracts).
