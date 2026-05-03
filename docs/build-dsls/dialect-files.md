---
title: Dialect Files
description: Document the runtime dialect file format and its role.
---

# Dialect Files

Dialect files describe which modules, optimizers, capabilities and backends are available for a Wist runtime composition.

## Problem

A reusable DSL framework needs a way to select language features without hardcoding them into one compiler. Dialect files are that selection layer. They let a DSL developer assemble a runtime surface from existing modules and make the selected surface explicit.

## Concept

A Wist dialect file uses the `.wistdialect` extension. It is compiled before program execution and becomes part of the runtime selection path:

```text
dialect source → dialect compilation → build plan → manifest-backed runtime selection → host creation → execution
```

The public runtime dialect format used by shipped Wist profiles is intentionally compact. Selection directives accept comma-separated identifier lists:

```text
dialect FullDefault
use Arithmetic,BooleanConditions,Comments,ComparisonConditions,Conditions,CSharpInterop,Equality,Identifier,Labels,Loops,Numbers,Scopes,SemicolonAsNewLine,Variables,Whitespaces
backend cil,interpreter
enable BooleanOptimization
enable ComparisonIntrinsicOptimization
security trusted
capability unsafe-interop
```

This is the format used by the executable examples under `UniversalToolchain/Dialects/examples/wist`.

## Minimal example

A minimal arithmetic dialect looks like this:

```text
dialect MinimalArithmetic
use Arithmetic,Numbers,Scopes,Whitespaces
backend interpreter
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

Selects one or more module aliases:

```text
use Arithmetic,Numbers,Scopes,Whitespaces
```

A module must be selected for its syntax and runtime behavior to exist. For example, arithmetic syntax needs arithmetic and number support; variable syntax usually needs variables and identifier support.

### `exclude`

Removes one or more modules from a composition:

```text
exclude CSharpInterop
```

Use this when deriving a narrower profile from a broader one.

### `backend`

Selects one or more execution backend ids:

```text
backend interpreter
backend cil,interpreter
```

Currently documented user-facing modes are:

- `compiler`, which maps to the CIL backend when the dialect exposes `cil`;
- `interpreter`, which runs through the interpreter backend when exposed.

Some dialects expose both `cil` and `interpreter`. Others expose only one backend.

### `enable` / `disable`

Enables or disables an optimizer alias:

```text
enable ArithmeticOptimization
disable EGraphOptimization
```

Optimizer directives apply to the current runtime dialect frontend policy. Enable optimizers after base semantics are already correct.

### `allow` / `forbid`

Allows or forbids an intrinsic name:

```text
allow add_i32
forbid reflect-call
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

Declares one or more enabled capability markers:

```text
capability interop-enabled
capability pricing-formula,user-authored
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
- Keep public dialect examples aligned with the shipped runtime dialect frontend, not with secondary parser experiments.

## Next

Continue with [Minimal DSL](/build-dsls/minimal-dsl).
