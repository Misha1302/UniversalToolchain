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

A typical dialect file contains:

```text
dialect FullDefault
use Arithmetic,BooleanConditions,Comments,ComparisonConditions,Conditions,CSharpInterop,Equality,Identifier,Labels,Loops,Numbers,Scopes,SemicolonAsNewLine,Variables,Whitespaces
backend cil,interpreter
enable BooleanOptimization
enable ComparisonIntrinsicOptimization
security trusted
capability unsafe-interop
```

## Minimal example

The smallest shipped arithmetic dialect is:

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

Selects modules:

```text
use Arithmetic,Numbers,Scopes,Whitespaces
```

A module must be selected for its syntax and runtime behavior to exist. For example, arithmetic syntax needs arithmetic and number support; variable syntax needs variables and identifier support.

### `exclude`

Removes selected modules from a composition:

```text
exclude CSharpInterop,Identifier,InternalPreprocessorLexemes,Labels,Loops,NativeTypes,ParametersSetter,SemicolonAsNewLine,Variables
```

The shipped `restricted-sandbox` example uses this to keep the runtime surface narrower.

### `backend`

Selects execution backends:

```text
backend cil,interpreter
```

Currently documented user-facing modes are:

- `compiler`, which maps to the CIL backend when the dialect exposes `cil`;
- `interpreter`, which runs through the interpreter backend when exposed.

Some dialect examples expose both `cil` and `interpreter`. Others expose only one backend.

### `enable`

Enables optimizers:

```text
enable ArithmeticOptimization
enable EGraphOptimization
enable NativeCilOptimization
enable NativeTypesOptimization
```

Only enable optimizers that are supported by the selected modules and backend path.

### `security`

Declares the intended security profile:

```text
security restricted
```

or:

```text
security trusted
```

A restricted dialect is a composition constraint, not a hardened sandbox. Use process and environment isolation for untrusted code.

### `capability`

Declares a capability marker:

```text
capability unsafe-interop
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
| `restricted-sandbox` | Composition-constrained profile; not a hardened sandbox guarantee. |

## How it fits into the pipeline

`ComposeText` and `ComposeFile` compile a dialect definition, build a deterministic plan and resolve selected modules, optimizers and backends from runtime manifests. `CreateHost` then builds the runtime provider for that selected composition.

## Rules and constraints

- Do not document rules as an available public runtime feature. `rule-schema`, `rule-run`, raw-source RuleSet MVP parsing and `RuleDeclarationsModule` are temporarily removed.
- Keep dialect files explicit. A future reader should be able to see which syntax and backend paths are available.
- Do not treat `restricted-sandbox` as a real security sandbox.
- Do not add raw-source parsing workarounds for missing language features. Syntax must be owned by lexer/parser/AST/module code.

## Next

Continue with [Minimal DSL](/build-dsls/minimal-dsl).
