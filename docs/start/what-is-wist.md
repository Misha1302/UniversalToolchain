---
title: What is Wist?
description: Explain Wist as the reference language built on top of UniversalToolchain.
---

# What is Wist?

Wist is **not** the product. It is the reference language that demonstrates how UniversalToolchain modules can be assembled into a usable DSL.

## Problem

A DSL framework needs a concrete language to validate the architecture. UniversalToolchain needs a proving ground that exercises module composition, dialect files, bytecode/AIR, optimizers, compiler execution and interpreter execution. Wist fulfils that role.

Wist is not meant to replace C#, scripting engines or full language workbenches. It exists so the framework can be tested against real syntax, real modules and real backend paths.

## Concept

**Wist** is the reference language in this repository. It demonstrates:

- shipped dialect profiles;
- manifest-backed dialect composition;
- compiler and interpreter execution modes;
- CLI execution through `UniversalToolchain/Wistc/Wistc.csproj`;
- programmatic usage through the Wist runtime facade;
- constrained runtime surfaces such as `pricing-restricted`.

Wist is built from modules. A dialect selects which modules and backends are available. For example, `minimal-arithmetic` exposes a small arithmetic surface, while `full-default` exposes a broader language surface.

Rules are currently removed from the public runtime surface. Do not treat rule schemas, `rule-run`, raw-source RuleSet MVP parsing or `RuleDeclarationsModule` as available Wist runtime features.

## Minimal example

Run a simple expression through the Wist CLI:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --mode compiler
```

Expected output:

```text
12
```

You can also run the same expression in interpreter mode:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --mode interpreter
```

If a selected dialect does not expose the requested backend, Wistc reports an execution-mode error.

## How it fits into the pipeline

Wist uses UniversalToolchain’s pipeline:

```text
source → lexer/parser → AST → bytecode/AIR → optimizer → backend → result
```

Wist-specific code orchestrates one concrete language. UniversalToolchain remains the framework layer: dialect definitions, build plans, manifests, module selection and backend activation. Treat Wist as an example and validation target for the framework, not as the only possible language.

## Rules and constraints

- Wist programs run under a selected dialect.
- Syntax is only available when the dialect includes the owning module.
- `compiler` is the user-facing mode for the CIL backend.
- `interpreter` runs through the interpreter backend when the dialect enables it.
- Restricted dialects are composition constraints, not hardened sandboxes.
- Rules are not currently a public runtime capability.

## Next

Continue with [Installation](/start/installation), then run your [First Program](/start/first-program).
