---
title: CLI Reference
description: Document the current Wistc command surface used by onboarding examples.
---

# CLI Reference

This page documents the Wist command-line surface used by the public examples. It is a practical reference for running source text, files, dialect files and backend modes from the repository checkout.

## When to read this page

Read this after [First Program](/start/first-program) when you want to run more than the smallest quick-start expression.

## Command shape

From the repository root, Wist examples are run through the `Wistc` project:

```text
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run [options]
```

The `--` separates `dotnet run` options from Wistc options.

## Common options

| Option | Meaning |
|---|---|
| `--eval "source"` | Runs source text passed directly on the command line. |
| `--file path/to/program.wist` | Runs a source file. |
| `--dialect-file path/to/dialect.wistdialect` | Uses an explicit dialect file instead of the default runtime surface. |
| `--mode compiler` | Runs through the user-facing compiler mode when the selected dialect exposes CIL. |
| `--mode interpreter` | Runs through the interpreter mode when the selected dialect exposes the interpreter backend. |

`compiler` is a user-facing mode name. In dialect files, the backend id is usually `cil`.

## Minimal expression

```text
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --mode compiler
```

Expected output:

```text
12
```

Interpreter mode should produce the same observable result when it is available:

```text
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --mode interpreter
```

## Running a program file

A file-based run combines a program file with an optional dialect file:

```text
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic/program.wist --mode interpreter
```

Use this form for documentation examples, tests and local debugging because the program and dialect are both explicit.

## Backend mode versus dialect backend id

Wistc accepts user-facing modes such as `compiler` and `interpreter`. Dialect files select backend ids such as `cil` and `interpreter`.

The mapping is intentional:

| CLI mode | Dialect backend requirement |
|---|---|
| `compiler` | selected dialect exposes `cil` |
| `interpreter` | selected dialect exposes `interpreter` |

A dialect that exposes only `interpreter` should reject `--mode compiler`. A dialect that exposes only `cil` should reject `--mode interpreter`. Silent fallback would hide composition errors.

## Common failures

| Symptom | Likely cause |
|---|---|
| Project path cannot be found | Command was run from the wrong directory. Run from the repository root. |
| Compiler mode is rejected | The selected dialect does not expose the `cil` backend. |
| Interpreter mode is rejected | The selected dialect does not expose the `interpreter` backend. |
| Syntax is rejected | The dialect does not select the module that owns the syntax. |
| Interop expression is rejected | The selected dialect does not include trusted interop support. |
| Dialect file fails to parse | The file may use older shorthand syntax instead of the current parser-tested directive form. |

## Current dialect syntax reminder

Current parser-tested dialect examples use one directive per line:

```text
dialect MinimalArithmetic
use Arithmetic
use Numbers
use Scopes
use Whitespaces
backend interpreter enable
```

Older shorthand such as `use Arithmetic,Numbers` or `backend cil,interpreter` may appear in historical material, but it should not be used for new public examples unless that compatibility path is explicitly being documented.

## Next

Continue with [Wist Syntax Tour](/wist/syntax-tour) or [Minimal DSL](/build-dsls/minimal-dsl).
