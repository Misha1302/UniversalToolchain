---
title: Testing a DSL
description: Show how to test dialect behavior and backend parity.
---

# Testing a DSL

Testing a DSL is not only checking that one valid program runs. A useful DSL test suite proves that the selected dialect accepts intended syntax, rejects omitted syntax and produces the same result across supported backends.

## When to read this page

Read this page when you have created or modified a dialect file, added a module, enabled an optimizer or exposed a second backend.

## Goal

Build a test set that protects the DSL surface and prevents backend drift.

## What to test

At minimum, a dialect should have these test categories:

| Test category | What it proves |
|---|---|
| Positive execution | intended programs run |
| Negative syntax | omitted syntax is rejected |
| Backend availability | unsupported modes fail |
| Backend parity | interpreter and compiler agree when both are exposed |
| Optimizer safety | optimizers do not change observable results |
| Runtime surface | interop or unrelated modules are not accidentally selected |

## 1. Positive execution test

Start with the smallest valid program for the dialect.

For a minimal arithmetic dialect:

```wist
2 + 3 * 4
```

Expected result:

```text
14
```

This proves that the selected modules are sufficient for the intended core use case.

## 2. Negative syntax test

A restricted dialect should reject syntax that belongs to omitted modules.

For example, an arithmetic-only dialect should reject:

```wist
let x = 2
x + 1
```

unless it explicitly selects `Identifier` and `Variables`.

Negative tests are as important as positive tests. Without them, a “restricted” dialect may silently grow into full Wist.

## 3. Backend availability test

If a dialect declares:

```text
backend interpreter
```

then asking for compiler mode should fail.

If a dialect declares:

```text
backend cil
```

then asking for interpreter mode should fail.

The failure should be explicit. Do not silently fall back to another backend, because fallback hides composition mistakes.

## 4. Backend parity test

When a dialect exposes both:

```text
backend cil,interpreter
```

run the same source through both modes and compare the result.

Example parity cases:

```wist
(2 + 2) * 3
```

```wist
price + fee * 2
```

```wist
if price > 100 then price - 10 else price
```

Use only examples supported by the selected modules. If a dialect does not include conditions, do not use a conditional example in its parity suite.

## 5. Optimizer safety test

Optimizers should improve or normalize execution without changing program meaning.

Test optimizer-sensitive programs in two ways:

1. without the optimizer, if the dialect/profile supports that path;
2. with the optimizer enabled.

Compare observable results. For backend-specific optimizers, also verify that unsupported backends do not receive backend-specific intrinsics.

Current runtime optimizer directives use this shape:

```text
enable ArithmeticOptimization
disable EGraphOptimization
```

## 6. Runtime surface test

A DSL intended for end-user formulas should not accidentally expose broad runtime features.

For a pricing formula dialect, test that these are unavailable unless intentionally selected:

- C# interop;
- loops;
- labels;
- broad native/runtime functionality;
- removed rule-declaration paths.

This is not only a security concern. It also keeps the product behavior predictable.

## Example test matrix

| Dialect | Valid program | Invalid program | Backends |
|---|---|---|---|
| `minimal-arithmetic` | `2 + 3 * 4` | `let x = 2` | `interpreter` |
| `minimal-arithmetic-native` | `2 + 3 * 4` | unsupported non-arithmetic syntax | `compiler` |
| `pricing-restricted` | pricing expression with declared inputs | interop or unrelated syntax | selected restricted backend path |
| `full-default` | broad Wist examples | invalid syntax only | `compiler`, `interpreter` |

## What not to test as a shortcut

Do not test only the final demo application. Demo success does not prove the dialect surface is correct.

Do not test only compiler mode. Interpreter/compiler drift is one of the easiest ways to make the project look correct while breaking semantic guarantees.

Do not treat benchmark success as correctness. A fast wrong backend is still wrong.

## Practical checklist

Before documenting a dialect as stable, verify:

- at least one positive source example passes;
- at least one omitted feature is rejected;
- declared backend modes match the dialect file;
- unsupported backend modes fail explicitly;
- parity tests exist when both compiler and interpreter are enabled;
- optimizer behavior is tested separately from base semantics;
- restricted dialects do not expose interop or unrelated modules;
- dialect examples use the runtime `.wistdialect` format used by shipped profiles.

## Next

Continue with [Writing Modules](/write-modules/) if the DSL needs syntax that existing modules do not provide, or with [Internals](/internals/) if you need to understand the execution pipeline in more detail.
