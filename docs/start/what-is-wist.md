---
title: What is Wist?
description: Explain Wist as the reference language built on top of UniversalToolchain.
---

# What is Wist?

Wist is **not** the product. It is the reference language that demonstrates how UniversalToolchain's typed language, planning, artifact-route and runtime contracts can produce a usable DSL.

## Problem

A language framework needs a concrete proving ground. Wist exercises real syntax, modular frontend behavior, bytecode/AIR lowering, optimizers, compiler/interpreter backends, runtime policy and public embedding APIs.

Wist is not intended to replace C#, scripting engines or full language workbenches. It validates the framework against a real language surface.

## Concept

Wist demonstrates:

- shipped `.wistdialect` profiles and presets;
- typed feature/contribution composition;
- one canonical `LanguageCompiler -> LanguagePlan -> LanguageRuntime` execution path;
- compiler and interpreter backends;
- CLI execution through `UniversalToolchain/Wistc/Wistc.csproj`;
- programmatic usage through `UniversalToolchain.Wist`;
- constrained surfaces such as `pricing-restricted`.

A dialect requests Wist features/modules and backends. The Wist configuration frontend translates that request to `LanguageDefinition`; `LanguageCompiler` closes feature dependencies, applies exclusions and resolves the exact contribution/backend routes. `LanguageRuntime` then materializes only the components in that immutable plan.

Rules are currently removed from the public runtime surface. Do not treat rule schemas, `rule-run`, raw-source RuleSet MVP parsing or `RuleDeclarationsModule` as available Wist runtime features.

## Minimal example

Run a simple expression through the Wist CLI:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --backend cil
```

Expected output:

```text
12
```

Run the same expression in interpreter mode:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --backend interpreter
```

If the selected dialect does not expose the requested backend, Wistc reports an execution-mode error.

## How it fits into the pipeline

The semantic/runtime ownership chain is:

```text
Wist configuration
  -> LanguageDefinition
  -> LanguageCompiler
  -> LanguagePlan
  -> LanguageRuntime
```

The planned artifact route then executes the language pipeline:

```text
source -> syntax/AST -> bytecode -> AIR -> optimizers/optional SSA -> backend artifact -> result
```

Wist-specific code translates Wist-facing aliases and supplies Wist implementations. UniversalToolchain owns the generic feature/package/planning/runtime contracts. Wist does not maintain a second build plan or manifest-selected runtime beside `LanguagePlan`.

## Rules and constraints

- Wist programs run under one canonical language plan.
- Syntax is available only when the owning contribution is selected by that plan.
- Feature dependencies are closed by `LanguageCompiler`; callers do not need to mirror the complete transitive graph manually.
- Explicit exclusions fail closed if dependency closure requires an excluded contribution.
- `cil` selects the CIL backend when the language plan exposes it.
- `interpreter` selects the interpreter backend when the language plan exposes it.
- Restricted dialects constrain composition and host interop; they are not hardened process sandboxes.
- Rules are not currently a public runtime capability.

## Next

Continue with [Installation](/start/installation), then run your [First Program](/start/first-program).
