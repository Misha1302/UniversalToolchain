---
title: What is UniversalToolchain?
description: Explain UniversalToolchain as a modular .NET framework for building DSLs.
---

# What is UniversalToolchain?

UniversalToolchain is the project’s **framework**, not a language itself. This page clarifies what the framework is for, when to reach for it and when to look elsewhere.

## Problem

Many applications start by evaluating simple expressions. As business logic grows, those expressions turn into small languages: formulas need variables, conditions, loops and sometimes calls into native code. A one-off evaluator or rules library often becomes hard to extend and hard to constrain. The gap between a trivial evaluator and a full language workbench is large, leaving developers with few options when they need a restricted but embeddable DSL.

## Concept

**UniversalToolchain** is an embeddable .NET framework for composing restricted DSLs. It supplies infrastructure for lexing, parsing, AST construction, bytecode/AIR generation, optimizations and execution backends. Language features are delivered as *modules*; a *dialect* selects which modules, optimizers and backends participate; a *manifest* resolves those selections into a runtime plan. The pipeline is always the same:

```text
Source → Lexer/Parser → AST → Bytecode/AIR → Optimizations → Compiler/Interpreter → Execution
```

Unlike parser libraries or turnkey expression evaluators, UniversalToolchain separates language construction from execution. Developers decide which capabilities are available by editing dialect files rather than editing framework code.

## Minimal example

UniversalToolchain does not have a built-in language. The quickest way to see it in action is through the **Wist** reference language. To run a simple expression you can use the CLI shipped in the repository:

```bash
# from the repository root
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --backend compiler
```

This uses UniversalToolchain’s pipeline to parse and evaluate the expression. The expected output is `12`. For a richer demonstration of dialect composition, run the pricing demo:

```bash
dotnet run --project UniversalToolchain/Example/Example.csproj
```

The pricing demo compares hard-coded C# logic, a full Wist preset and a restricted pricing dialect.

## When to use UniversalToolchain

Use UniversalToolchain when a plain evaluator is too narrow for your rules or formulas, you need a syntax that matches your domain, you want to restrict which language features are available, or you need both compiler and interpreter modes. Typical scenarios include pricing formulas, routing rules, internal workflow rules and DSL experiments inside .NET applications.

## When not to use UniversalToolchain

Do not start here if you only need trivial arithmetic, a small subset of C# expressions, a parser generator or a simple library call can evaluate all of your rules. UniversalToolchain is heavier than a one-line expression evaluator and lighter than a full language workbench; it is designed for cases where you need a carefully controlled runtime rather than the full power of C# or a generic parser.

## How it fits into the pipeline

UniversalToolchain owns the framework story: dialect definitions, build plans, runtime manifests, selected runtime plans and backend activation. Wist, the reference language, provides a concrete orchestration layer on top of that framework. Developers build or select dialects to choose modules and backends, and the framework takes care of lexing, parsing, AST translation and execution. Because the pipeline is compositional, you can add new language features by authoring modules without modifying the core framework.

## Next

Continue with [What is Wist?](/start/what-is-wist) to learn about the reference language built on top of UniversalToolchain.
