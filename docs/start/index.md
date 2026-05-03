---
title: Start Here
description: Give developers the fastest path from zero to a running Wist program.
---

# Start Here

This page introduces **UniversalToolchain** and the **Wist** reference language, explains who should read which sections of the documentation and helps you choose your route through the project.

## When to read this page

Read this page if you have just discovered the repository and want to get started. It helps you understand the difference between the framework and the reference language and points you to the right section.

## Goal

Understand the difference between UniversalToolchain and Wist, identify your role, and know where to start.

## Project overview

- **UniversalToolchain** is an embeddable .NET framework for composing restricted DSLs. It provides infrastructure for lexing, parsing, abstract syntax trees (AST), bytecode/AIR layers, optimizers and execution backends. Modules are the building blocks; dialects select modules, backends and optimizations; manifests resolve runtime activation.
- **Wist** is the reference language built on top of UniversalToolchain. It demonstrates how to assemble modules into a usable language and provides CLI and programmatic entry points.

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
