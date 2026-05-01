---
title: Loops
description: Document loop constructs and common examples.
---

# Loops

Loops let Wist programs repeat a grouped body while updating local state.

Loop support is dialect-dependent. A dialect must select the module that owns loop syntax and the modules required by the loop condition and body.

## When to read this page

Read this page when you want to use `while` or `for` constructs, or when you are deciding whether a DSL should allow repeated execution at all.

## Goal

Understand the current loop syntax, typical accumulator examples and the modules needed for loop programs.

## `while` loop

A `while` loop repeats while its condition remains satisfied:

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

This program sums numbers from 1 to 5.

The condition does not have to be wrapped in parentheses when it parses as one expression node. The parser builds the `while` node from the next AST node as the condition and the following AST node as the body. Parentheses around the condition are still allowed and can be used for readability:

```wist
while (i <= 5) (
    sum = sum + i
    i = i + 1
)
```

For multi-operation loop bodies, keep the body grouped with parentheses.

## Zero-iteration loop

If the condition is not satisfied initially, the body is not executed:

```wist
let marker = 17
let i = 10

while i < 10 (
    marker = marker + 100
    i = i + 1
)

marker
```

Expected result:

```text
17
```

This is an important test case because it proves that loop entry logic is correct.

## `for` loop

The shipped `full-default` example uses this form:

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

The `for` loop groups initialization, condition, update and body:

```text
for (initialization) (condition) (update) (body)
```

Use this as the canonical documented shape for current Wist `for` syntax. Unlike `while`, the current `for` parser expects the initialization, condition, update and body to be grouped as scopes.

## Required modules

Loop programs usually need a broader module set than arithmetic-only programs:

```text
use Loops,Variables,Identifier,Arithmetic,Numbers,ComparisonConditions,Scopes,Whitespaces
```

Depending on the condition expression, equality or boolean modules may also be needed.

## Loops and restricted DSLs

Loops are powerful, but they broaden the runtime surface. A formula DSL for pricing, scoring or simple validation often does not need loops.

Before adding loops to a user-facing DSL, check:

- whether a loop is part of the actual product requirement;
- whether repeated execution should be bounded externally;
- whether both compiler and interpreter paths handle the loop consistently;
- whether a simpler expression or aggregate input would be safer and easier to test.

## What to test

For a dialect that enables loops, test:

- normal loop execution;
- zero iterations;
- mutation of a loop variable;
- parenthesized and unparenthesized `while` conditions when both are intended to remain valid;
- nested loops if they are supported by the intended dialect;
- malformed loop syntax;
- compiler/interpreter parity when both backends are exposed.

## Common mistakes

- Adding `Loops` without the variable and comparison modules required by the loop body.
- Assuming `while` requires a parenthesized condition.
- Forgetting to update the loop variable.
- Using loops in a restricted formula DSL when a simpler expression would be enough.
- Testing only the happy path and missing zero-iteration behavior.
- Assuming `while` and `for` are available under every Wist dialect.

## Next

Continue with [Scopes](/wist/scopes) to understand grouped expressions and variable visibility notes.
