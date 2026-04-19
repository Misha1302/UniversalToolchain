# UniversalToolchain vs Nearby Alternatives

## Why this file exists

UniversalToolchain is easy to misclassify.
It is not only a parser generator, not only an expression evaluator, and not a full language workbench.
This document explains where it fits, where it does not fit, and when another tool is the better engineering choice.

The goal of this file is not marketing.
The goal is to reduce category confusion and make trade-offs explicit.

## Short positioning

UniversalToolchain is best described as an **embeddable .NET DSL/runtime framework** for the point where:

- a plain expression evaluator is no longer enough,
- a parser alone is not enough,
- but a full language platform or workbench would be too heavy.

In practical terms, it sits between:

- **expression evaluators and rules libraries** such as NCalc, Dynamic Expresso, and RulesEngine,
- **parser-oriented tools** such as ANTLR, csly, and Irony,
- **full language workbenches** such as JetBrains MPS.

## Decision table

| Need | Better choice |
|---|---|
| Evaluate compact formulas or conditions | NCalc / Dynamic Expresso |
| Run JSON-defined business rules in a .NET app | RulesEngine |
| Build a parser and own the rest yourself | ANTLR / csly / Irony |
| Build a full language platform with rich editor tooling | JetBrains MPS |
| Build a restricted, embeddable DSL for .NET with reusable execution pipeline and selectable backends | UniversalToolchain |

## The closest practical alternative: RulesEngine

RulesEngine is the closest alternative when the business request is:

> "We want configurable rules in a .NET application without hardcoding every branch in C#."

That is the nearest overlap in day-to-day product work.

### Pick RulesEngine when

- rules can be expressed as JSON plus expression strings,
- the language surface does not need to become your own DSL,
- the main goal is externalized business policy,
- interpreter-like execution is enough,
- the team wants the smallest possible conceptual jump from ordinary application code.

### Pick UniversalToolchain instead when

- the rule language itself must have **custom syntax**,
- different contexts need **different allowed capabilities**,
- the runtime surface must be intentionally **restricted by dialect**,
- you need **compiler and interpreter** execution modes for the same language,
- you need a more explicit pipeline for diagnostics, translation, inspection, or backend experimentation.

### Hard boundary

RulesEngine is mainly a **rules product**.
UniversalToolchain is a **language/runtime framework** that can be used for rules.
Those are close, but not identical scopes.

## Lower-level alternatives: NCalc and Dynamic Expresso

These are the strongest alternatives when the real need is only:

- formulas,
- conditions,
- dynamic expressions,
- lightweight user-defined logic.

### Pick NCalc or Dynamic Expresso when

- syntax complexity is low,
- the accepted language shape is mostly fixed,
- users do not need a domain-specific syntax,
- you do not need explicit dialect composition,
- you do not need a bytecode/IR-style internal pipeline,
- you do not need to treat the language as a growing platform concern.

### Pick UniversalToolchain instead when

- expression evaluation is only the first step,
- the product is slowly growing toward a real DSL,
- you need execution-surface control rather than just evaluation,
- you need modular language feature composition,
- you want the same language to run through multiple backend strategies.

### Hard boundary

If the product only needs expression evaluation, UniversalToolchain is usually too much.
That is not a weakness of UniversalToolchain.
It is a scope mismatch.

## Frontend-oriented alternatives: ANTLR, csly, Irony

These tools are close to UniversalToolchain only in the **language frontend** part of the problem.
They are the most relevant alternatives if the team mainly needs:

- lexing,
- parsing,
- grammar ownership,
- syntax trees,
- parser error handling.

### Pick ANTLR, csly, or Irony when

- parser construction is the main problem,
- you already have your own runtime model,
- you already know how execution, validation, and backend mapping should work,
- you do not need an opinionated end-to-end runtime composition layer.

### Pick UniversalToolchain instead when

- parsing is only one stage in a larger runtime pipeline,
- the project needs reusable composition beyond the frontend,
- the same language should flow through translation, optimization, and backend execution,
- modules should participate in several pipeline stages instead of only the grammar layer.

### Hard boundary

ANTLR and csly are often better answers to the question:

> "How do I build a parser?"

UniversalToolchain is a better answer to the question:

> "How do I build and own an embeddable runtime language stack under .NET?"

## Upper-layer alternative: JetBrains MPS

JetBrains MPS is not the nearest day-to-day alternative, but it is the clearest comparison on the "language platform" side.

### Pick MPS when

- the language is becoming a serious product of its own,
- you need rich editor support as a first-class concern,
- you want a broader language engineering platform,
- projectional editing, advanced modeling, or richer tooling are part of the plan.

### Pick UniversalToolchain instead when

- the target is an **embeddable runtime layer inside a .NET application**,
- you need practical integration sooner than heavyweight language tooling,
- the project must stay small enough to own inside an application team,
- rich language IDE/tooling is not the main near-term goal.

### Hard boundary

MPS is broader and heavier.
UniversalToolchain is narrower and lighter.
A direct winner/loser comparison between them is usually the wrong frame.

## Visual Studio DSL Tools

Visual Studio DSL Tools are relevant when the desired outcome is not mainly a runtime DSL, but a **Visual Studio-centered modeling/tooling experience**.

### Pick DSL Tools when

- the center of gravity is Visual Studio integration,
- designer-style tooling matters more than runtime modularity,
- the language is strongly tied to modeling workflows.

### Pick UniversalToolchain instead when

- the center of gravity is runtime execution,
- CLI/programmatic execution is more important than VS designer tooling,
- the language should remain embeddable and framework-like.

## When UniversalToolchain is a bad fit

Do not start with UniversalToolchain when:

- you only need arithmetic expressions,
- you only need a subset of C# expressions,
- you only need a parser,
- you only need JSON-based rule configuration,
- a single execution surface is enough,
- a simple library already evaluates every rule you plan to support,
- the team does not actually want to own a language/runtime concern.

This is the most important anti-marketing part of the comparison.
A smaller tool is often the better tool.

## What would be an unfair comparison

The following comparisons are misleading:

### 1. Comparing UniversalToolchain to NCalc only on raw expression convenience

That ignores dialect control, execution modes, and reusable pipeline structure.

### 2. Comparing UniversalToolchain to ANTLR only on parser quality

That ignores the runtime, translation, backend, and composition parts.

### 3. Comparing UniversalToolchain to MPS as if both aim at the same product scope

They do not.
One is an embeddable .NET runtime framework.
The other is a broader language engineering environment.

### 4. Comparing UniversalToolchain to RulesEngine as if both are just two syntaxes for the same system

They overlap in business value, but differ in system ambition.
RulesEngine is centered on rules execution.
UniversalToolchain is centered on owning the language/runtime surface itself.

## Practical summary

If the team asks:

- **"How do we evaluate formulas?"** start with NCalc or Dynamic Expresso.
- **"How do we externalize business rules?"** start with RulesEngine.
- **"How do we build a parser?"** start with ANTLR, csly, or Irony.
- **"How do we build a full language platform?"** look at MPS.
- **"How do we embed a controlled DSL/runtime stack into a .NET product?"** UniversalToolchain becomes a serious candidate.

## One-sentence positioning

UniversalToolchain is best treated as a **middle-layer .NET language/runtime framework**: heavier than an evaluator, broader than a parser, and lighter than a full language workbench.
