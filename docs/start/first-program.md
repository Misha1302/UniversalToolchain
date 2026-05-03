---
title: First Program
description: Run the smallest useful Wist program and explain what happened internally.
---

# First Program

This is the shortest practical check that Wist, Wistc and the default runtime path are working.

## When to read this page

Read this after [Installation](/start/installation). It is the first executable page in the documentation.

## Goal

Run one Wist expression through the CLI and verify the expected output.

## Prerequisites

- You are in the repository root.
- The .NET solution has been restored and built.
- You are using the branch required by your current task or pull request.

## Steps

### 1. Run the compiler mode quick start

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --backend compiler
```

Expected output:

```text
12
```

`compiler` is the user-facing backend alias that selects the CIL backend when the active dialect exposes the CIL backend.

### 2. Run the same expression through the interpreter

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --backend interpreter
```

Expected output:

```text
12
```

The interpreter path is useful when validating semantic parity. If a selected dialect does not expose `interpreter`, Wistc will reject that backend alias.

### 3. Run the pricing demo

```bash
dotnet run --project UniversalToolchain/Example/Example.csproj
```

The pricing demo runs a pricing formula through hardcoded C# logic, the shipped `full-default-native` Wist preset and the shipped `pricing-restricted` dialect. It also demonstrates compiler, interpreter and fast native invocation paths.

## Expected result

The quick start prints `12`. The pricing demo should show matching pricing results across the supported execution paths and should reject the formula shape that the restricted pricing dialect intentionally does not allow.

## What happened internally

For the simple expression, the runtime path is:

```text
source → parser → AST → bytecode/AIR → selected backend → result
```

The CLI receives source text as the positional `code` argument. The `--eval` flag tells Wistc to evaluate that argument as an expression and print the result. Wist uses the active dialect to select modules and backends, parses the expression, translates it into intermediate representations and runs it through the selected backend.

The same source should produce the same observable result in compiler and interpreter modes when both modes are available. This parity is one of the main correctness expectations for Wist backends.

## Common mistakes

- Running the command from a subdirectory, causing project paths to fail.
- Using `--backend compiler` with a dialect that exposes only `interpreter`.
- Assuming all dialects expose all Wist syntax. Syntax exists only when the owning module is selected.
- Treating restricted dialects as security sandboxes. They restrict composition, but they are not hardened process sandboxes.

## Next

Read the [CLI Reference](/start/cli-reference) if you want the command surface, or continue with the [Mental Model](/start/mental-model) and the [Wist Syntax Tour](/wist/syntax-tour).
