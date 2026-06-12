# UniversalToolchain Documentation

## UniversalToolchain

Build restricted formulas and small DSL runtimes for .NET.

Use it when an expression evaluator is too small,
C# scripting is too broad,
and writing a compiler from scratch is too expensive.

## 30-second demo

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateSafeFormulas();

var formula = wist.CompileFunc<double, double, double>(
    "price * 0.9 + fee",
    "price",
    "fee");

double result = formula.Invoke(100.0, 5.0);
```

```text
95
```

<!-- langdev-2026-site:start -->

## Featured technical story

### Build the Language, Then Make the Abstractions Disappear

UniversalToolchain explores a practical form of extensible programming:
language features are composed as modules during construction, then
progressively lowered into concrete runtime or typed CIL operations
before execution.

The LangDev 2026 proposal demonstrates:

- deterministic dialect and runtime-plan composition;
- Bytecode → AIR → capability-gated specialization;
- AIR interpreter and `DynamicMethod`-based CIL execution;
- a real semantic-parity regression involving external bindings and local-variable shadowing;
- reproducible regression tests and carefully scoped benchmark evidence.

For supported compiled paths, the prepared hot invocation path does not
perform per-operation language-module dispatch. The generated typed CIL
is instead presented to the .NET JIT, while cross-backend tests protect
the language semantics.

[Read the talk proposal and reproducible demo](https://github.com/Misha1302/Wist2/tree/master/docs/talks/langdev-2026) ·
[Follow the lowering walkthrough](https://github.com/Misha1302/Wist2/blob/master/docs/talks/langdev-2026/lowering-walkthrough.md) ·
[Review benchmark evidence](https://github.com/Misha1302/Wist2/blob/master/docs/talks/langdev-2026/benchmark-evidence.md)

<!-- langdev-2026-site:end -->

## When to use / when not to use

Use UniversalToolchain when:
- you need controlled formulas/rules in a .NET app
- you want to restrict available language features
- you need a path from interpreter to compiled execution
- you want reusable DSL infrastructure

Do not use it when:
- you only need one arithmetic expression
- you need a hardened sandbox for untrusted code
- you need a stable production API today
- you need broad C# scripting

## Preview status

Current status:
- Wist-first preview
- .NET 10 baseline
- compiler-first hot path + interpreter diagnostics/parity backend
- restricted dialects are not hardened sandboxes
- APIs may change before stable release

## I want to...

- [embed formulas in .NET](/start/what-is-wist)
- [build a restricted DSL](/build-dsls/)
- [write a module](/write-modules/)
- [study compiler/runtime internals](/internals/)



UniversalToolchain is a Wist-first modular .NET DSL/runtime framework.

It helps you build small embeddable languages for .NET applications when a plain expression evaluator is too limited, full C# scripting is too broad, and writing a compiler from scratch would be too expensive.

Wist is the reference language built on top of UniversalToolchain. It is not the main product; it demonstrates how the framework pieces fit together.

> This documentation is written as a developer manual. It is not a landing page, a project report, or a promotional overview.

## In 60 seconds

- **UniversalToolchain** is the framework.
- **Wist** is the reference language used to validate the framework.
- **Dialects** select modules, optimizers, security posture and execution backends.
- **Modules** own reusable language features such as syntax, AST translation and bytecode behavior.
- **Backends** execute the selected language surface through interpreter or compiled execution paths.
- **Bytecode** and **AIR** keep frontend semantics separate from backend execution.

A typical use case is a .NET application that needs configurable formulas or restricted business rules without exposing a full general-purpose language.

## What this is not

UniversalToolchain is not a production-grade sandbox, not a replacement for C#, and not a finished general-purpose language workbench. Restricted dialects control language composition, but untrusted execution still needs external process or environment isolation.

See [Current Limitations](/limitations) for the current maturity boundaries.

## Entry points

| Goal | Start here | Continue with |
|---|---|---|
| Run the reference language | [Start](/start/) | [Wist](/wist/) |
| Compose a DSL | [Dialects](/build-dsls/) | [Module composition](/build-dsls/module-composition) |
| Add a language feature | [Modules](/write-modules/) | [Bytecode generation](/write-modules/bytecode-generation) |
| Study implementation details | [Internals](/internals/) | [Pipeline](/internals/pipeline) |
| Check precise contracts | [Reference](/reference/) | [Backend contracts](/reference/backend-contracts) |

## Recommended order

1. [Start here](/start/)
2. [Run the first Wist program](/start/first-program)
3. [Read the mental model](/start/mental-model)
4. [Build a minimal DSL](/build-dsls/minimal-dsl)
5. [Write a module](/write-modules/)
6. [Read the pipeline overview](/internals/pipeline)
7. [Use the reference section](/reference/)

## Pipeline

```text
.NET host application
  -> Wist or custom DSL source
  -> dialect-selected modules and backends
  -> lexer modules
  -> parser modules
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

## Project model

UniversalToolchain is not a single monolithic compiler.

The core idea is to make language features composable, testable, and reusable across dialects. Wist exists as the reference language that demonstrates how the framework pieces fit together.
