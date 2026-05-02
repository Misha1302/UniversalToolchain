---
title: Dialect Reference
description: Document the dialect file format precisely.
---

# Dialect Reference

This page documents the current v1 dialect DSL syntax implemented by `DialectDefinitionParser`.

For conceptual guidance, read [Dialect Files](/build-dsls/dialect-files). This page is stricter: it describes parser-accepted directive shapes.

## Lexical rules

The dialect lexer currently recognizes:

| Token | Shape |
|---|---|
| identifier | letters, digits, `_`, `-`, `.` |
| string literal | double-quoted text on one line |
| arrow | `->` |
| equals | `=` |
| newline | line separator |

Whitespace other than newlines is skipped. Newlines are significant because each directive is line-based.

## Document shape

A dialect file starts with a required header:

```text
dialect <Name>
```

or with an explicit version:

```text
dialect <Name> version "<Version>"
```

Then zero or more directives follow, one directive per line.

## Minimal valid dialect

The parser accepts a dialect header alone:

```text
dialect Core
```

This only defines a dialect document. It does not by itself select a useful runtime surface.

## Directive reference

### `use`

Selects one module alias.

```text
use Arithmetic
```

Current parser shape:

```text
use <ModuleAlias>
```

Use multiple lines for multiple modules:

```text
use Numbers
use Arithmetic
use Scopes
use Whitespaces
```

### `exclude`

Excludes one module alias.

```text
exclude CSharpInterop
```

Current parser shape:

```text
exclude <ModuleAlias>
```

A module cannot be both used and excluded in the same dialect document.

### `requires`, `before`, `after`

Adds dependency/order rules.

```text
requires Variables -> Scopes
before Conditions -> Labels
after Loops -> Labels
```

Current parser shapes:

```text
requires <LeftAlias> -> <RightAlias>
before <LeftAlias> -> <RightAlias>
after <LeftAlias> -> <RightAlias>
```

### `backend`

Enables or disables one backend id.

```text
backend interpreter enable
backend cil disable
```

Current parser shape:

```text
backend <BackendId> enable
backend <BackendId> disable
```

Backend identifiers are open-ended identifiers. A custom backend id is accepted syntactically, but runtime resolution still requires a matching registered backend/runtime surface.

### `allow intrinsic` and `forbid intrinsic`

Allows or forbids an intrinsic for a backend selector.

```text
allow intrinsic "add_i32" for any
forbid intrinsic "reflect-call" for cil
```

Current parser shapes:

```text
allow intrinsic "<IntrinsicName>" for <BackendSelector>
forbid intrinsic "<IntrinsicName>" for <BackendSelector>
```

### `enable optimizer` and `disable optimizer`

Enables or disables an optimizer for a backend selector.

```text
enable optimizer ArithmeticOptimization for any
disable optimizer AggressiveInline for interpreter
```

Current parser shapes:

```text
enable optimizer <OptimizerAlias> for <BackendSelector>
disable optimizer <OptimizerAlias> for <BackendSelector>
```

### `security`

Declares the intended security profile.

```text
security restricted
security trusted
```

Only one security directive may be present.

### `capability`

Declares a boolean capability marker.

```text
capability supports-floats = true
capability interop-enabled = false
```

Current parser shape:

```text
capability <Name> = true
capability <Name> = false
```

Duplicate capability names are parser errors.

## Backend selector

Backend selectors are used by intrinsic and optimizer directives.

Accepted selector shapes:

| Selector | Meaning |
|---|---|
| `any` | all applicable backends |
| `*` | all applicable backends |
| `<BackendId>` | one backend id, such as `interpreter` or `cil` |

`any` and `*` are accepted only where the parser allows wildcard selectors.

## Duplicate/conflict diagnostics

The parser currently reports errors for:

| Code | Condition |
|---|---|
| `P100` | duplicate `use` module |
| `P101` | same module is both used and excluded |
| `P102` | duplicate `exclude` module |
| `P103` | conflicting backend directive for the same backend |
| `P104` | duplicate backend directive with the same state |
| `P105` | duplicate security directive |
| `P106` | duplicate capability directive |
| `P107` | unknown directive |

Lexing and parser expectation errors also have `P001`, `P002`, and `P200`-series diagnostics.

## Complete parser-tested example

```text
dialect Strict version "1.0"
use Arithmetic
exclude CSharpInterop
requires Variables -> Scopes
before Conditions -> Labels
after Loops -> Labels
backend interpreter enable
backend cil disable
allow intrinsic "add_i32" for any
forbid intrinsic "reflect-call" for cil
enable optimizer ConstFold for any
disable optimizer AggressiveInline for interpreter
security restricted
capability supports-floats = true
capability interop-enabled = false
```

## Compatibility note

Older docs or examples may contain shorthand forms such as:

```text
use Arithmetic,Numbers,Scopes,Whitespaces
backend interpreter
backend cil,interpreter
enable ArithmeticOptimization
capability interop-enabled
```

The current parser-tested v1 shape is stricter than those shorthand examples: it parses one module per `use`, requires backend `enable` or `disable`, requires `enable optimizer ... for ...`, and requires boolean values for `capability`.

When updating examples, prefer the parser-tested v1 form documented on this page.

## Related pages

- [Dialect Files](/build-dsls/dialect-files)
- [Module Reference](/reference/module-reference)
- [Backend Contracts](/reference/backend-contracts)
