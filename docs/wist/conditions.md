---
title: Conditions
description: Document if/else and boolean conditions.
---

# Conditions

Conditions let Wist programs choose one of two result expressions.

Condition support is dialect-dependent. A program can use `if` only when the active dialect includes the modules that own condition syntax and the comparison or equality operators used by the condition expression.

## When to read this page

Read this page when you want to write `if` / `else` expressions or when a custom dialect needs conditional logic.

## Goal

Understand the current `if` syntax, result selection behavior and module dependencies.

## Minimal if/else

```wist
if 2 == 2 (1) else (2)
```

Expected result:

```text
1
```

If the condition is not satisfied, Wist returns the alternative expression:

```wist
if 2 == 3 (1) else (2)
```

Expected result:

```text
2
```

## Syntax shape

The current documented shape is:

```wist
if condition (thenExpression) else (elseExpression)
```

The result expressions are grouped with parentheses. Multi-line grouped expressions are possible when the selected modules support the required inner syntax.

## Conditions with variables

```wist
let x = 2
if x == 2 (10) else (20)
```

Expected result:

```text
10
```

This requires variable and identifier support in addition to condition and equality support.

## Nested conditions

```wist
if 2 == 2 (
    if 3 == 3 (9) else (8)
) else (7)
```

Expected result:

```text
9
```

Nested conditions are useful in tests because they prove that grouped conditional expressions are parsed and lowered consistently.

## Required modules

Depending on the expression, a conditional dialect may need modules such as:

```text
use Conditions,ComparisonConditions,Equality,BooleanConditions,Arithmetic,Numbers,Identifier,Variables,Scopes,Whitespaces
```

The exact set depends on the condition expression and result contents:

- `Conditions` owns conditional syntax.
- `Equality` is needed for `==`-style equality checks.
- `ComparisonConditions` is needed for comparison operators such as `<`, `<=`, `>` or `>=`.
- `BooleanConditions` is needed for boolean composition when used.
- `Variables` and `Identifier` are needed when conditions read named values.

## Conditions in formula DSLs

A pricing or scoring DSL may use conditions to express thresholds:

```wist
if price > 100 (price - 10) else (price)
```

This can be useful, but it also broadens the DSL surface. Add it only when conditional logic is part of the product requirement and test both outcomes.

## What to test

For every dialect that enables conditions, test:

- satisfied condition result;
- unsatisfied condition result;
- variable-based conditions if variables are allowed;
- invalid condition syntax;
- compiler/interpreter parity when both backends are exposed.

## Common mistakes

- Adding `Conditions` but forgetting equality or comparison modules required by the condition expression.
- Assuming `if` syntax works in minimal arithmetic dialects.
- Testing only one outcome.
- Documenting conditions as general statement syntax when the examples use expression-like result values.
- Letting compiler and interpreter disagree on nested condition lowering.

## Next

Continue with [Loops](/wist/loops) for repeated execution.
