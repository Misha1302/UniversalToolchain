---
title: Numbers
description: Document numeric literals and arithmetic behavior.
---

# Numbers

Numbers are the smallest useful Wist values. They are used by arithmetic expressions, comparisons, conditions, loops and most shipped examples.

## When to read this page

Read this page when you want to understand the numeric examples used by Wist dialects or when you are deciding whether a custom DSL needs only arithmetic or a broader language surface.

## Goal

Understand numeric literals, arithmetic operators and the modules needed to run numeric expressions.

## Minimal example

```wist
2 + 3 * 4
```

Expected result:

```text
14
```

Multiplication binds before addition. Use parentheses when the intended order should be explicit:

```wist
(2 + 3) * 4
```

Expected result:

```text
20
```

## Required modules

A minimal arithmetic dialect uses:

```text
use Arithmetic,Numbers,Scopes,Whitespaces
```

These modules cover numeric values, arithmetic operators, grouping and ordinary whitespace handling.

## Arithmetic operators

The common arithmetic operators are:

| Operator | Meaning |
|---|---|
| `+` | addition |
| `-` | subtraction |
| `*` | multiplication |
| `/` | division |

The exact runtime numeric representation depends on the selected dialect and backend path. Native profiles may also enable native type and CIL-oriented optimizations.

## Grouping

Parentheses group expressions:

```wist
(10 - 2) * (3 + 1)
```

Expected result:

```text
32
```

Grouping is important in DSL examples because it makes intent readable even when precedence already defines the same result.

## Numbers in larger programs

Numbers can be stored in variables when the active dialect includes variable support:

```wist
let price = 100
let fee = 5
price + fee * 2
```

Expected result:

```text
110
```

This example is no longer arithmetic-only. It also needs `Identifier` and `Variables`.

## Numbers in conditions

Numbers can participate in equality and comparison checks when the active dialect includes condition/comparison modules:

```wist
if 2 == 2 (1) else (0)
```

Expected result:

```text
1
```

This requires more than `Numbers` and `Arithmetic`; it also needs condition and equality support.

## Optimized numeric profiles

Some shipped dialects include native-oriented modules and optimizers. These are useful for performance-sensitive arithmetic, but they should be enabled after the basic semantics are already correct.

A native arithmetic profile may include optimizer flags such as:

```text
enable ArithmeticOptimization
enable NativeCilOptimization
enable NativeTypesOptimization
```

Do not use optimizers to hide missing language semantics. First make the unoptimized or simpler path correct, then optimize.

## Common mistakes

- Assuming arithmetic implies variables. It does not.
- Assuming arithmetic implies conditions. It does not.
- Using `cil` mode with an interpreter-only arithmetic dialect.
- Enabling native optimizers before testing the base expression behavior.
- Treating benchmark output as a substitute for semantic tests.

## Next

Continue with [Variables](/wist/variables) to use numeric values in named bindings.
