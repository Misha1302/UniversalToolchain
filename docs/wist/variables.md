---
title: Variables
description: Document variable declarations, assignments, and lookup rules.
---

# Variables

Variables let Wist programs name intermediate values and reuse them later.

Variable support is not part of the smallest arithmetic dialect. A dialect must select the modules that own identifiers and variable behavior.

## When to read this page

Read this page when you want to write Wist programs with `let`, update values or pass named inputs into formula-like programs.

## Goal

Understand declaration, lookup, reassignment and the module dependencies behind variable syntax.

## Minimal declaration

```wist
let x = 10
x
```

Expected result:

```text
10
```

The first line declares a variable. The final expression returns its value.

## Variable in an expression

```wist
let x = 10
x + 5
```

Expected result:

```text
15
```

This requires arithmetic, numbers, identifiers and variables.

## Reassignment

A variable can be updated after declaration:

```wist
let x = 5
x = x + 1
x
```

Expected result:

```text
6
```

Reassignment is useful in loops and accumulator-style programs.

## Declaration depending on another variable

```wist
let x = 2
let y = x + 3
y
```

Expected result:

```text
5
```

The second declaration reads the value created by the first declaration.

## Required modules

A dialect with variables usually needs at least:

```text
use Arithmetic,Numbers,Identifier,Variables,Scopes,Whitespaces
```

`Identifier` is important because variable names are not just numeric literals. If a dialect includes `Variables` but not the necessary identifier support, variable syntax should not be expected to work correctly.

## Runtime inputs

Programmatic execution may provide named runtime inputs. For example, a pricing expression may use names such as `price` and `fee`:

```wist
price + fee * 2
```

The runtime host must keep declared runtime inputs separate from local variables declared inside the program. Local variables should not be accidentally overwritten by unrelated external argument values.

This boundary is important for formula DSLs. Runtime bindings are part of the host boundary; `let` declarations are part of the program semantics.

## Use before declaration

This is invalid in normal variable usage:

```wist
x
let x = 1
```

The variable is read before it is declared.

A good dialect test suite should cover this kind of failure in both compiler and interpreter modes when both are enabled.

## Variables in loops

Variables are commonly used as loop counters and accumulators:

```wist
let sum = 0
let i = 1

while i <= 5 (
    sum = sum + i
    i = i + 1
)

sum
```

Expected result:

```text
15
```

This requires variables plus loop and comparison support. Parentheses around the `while` condition are optional when the condition parses as one expression node.

## Common mistakes

- Adding `Variables` but forgetting `Identifier`.
- Using variables in a minimal arithmetic dialect.
- Treating runtime arguments and local variables as the same storage concept.
- Testing only variable declaration but not reassignment.
- Testing CIL mode but forgetting interpreter/compiler parity for variable mutation.

## Next

Continue with [Conditions](/wist/conditions) to use variables in branches.
