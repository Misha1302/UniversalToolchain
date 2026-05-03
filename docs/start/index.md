---
title: Start Here
description: Give developers the fastest path from zero to understanding and running Wist.
---

# Start Here

This page introduces **UniversalToolchain** and the **Wist** reference language, explains the practical reason the project exists, and helps you choose your route through the documentation.

## When to read this page

Read this page if you have just discovered the repository and want to understand the project before going deep into implementation details.

## Goal

Understand what UniversalToolchain is, what Wist is, when the project is useful, and where to start.

## Project overview

- **UniversalToolchain** is a Wist-first modular .NET framework for building embeddable DSL runtimes. It provides infrastructure for lexing, parsing, abstract syntax trees (AST), bytecode/AIR layers, optimizers and execution backends.
- **Wist** is the reference language built on top of UniversalToolchain. It demonstrates how to assemble modules into a usable language and provides CLI and programmatic entry points.
- **Modules** are reusable language features. They can contribute lexer, parser, AST translation, bytecode and IR behavior.
- **Dialects** select modules, backends and optimizations. They describe a chosen language/runtime surface instead of hardcoding one compiler profile.

## Use it when

- you want configurable formulas or restricted business rules inside a .NET application;
- you need language features to be selectable and testable as modules;
- you want a reference language that demonstrates a modular compiler/runtime pipeline;
- you need to compare interpreter and compiled execution paths for the same language surface.

## Do not treat it as

- a hardened sandbox for untrusted code;
- a drop-in replacement for C# scripting;
- a mature general-purpose language workbench;
- a simple expression evaluator with no compiler/runtime concepts.

For the current maturity boundaries, read [Current Limitations](/limitations).

## Identify your route

| User type | What to read first | Then read |
|---|---|---|
| **Wist user** (run programs, learn syntax) | [First Program](/start/first-program), [CLI Reference](/start/cli-reference) | [Syntax Tour](/wist/syntax-tour), [Examples](/wist/examples) |
| **DSL developer** (compose your own language) | [Mental Model](/start/mental-model), [Dialect Files](/build-dsls/dialect-files) | [Minimal DSL](/build-dsls/minimal-dsl), [Embedding in .NET](/build-dsls/embedding-dotnet) |
| **Module author** (add new language features) | [Write Modules](/write-modules/) | [Module Contracts](/reference/module-contracts), [Testing a Module](/write-modules/testing-module) |
| **Runtime/compiler engineer** | [Pipeline](/internals/pipeline) | [Bytecode](/internals/bytecode), [AIR](/internals/air), [Backends](/internals/backends) |
| **Contributor/maintainer** | [Internals](/internals/) | [Project Rules](/reference/project-rules), [Documentation Rules](/reference/documentation-rules) |
| **Reference user** | [Reference](/reference/) | [Backend Contracts](/reference/backend-contracts), [Module Reference](/reference/module-reference) |

## Recommended order

1. [First Program](/start/first-program)
2. [CLI Reference](/start/cli-reference)
3. [Mental Model](/start/mental-model)
4. [Syntax Tour](/wist/syntax-tour)
5. [Dialect Files](/build-dsls/dialect-files)
6. [Minimal DSL](/build-dsls/minimal-dsl)
7. [Embedding in .NET](/build-dsls/embedding-dotnet)
8. [Write Modules](/write-modules/)
9. [Pipeline overview](/internals/pipeline)
10. [Reference](/reference/)

## Next

Continue with [First Program](/start/first-program) to run your first Wist expression.
