---
title: Mental Model
description: Explain the source to backend pipeline without overwhelming the reader.
---

# Mental Model

This page explains the project model behind UniversalToolchain and Wist without going deep into implementation details.

## Problem

A DSL runtime is not only a parser. A useful embeddable DSL needs syntax, semantic representation, execution strategy, module selection and tests that prove different execution paths behave the same. UniversalToolchain separates these responsibilities so features can be composed and validated instead of hardcoded into one compiler path.

## Concept

The core distinction is:

- **UniversalToolchain** is the framework.
- **Wist** is the reference language built on the framework.
- **Dialect** is a selected language/runtime composition.
- **Module** is a reusable language feature.
- **Backend** is an execution strategy.
- **Bytecode/AIR** are intermediate layers between AST and execution.

A simplified pipeline looks like this:

```text
source
  → lexer/parser
  → AST
  → bytecode / AIR
  → optimizers
  → interpreter backend / CIL backend
  → result
```

## Minimal example

A tiny Wist expression:

```wist
(2 + 2) * 3
```

When run through the CLI, this expression is not evaluated directly from raw text. The active dialect selects modules such as numbers and arithmetic. Those modules contribute lexer and parser behavior. The AST is lowered into intermediate representation, then the selected backend produces the final result.

## How it fits into the pipeline

A `.wistdialect` file decides which modules and backends are available. For example:

```text
dialect MinimalArithmetic
use Arithmetic,Numbers,Scopes,Whitespaces
backend interpreter
```

This dialect selects a very small language surface: arithmetic expressions, numbers, scopes and whitespace handling. It exposes only the interpreter backend. If you try to run code in `compiler` mode under this dialect, the runtime should reject that mode.

## Rules and constraints

- A module must own its syntax. Do not recognize language syntax through ad hoc raw-source parsing outside the lexer/parser/AST pipeline.
- A dialect selects runtime capabilities; it should not be replaced by hardcoded profile checks.
- Compiler and interpreter behavior should stay semantically aligned for the same dialect and source.
- Runtime selection must stay deterministic.
- Rules and rule declarations are currently removed from the public runtime surface. Do not document them as available runtime capabilities.

## Next

Continue with the [Wist Syntax Tour](/wist/syntax-tour), then read [Dialect Files](/build-dsls/dialect-files).
