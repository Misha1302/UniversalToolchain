# UniversalToolchain Documentation

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
