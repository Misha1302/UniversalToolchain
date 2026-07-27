---
title: Wist Overview
description: Introduce Wist as a practical language used to demonstrate UniversalToolchain.
---

# Wist Overview

Wist is the reference language built on top of UniversalToolchain.

It is not meant to be a separate monolithic compiler competing with general-purpose languages. Its main role is to demonstrate how a language can be assembled from modules, constrained through dialect files and executed through different backend paths.

## When to read this section

Read this section when you want to:

- run existing Wist programs;
- understand the syntax used in shipped dialect examples;
- decide which modules a custom DSL needs;
- compare full Wist with restricted dialects;
- understand which language features belong to which modules.

If you want to build your own DSL, read this section first, then continue with [Build DSLs](/build-dsls/).

## What Wist demonstrates

Wist demonstrates several UniversalToolchain ideas in one concrete language:

- syntax is contributed by modules;
- dialect files select which syntax and runtime features are available;
- the same source can run through interpreter and CIL modes when both are exposed;
- restricted dialects can intentionally remove features from the language surface;
- module and backend choices must be tested instead of assumed.

## Minimal example

The smallest useful Wist program is an arithmetic expression:

```wist
2 + 3 * 4
```

Expected result:

```text
14
```

This works under the shipped `minimal-arithmetic` dialect because that dialect selects arithmetic, numbers, scopes and whitespace handling.

## Full language example

The shipped `full-default` example computes the sum from 1 to 5:

```wist
let sum = 0

for (let i = 1) (i <= 5) (i = i + 1) (
    sum = sum + i
)

sum
```

Expected result:

```text
15
```

This requires a broader module set: variables, identifiers, arithmetic, comparisons, loops, scopes and whitespace handling.

## Language features in this section

| Page | What it explains |
|---|---|
| [Syntax Tour](/wist/syntax-tour) | short examples across the language surface |
| [Numbers](/wist/numbers) | numeric literals and arithmetic expressions |
| [Variables](/wist/variables) | `let`, assignment and lookup |
| [Conditions](/wist/conditions) | `if` / `else`, equality and comparisons |
| [Loops](/wist/loops) | `while` and `for` examples |
| [Scopes](/wist/scopes) | block-like expression grouping and variable visibility notes |
| [Examples](/wist/examples) | complete runnable programs and matching dialects |

## Important boundary

A Wist feature exists only when the active dialect selects the owning module or module group.

For example, this program needs variable support:

```wist
let x = 10
x + 5
```

It should not be expected to work under a dialect that selects only arithmetic and numbers.

## Wist vs. UniversalToolchain

Use this mental split:

- **UniversalToolchain** is the framework for composing DSL runtimes.
- **Wist** is the reference language that proves the framework can compose a usable language.
- **Dialect files** are the selection layer that controls which Wist features are visible in a specific runtime.

## Common mistakes

- Treating Wist as one fixed language surface. In practice, the active dialect matters.
- Assuming `full-default` is the right profile for end-user formulas. Restricted profiles are usually better for that.
- Assuming compiler and interpreter modes are always both available. The dialect decides which backends are exposed.
- Adding syntax examples without checking which modules own that syntax.

## Next

Start with the [Syntax Tour](/wist/syntax-tour), then continue to [Numbers](/wist/numbers) and [Variables](/wist/variables).
