---
title: Scopes
description: Explain block scopes and variable visibility.
---

# Scopes

Scopes and grouped expressions help Wist organize nested syntax such as conditions and loop bodies.

In current Wist examples, parentheses are used heavily for grouping: arithmetic precedence, conditional result expressions, loop bodies and the grouped parts of `for` loops. Some constructs, such as `while` conditions, can also accept an unparenthesized expression when it parses as a single AST node.

## When to read this page

Read this page when you want to understand why many Wist constructs use parenthesized groups and how grouped bodies interact with variables.

## Goal

Understand grouping syntax, variable visibility expectations and why scope behavior must be tested together with variables, conditions and loops.

## Arithmetic grouping

Parentheses can group arithmetic expressions:

```wist
(2 + 3) * 4
```

Expected result:

```text
20
```

Without parentheses, multiplication binds before addition:

```wist
2 + 3 * 4
```

Expected result:

```text
14
```

## Grouped conditional results

Conditional result expressions are grouped with parentheses:

```wist
if 2 == 2 (1) else (2)
```

Nested grouped expressions are also possible:

```wist
if 2 == 2 (
    if 3 == 3 (9) else (8)
) else (7)
```

Expected result:

```text
9
```

## Grouped loop bodies

Loop bodies are grouped with parentheses:

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

The grouped body contains multiple operations: update the accumulator and update the loop variable.

The `while` condition above is not wrapped in parentheses because it already parses as one expression node. Parenthesized conditions are also valid and may be clearer in longer examples:

```wist
while (i <= 5) (
    sum = sum + i
    i = i + 1
)
```

## Variables and grouped bodies

A grouped body often reads and writes variables declared before the body:

```wist
let total = 0
let outer = 1

while outer <= 3 (
    let inner = 1
    while inner <= 2 (
        total = total + (outer * inner)
        inner = inner + 1
    )
    outer = outer + 1
)

total
```

Expected result:

```text
18
```

This kind of example is useful for testing that nested groups, local declarations and mutation behave consistently across backends.

## Required modules

Grouping appears in many features, but grouping alone is not a complete language surface. Depending on the program, a dialect may also need:

```text
use Scopes,Whitespaces,Numbers,Arithmetic,Identifier,Variables,Conditions,ComparisonConditions,Equality,Loops
```

Select only the modules required by the syntax you actually want to allow.

## What scopes are not

Do not use this page as a full formal specification of lexical scope rules. The project documentation should stay honest: current examples and tests demonstrate practical grouped execution behavior, while deeper implementation details belong in internals and contract pages.

For module authors, the important rule is ownership: grouped syntax should be represented through parser/AST structures, not recovered later by scanning raw source text.

## What to test

When changing scope-related behavior, test:

- arithmetic grouping;
- grouped conditional expressions;
- loop bodies with multiple operations;
- parenthesized and unparenthesized `while` conditions when both forms should remain accepted;
- nested loops or nested conditional groups;
- variable declarations inside grouped bodies;
- compiler/interpreter parity.

## Common mistakes

- Treating parentheses as plain text delimiters outside the parser/AST pipeline.
- Assuming every `while` condition must be parenthesized.
- Assuming grouped loop bodies work without `Loops`, `Variables` and comparison modules.
- Testing only flat expressions and missing nested grouped behavior.
- Documenting stronger scope guarantees than the current tests prove.

## Next

Continue with [Examples](/wist/examples) for complete runnable programs.
