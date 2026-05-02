---
title: Dialect Files
description: Document the dialect file format and its role.
---

# Dialect Files

Dialect files describe which modules, optimizers and backends are available for a Wist runtime composition.

## Problem

A reusable DSL framework needs a way to select language features without hardcoding them into one compiler. Dialect files are that selection layer. They let a DSL developer assemble a runtime surface from existing modules and make the selected surface explicit.

## Concept

A Wist dialect file uses the `.wistdialect` extension. It is compiled before program execution and becomes part of the runtime selection path:

```text
dialect source → dialect compilation → build plan → manifest-backed runtime selection → host creation → execution
```

A parser-tested v1 dialect file uses one directive per line:

```text
dialect FullDefault
use Arithmetic
use BooleanConditions
use Comments
use ComparisonConditions
use Conditions
use CSharpInterop
use Equality
use Identifier
use Labels
use Loops
use Numbers
use Scopes
use SemicolonAsNewLine
use Variables
use Whitespaces
backend cil enable
backend interpreter enable
enable optimizer BooleanOptimization for any
enable optimizer ComparisonIntrinsicOptimization for any
security trusted
capability interop-enabled = true
```

## Minimal example

A minimal arithmetic dialect looks like this in the current parser-tested v1 form:

```text
dialect MinimalArithmetic
use Arithmetic
use Numbers
use Scopes
use Whitespaces
backend interpreter enable
```

This dialect can run a program such as:

```wist
2 + 3 * 4
```

Expected result:

```text
14
```

## Directives

### `dialect`

Declares the dialect name:

```text
dialect MinimalArithmetic
```

The name is used in diagnostics and composition output.

### `use`

Selects one module:

```text
use Arithmetic
```

Use multiple `use` lines to select multiple modules:

```text
use Arithmetic
use Numbers
use Scopes
use Whitespaces
```

A module must be selected for its syntax and runtime behavior to exist. For example, arithmetic syntax needs arithmetic and number support; variable syntax needs variables and identifier support.

### `exclude`

Removes one selected module from a composition:

```text
exclude CSharpInterop
```

Use multiple `exclude` lines for multiple modules.

### `backend`

Enables or disables one execution backend:

```text
backend cil enable
backend interpreter enable
```

Currently documented user-facing modes are:

- `compiler`, which maps to the CIL backend when the dialect exposes `cil`;
- `interpreter`, which runs through the interpreter backend when exposed.

Some dialects expose both `cil` and `interpreter`. Others expose only one backend.

### `enable optimizer` / `disable optimizer`

Enables or disables an optimizer for a backend selector:

```text
enable optimizer ArithmeticOptimization for any
disable optimizer AggressiveInline for interpreter
```

Use `any` or `*` for all applicable backends, or a specific backend id such as `cil` or `interpreter`.

### `allow intrinsic` / `forbid intrinsic`

Allows or forbids an intrinsic for a backend selector:

```text
allow intrinsic "add_i32" for any
forbid intrinsic "reflect-call" for cil
```

This is a dialect-level intrinsic policy directive. It should match backend capability expectations.

### `security`

Declares the intended security profile:

```text
security restricted
```

or:

```text
security trusted
```

A restricted dialect is a composition constraint, not a process isolation guarantee.

### `capability`

Declares a boolean capability marker:

```text
capability interop-enabled = false
```

Capabilities explain selected composition. They must not be treated as hidden runtime activation mechanisms.

## Shipped dialect examples

Examples are located under `UniversalToolchain/Dialects/examples/wist`:

| Directory | Purpose |
|---|---|
| `full-default` | Standard Wist profile over `cil` and `interpreter`. |
| `full-default-native` | Native arithmetic/type profile over `cil` and `interpreter`. |
| `function-calls-safe-math` | Function calls plus SafeMath profile without rule declarations. |
| `minimal-arithmetic` | Smallest interpreter arithmetic profile. |
| `minimal-arithmetic-native` | Smallest native arithmetic profile over `cil`. |
| `pricing-restricted` | Composition-constrained pricing profile with a restricted runtime surface. |
| `restricted-sandbox` | Composition-constrained profile; not an isolation guarantee. |

## How it fits into the pipeline

`ComposeText` and `ComposeFile` compile a dialect definition, build a deterministic plan and resolve selected modules, optimizers and backends from runtime manifests. `CreateHost` then builds the runtime provider for that selected composition.

## Rules and constraints

- Do not document rules as an available public runtime feature. `rule-schema`, `rule-run`, raw-source RuleSet MVP parsing and `RuleDeclarationsModule` are temporarily removed.
- Keep dialect files explicit. A future reader should be able to see which syntax and backend paths are available.
- Do not treat `restricted-sandbox` as a process isolation guarantee.
- Do not add raw-source parsing workarounds for missing language features. Syntax must be owned by lexer/parser/AST/module code.
- Prefer the parser-tested v1 directive shape documented in [Dialect Reference](/reference/dialect-reference).

## Next

Continue with [Minimal DSL](/build-dsls/minimal-dsl).
