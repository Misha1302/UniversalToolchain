# UniversalToolchain Documentation

UniversalToolchain is a modular .NET framework for building domain-specific languages.

Wist is the reference language built on top of UniversalToolchain. Start with the language, then move to dialects, modules, bytecode, AIR, and execution backends.

> This documentation is written as a developer manual. It is not a landing page, a project report, or a promotional overview.

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
source
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
