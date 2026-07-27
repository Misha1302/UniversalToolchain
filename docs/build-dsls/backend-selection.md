---
title: Backend Selection
description: Explain interpreter, CIL, and backend choice.
---

# Backend Selection

Backends decide how a composed DSL program is executed after parsing and lowering.

Wist currently exposes two canonical backend IDs:

- `interpreter`
- `cil`

The `.wistdialect` file, CLI and facade use the same canonical backend IDs: `cil` and `interpreter`. Compatibility aliases are not supported. New backends must be discoverable through runtime manifests and registrars.

## When to read this page

Read this page when you are deciding whether a dialect should run through the interpreter, through generated CIL, or through both.

## Goal

Choose a backend intentionally and understand what must be tested when multiple backends are enabled.

## Backend modes

### Interpreter

Use interpreter mode when you want:

- simpler debugging;
- a smaller first implementation path;
- easier semantic validation;
- a backend that is useful before native/CIL-specific optimization is introduced.

Dialect example:

```text
dialect MinimalArithmetic
use Arithmetic,Numbers,Scopes,Whitespaces
backend interpreter
```

Run example:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic/program.wist --backend interpreter
```

### CIL compiler

Use the `cil` backend when you want:

- faster repeated execution;
- native .NET execution paths;
- optimized arithmetic or type-aware runtime behavior;
- integration with compiled artifacts.

Dialect example:

```text
dialect MinimalArithmeticNative
use Arithmetic,Numbers,Scopes,Whitespaces,NativeTypes
backend cil
enable ArithmeticOptimization
enable NativeCilOptimization
enable NativeTypesOptimization
```

Run example:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic-native/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic-native/program.wist --backend cil
```

### Both backends

Use both backends when you need:

- semantic parity checks;
- debugging through the interpreter and production execution through CIL;
- confidence that optimizations do not change program meaning.

Dialect example:

```text
backend cil,interpreter
```

When both are enabled, tests should run the same source through both modes and compare observable results.

## Choosing the right backend

| Scenario | Recommended backend |
|---|---|
| First prototype of a narrow DSL | `interpreter` |
| Debugging parser/AST/lowering behavior | `interpreter` |
| Performance-sensitive formula execution | `cil`, after parity tests |
| Documentation examples for semantics | both, when available |
| Restricted user-authored formulas | usually `cil` or `interpreter`, but only with a narrow module set |
| Backend feature development | both, with explicit parity and negative tests |

The backend choice is not a substitute for language restriction. A CIL-backed DSL can still be too broad if the dialect selects unnecessary modules.

## Programmatic entry point

First-contact users can build a Wist runtime facade instead of wiring services manually:

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
    backend: "cil");
```

The facade still uses the composed dialect runtime. It should not be treated as a separate language implementation.

## Failure modes

A backend selection should fail when:

- the CLI asks for `cil`, but the dialect exposes only `interpreter`;
- the CLI asks for `interpreter`, but the dialect exposes only `cil`;
- a selected module emits semantics the backend cannot execute;
- an optimizer lowers to backend-specific intrinsics that the backend does not support.

These failures are useful. They show that the dialect/backend boundary is enforced instead of silently falling back to an unintended path.

## Backend parity

Backend parity means that the same dialect and source program produce the same observable result across supported backends.

Parity matters most when:

- adding a new module;
- changing AST-to-bytecode/AIR lowering;
- introducing an optimizer;
- modifying interpreter execution;
- modifying CIL emission.

Parity does not mean every backend must support every dialect. A dialect may intentionally expose only one backend. In that case, document the limitation and test that unsupported modes are rejected.

## Common mistakes

- Documenting `cil` as available for a dialect that exposes only `interpreter`.
- Enabling CIL-specific optimizers without proving base semantics first.
- Treating the interpreter as a fallback when the selected backend fails.
- Adding backend-specific intrinsics to a general interpreter surface.
- Comparing benchmark numbers before separating compilation time from execution time.
- Copying syntax from secondary parser experiments instead of the runtime `.wistdialect` format used by shipped profiles.

## Next

Continue with [Testing a DSL](/build-dsls/testing-dsl).

## Backend contract invariant

A backend is selected by its canonical backend ID. The repository currently ships `interpreter` and `cil`; compatibility aliases are intentionally unsupported. A backend is considered first-class only if it can be discovered from runtime manifests, registered through its backend registrar, selected by dialect/runtime configuration, and executed through the backend-neutral artifact/session contract without editing `WistRuntimeFacade`.
