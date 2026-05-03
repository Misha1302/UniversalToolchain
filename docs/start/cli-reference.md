---
title: CLI Reference
description: Document the current Wistc command surface used by onboarding examples.
---

# CLI Reference

This page documents the Wist command-line surface used by the public examples. It is a practical reference for running source text, files, dialect files and backend aliases from the repository checkout.

## When to read this page

Read this after [First Program](/start/first-program) when you want to run more than the smallest quick-start expression.

## Command shape

From the repository root, Wist examples are run through the `Wistc` project:

```text
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run [options] [code]
```

The `--` separates `dotnet run` options from Wistc options.

## Common options

| Option | Meaning |
|---|---|
| `--eval` | Treats the positional `code` argument as an expression and prints the result. |
| `--file path/to/program.wist` | Runs a source file. |
| `--dialect-file path/to/dialect.wistdialect` | Uses an explicit dialect file instead of the default runtime surface. |
| `--backend compiler` | Runs through the user-facing compiler alias when the selected dialect exposes CIL. |
| `--backend interpreter` | Runs through the interpreter backend alias when the selected dialect exposes the interpreter backend. |
| `--list-modules` | Lists available runtime components and exits. |

`compiler` is a user-facing backend alias. In dialect files, the backend id is usually `cil`.

## Minimal expression

```text
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --backend compiler
```

Expected output:

```text
12
```

In this command, `--eval` is a flag and `"(2 + 2) * 3"` is the positional source argument.

Interpreter mode should produce the same observable result when it is available:

```text
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --backend interpreter
```

## Running a program file

A file-based run combines a program file with an optional dialect file:

```text
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic/program.wist --backend interpreter
```

Use this form for documentation examples, tests and local debugging because the program and dialect are both explicit.

## Backend alias versus dialect backend id

Wistc accepts backend aliases such as `compiler` and `interpreter`. Dialect files select backend ids such as `cil` and `interpreter`.

The mapping is intentional:

| CLI backend alias | Dialect backend requirement |
|---|---|
| `compiler` | selected dialect exposes `cil` |
| `interpreter` | selected dialect exposes `interpreter` |

A dialect that exposes only `interpreter` should reject `--backend compiler`. A dialect that exposes only `cil` should reject `--backend interpreter`. Silent fallback would hide composition errors.

## Common failures

| Symptom | Likely cause |
|---|---|
| Project path cannot be found | Command was run from the wrong directory. Run from the repository root. |
| Compiler backend alias is rejected | The selected dialect does not expose the `cil` backend. |
| Interpreter backend alias is rejected | The selected dialect does not expose the `interpreter` backend. |
| Syntax is rejected | The dialect does not select the module that owns the syntax. |
| Interop expression is rejected | The selected dialect does not include trusted interop support. |
| Dialect file fails to parse | The file may use syntax from a secondary parser path instead of the runtime `.wistdialect` format. |

## Current dialect syntax reminder

Current shipped runtime dialect examples use compact comma-separated selection directives:

```text
dialect MinimalArithmetic
use Arithmetic,Numbers,Scopes,Whitespaces
backend interpreter
```

For a CIL-capable dialect, select the `cil` backend id and request it from the CLI through `--backend compiler`:

```text
dialect MinimalArithmeticNative
use Arithmetic,Numbers,Scopes,Whitespaces,NativeTypes
backend cil
enable ArithmeticOptimization
enable NativeCilOptimization
enable NativeTypesOptimization
```

The repository also contains a stricter parser-specific dialect syntax, but public Wist runtime examples should follow the syntax used by shipped `.wistdialect` profiles unless the runtime path is intentionally migrated.

## Next

Continue with [Wist Syntax Tour](/wist/syntax-tour) or [Minimal DSL](/build-dsls/minimal-dsl).
