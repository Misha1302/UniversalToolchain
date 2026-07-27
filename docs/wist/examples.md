---
title: Examples
description: Collect complete Wist programs.
---

# Examples

This page collects complete Wist programs and shows which dialect shape they belong to.

Use these examples as small executable references, not as a full language specification.

## When to read this page

Read this page after the [Syntax Tour](/wist/syntax-tour) when you want complete snippets that can be copied into examples, tests or dialect documentation.

## Goal

Connect Wist syntax examples to the dialects and modules that make them valid.

## Minimal arithmetic

Program:

```wist
2 + 3 * 4
```

Expected result:

```text
14
```

Typical dialect:

```text
dialect MinimalArithmetic
use Arithmetic,Numbers,Scopes,Whitespaces
backend interpreter
```

This is the best first example because it proves that the basic parser, arithmetic modules and interpreter path are working.

## Grouped arithmetic

Program:

```wist
(2 + 3) * 4
```

Expected result:

```text
20
```

Use this when documenting precedence and grouping.

## Variables and reassignment

Program:

```wist
let x = 5
x = x + 1
x
```

Expected result:

```text
6
```

Typical modules:

```text
use Arithmetic,Numbers,Identifier,Variables,Scopes,Whitespaces
```

This example proves that declaration, lookup and reassignment work together.

## Variable-based formula

Program:

```wist
price + fee * 2
```

With inputs:

```text
price = 100
fee = 5
```

Expected result:

```text
110
```

This is useful for pricing-style DSLs. Keep the dialect narrow and avoid enabling unrelated runtime features.

## Conditional expression

Program:

```wist
let x = 2
if x == 2 (10) else (20)
```

Expected result:

```text
10
```

Typical modules:

```text
use Conditions,Equality,Arithmetic,Numbers,Identifier,Variables,Scopes,Whitespaces
```

Add `ComparisonConditions` when using operators such as `<`, `<=`, `>` or `>=`.

## While loop accumulator

Program:

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

The `while` condition does not have to be parenthesized when it parses as one expression node. The loop body remains grouped because it contains multiple operations.

Typical modules:

```text
use Loops,Variables,Identifier,Arithmetic,Numbers,ComparisonConditions,Scopes,Whitespaces
```

Use this to test loop execution, variable mutation and backend parity.

## Full default `for` example

Program:

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

This is the shipped `full-default` example shape. It is useful for validating a broader Wist dialect over both interpreter and CIL modes. The current `for` parser expects initialization, condition, update and body to be grouped.

## Nested loop aggregate

Program:

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

Use this only for dialects that intentionally allow nested loops, local declarations and mutation.

## Interop expression in trusted profiles

Program:

```wist
NumbersModule.Core.RealNumberImpl.Add(2, 5)
```

Expected result:

```text
7
```

This belongs to trusted full profiles that include `CSharpInterop`. Do not use this example for restricted formula dialects.

## Choosing examples for documentation

Use the smallest example that proves the concept:

| Concept | Recommended example |
|---|---|
| First run | `2 + 3 * 4` |
| Grouping | `(2 + 3) * 4` |
| Variables | `let x = 5; x = x + 1; x` |
| Conditions | `if x == 2 (10) else (20)` |
| Loops | sum from 1 to 5 |
| Backend parity | same arithmetic/loop source in both modes |
| Restricted formula DSL | `price + fee * 2` |

## Common mistakes

- Using a broad full-default example to document a restricted DSL.
- Showing interop in user-facing formula documentation.
- Using loop examples before explaining variables and comparisons.
- Treating parenthesized `while` conditions as the only valid form.
- Forgetting to mention which dialect or modules make an example valid.
- Treating examples as complete language specification.

## Next

Continue with [Build DSLs](/build-dsls/) to turn these examples into explicit dialect profiles.
