---
title: Minimal DSL
description: Build the smallest useful DSL from existing modules.
---

# Minimal DSL

This page shows how to build a small DSL by selecting existing Wist modules in a dialect file. It does not start with writing a new module.

## When to read this page

Read this after [Dialect Files](/build-dsls/dialect-files) if you want to compose a smaller language surface than full Wist.

## Goal

Create a small dialect from existing modules and run a simple program through it.

## Prerequisites

- You can run Wistc from the repository root.
- You understand that dialects select modules and backends.
- You do not need a brand-new syntax feature yet.

## Steps

### 1. Start from the shipped minimal arithmetic dialect

The repository already contains a minimal arithmetic example:

```text
UniversalToolchain/Dialects/examples/wist/minimal-arithmetic/dialect.wistdialect
```

Its dialect file is intentionally small:

```text
dialect MinimalArithmetic
use Arithmetic,Numbers,Scopes,Whitespaces
backend interpreter
```

The matching program is:

```wist
2 + 3 * 4
```

Expected result:

```text
14
```

### 2. Run it through Wistc

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic/program.wist --backend interpreter
```

This executes the program using only the modules and backend selected by the dialect.

### 3. Add one feature at a time

If you need variables, extend the module list:

```text
dialect MinimalVariables
use Arithmetic,Numbers,Identifier,Variables,Scopes,Whitespaces
backend interpreter
```

Then a program like this becomes valid:

```wist
let x = 2
let y = 3
x + y
```

If you need comparisons and conditions, add the owning modules explicitly:

```text
use ComparisonConditions,Conditions,Equality,BooleanConditions
```

The exact module set depends on the syntax you want to allow. Start narrow and add only the features you can test.

### 4. Choose backend intentionally

For interpreter-only DSLs:

```text
backend interpreter
```

For CIL-backed DSLs:

```text
backend cil
```

For both modes:

```text
backend cil,interpreter
```

If both backends are enabled, add parity tests so the same source produces the same observable result in both modes.

### 5. Add optimizers only when needed

A native arithmetic dialect can enable optimizers such as:

```text
enable ArithmeticOptimization
enable EGraphOptimization
enable NativeCilOptimization
enable NativeTypesOptimization
```

Do this after the minimal interpreter version works. Optimizers should not be used to hide missing semantics.

## Expected result

You should end up with a dialect that exposes only the language features you selected. Code using omitted syntax should fail. That failure is useful: it proves the dialect is actually constraining the runtime surface.

## What happened internally

The dialect file is compiled into a build plan, resolved against runtime manifests and used to create a Wist execution host. Only the selected modules and backends are activated for that host.

## Common mistakes

- Starting with `full-default` and removing features without testing the result.
- Forgetting `Identifier` when adding `Variables`.
- Enabling `cil` mode in the CLI while the dialect declares only `backend interpreter`.
- Adding `CSharpInterop` or interop capabilities to a DSL intended for restricted user-authored formulas.
- Documenting rules as available. Rule runtime functionality is currently removed from the public surface.
- Copying syntax from secondary parser experiments instead of the runtime `.wistdialect` format used by shipped profiles.

## Next

Continue with [Write Modules](/write-modules/) if you need syntax that no existing module provides.
