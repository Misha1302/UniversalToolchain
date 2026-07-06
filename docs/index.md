# UniversalToolchain Documentation

## Do not execute AI-generated code. Execute tiny rules in a language your .NET app controls.

UniversalToolchain is a compiler/runtime framework for restricted formulas and application DSLs.

It is useful when configuration turns into logic, expression evaluators become too small, and C# scripting is too broad for the surface you want to expose.

```text
admin / config / LLM suggestion
        -> tiny rule text
        -> restricted Wist formula surface
        -> validation or rejection
        -> interpreter for diagnostics
        -> CIL-backed typed delegate for hot paths
        -> your application decides the side effect
```

Wist is the reference language. UniversalToolchain is the framework behind it.

## 30-second demo

```csharp
using UniversalToolchain.Wist;

using var rules = WistEngine.CreateSafeFormulas();

var rolloutScore = rules.Compile<Func<double, double, double, double>>(
    "usage * 0.7 + reliability * 0.3 - incidents * 15.0",
    "usage",
    "reliability",
    "incidents");

double score = rolloutScore.CompiledDelegate(100.0, 90.0, 1.0);
bool enableNewDashboard = score >= 80.0;
```

The rule returns data. Your application performs the action.

## Why developers should care

Most apps grow through this path:

```text
hardcoded C# logic
        -> configurable formulas
        -> user/admin/LLM-suggested rules
        -> restricted application DSL
        -> compiled hot path
```

UniversalToolchain gives you a middle ground between JSON-rule trees and broad scripting.

## What is stable enough to show in the preview

- `WistEngine` facade for application-level formula execution.
- Restricted arithmetic/formula preset through `CreateSafeFormulas()` and `CreateRestrictedArithmetic()`.
- One-off `Evaluate<T>()` for previews and non-hot paths.
- `Validate()` and `TryCompile<TDelegate>()` for non-throwing validation flows.
- `Compile<TDelegate>()` and `CompileFunc(...)` for typed compiled invocation.
- Interpreter/compiler split for diagnostics, backend work, and parity checks.

The larger business-rule DSL direction is intentional, but the current public claim is scoped to supported formula shapes.

## Start here

| Goal | Start here |
|---|---|
| Install the package | [Installation](/start/installation) |
| Run the first program | [First Program](/start/first-program) |
| See the product showcase | [Showcase: controlled rules](/start/showcase) |
| Understand Wist | [What is Wist?](/start/what-is-wist) |
| Build a restricted DSL | [Building DSLs](/build-dsls/) |
| Study internals | [Pipeline](/internals/pipeline) |

## What this is not

UniversalToolchain is not a hardened sandbox, not a replacement for C#, and not a finished general-purpose language workbench. Restricted dialects control the selected language/runtime surface, but untrusted execution still needs external process or environment isolation.

See [Current Limitations](/limitations) and [Restricted DSL Security](/build-dsls/restricted-dsl-security) before exposing user-authored formulas.

## Architecture in one picture

```text
.NET host application
  -> Wist or custom DSL source
  -> dialect-selected modules and backends
  -> lexer/parser modules
  -> AST
  -> bytecode + semantic tags
  -> AIR
  -> optimizers
  -> interpreter backend / CIL backend
  -> result
```

## Documentation sections

| Section | Description |
|---|---|
| [Start](/start/) | Basic project model and the shortest path to running Wist. |
| [Wist](/wist/) | Syntax and examples for the reference language. |
| [Dialects](/build-dsls/) | Dialect files, feature composition, and backend selection. |
| [Modules](/write-modules/) | Extension points for adding language features. |
| [Internals](/internals/) | Compiler pipeline, bytecode, AIR, optimizers, and backends. |
| [Reference](/reference/) | Exact technical contracts and reference material. |
