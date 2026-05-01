---
title: Syntax Tour
description: Walk through Wist syntax with small examples.
---

# Syntax Tour

This page is a tour, not a full language reference. It shows small Wist snippets that are useful when learning the reference language and when deciding which modules a dialect should include.

## When to read this page

Read this after [First Program](/start/first-program) and [Mental Model](/start/mental-model).

## Goal

Understand the basic Wist syntax surface used by shipped dialect examples: numbers, arithmetic, variables, conditions, loops and scopes.

## Prerequisites

- You can run Wistc from the repository root.
- You understand that syntax exists only when the active dialect includes the owning module.

## Steps

### 1. Numbers and arithmetic

```wist
2 + 3 * 4
```

Expected result:

```text
14
```

Arithmetic precedence follows the normal order: multiplication and division bind before addition and subtraction. Parentheses can be used to group expressions:

```wist
(2 + 3) * 4
```

### 2. Variables

Variables are declared with `let` and can be updated later:

```wist
let x = 2
let y = 3
x = x + y
x
```

This uses the `Variables`, `Identifier`, `Numbers`, `Arithmetic`, `Scopes` and `Whitespaces` modules.

### 3. Conditions

A simple if/else expression:

```wist
if 2 == 2 (1) else (2)
```

Expected result:

```text
1
```

A condition with a variable:

```wist
let x = 2
if x == 2 (10) else (20)
```

This requires condition, comparison, equality, variable and identifier support. In shipped full dialects, these are provided by modules such as `Conditions`, `ComparisonConditions`, `Equality`, `Variables` and `Identifier`.

### 4. Loops

A `while` loop:

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

For `while`, the condition can be written without parentheses when it parses as one expression node. Parentheses are still allowed for readability. Multi-operation loop bodies should remain grouped.

Loops require the `Loops` module plus variables, comparison and arithmetic support.

### 5. `for` loops

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

Use this example as the canonical reference for current `for` syntax. The current `for` parser expects initialization, condition, update and body to be grouped.

### 6. Comments

When the `Comments` module is selected, single-line and block comments are allowed:

```wist
// single-line comment
let x = 2
/* block
   comment */
x + 5
```

Expected result:

```text
7
```

### 7. Interop expression in full dialects

The full dialect can run C# interop expressions when `CSharpInterop` and trusted capabilities are enabled:

```wist
NumbersModule.Core.RealNumberImpl.Add(2, 5)
```

Expected result:

```text
7
```

Do not use this in restricted dialects. Interop is intentionally excluded from examples such as `restricted-sandbox`.

## Expected result

You should be able to map each syntax feature to the module that owns it. If a snippet fails under a custom dialect, check whether that dialect includes the required modules.

## What happened internally

Each syntax feature is introduced by one or more modules. Modules register lexer rules, parser node creators and AST-to-bytecode visitors. Dialects decide which modules are visible to the runtime.

## Common mistakes

- Trying to use variables in a dialect that omits `Variables` or `Identifier`.
- Trying to use loops in a dialect that omits `Loops`.
- Treating the parenthesized `while` condition examples as the only valid form.
- Using `compiler` mode with a dialect that only exposes `interpreter`.
- Assuming restricted dialects are hardened sandboxes. They constrain composition, but process isolation is still required for untrusted execution.

## Next

Continue with [Dialect Files](/build-dsls/dialect-files).
