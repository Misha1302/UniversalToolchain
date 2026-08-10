---
title: Dialect Files
description: Document the runtime dialect file format and its role.
---

# Dialect Files

Dialect files are the Wist-facing configuration syntax for requesting language features, optimizers, policy and backends. They do not create a second runtime plan.

## Problem

A reusable language framework needs a way to request a language surface without hardcoding one fixed compiler configuration. Wist `.wistdialect` files provide that user-facing selection syntax, while the generic language stack remains the semantic authority.

## Canonical flow

A Wist dialect file uses the `.wistdialect` extension. The current public path is:

```text
dialect source
  → DialectDslCompiler
  → Wist LanguageDefinition translation
  → LanguageCompiler
  → LanguagePlan
  → LanguageRuntime
  → execution/build
```

The Wist translation layer maps aliases and policy into typed generic contracts. `LanguageCompiler` alone closes feature dependencies, resolves contributions/providers, applies exclusions and ordering constraints, and creates backend artifact routes. `LanguageRuntime` materializes the exact graph from that plan.

The compact source format used by shipped Wist profiles looks like:

```text
dialect FullDefault
use Arithmetic,BooleanConditions,Comments,ComparisonConditions,Conditions,CSharpInterop,Equality,Identifier,Labels,Loops,Numbers,Scopes,SemicolonAsNewLine,Variables,Whitespaces
backend cil,interpreter
enable BooleanOptimization
enable ComparisonIntrinsicOptimization
security trusted
capability unsafe-interop
```

Executable examples live under `UniversalToolchain/Dialects/examples/wist`.

## Minimal example

```text
dialect MinimalArithmetic
use Arithmetic,Numbers,Scopes,Whitespaces
backend interpreter
```

It can run:

```wist
2 + 3 * 4
```

with expected result:

```text
14
```

## Directives

### `dialect`

Declares the dialect name:

```text
dialect MinimalArithmetic
```

The name is retained in language metadata for diagnostics and provenance.

### `use`

Requests one or more canonical Wist module aliases:

```text
use Arithmetic,Numbers,Scopes,Whitespaces
```

The textual list is not the final dependency graph. Features declare typed dependencies, and `LanguageCompiler` closes them deterministically. A caller therefore does not need to duplicate every transitive requirement merely to keep the runtime valid.

### `exclude`

Marks one or more module contributions as unavailable:

```text
exclude CSharpInterop
```

The translator maps these aliases to `LanguageDefinition.ExcludedContributions`. If dependency closure requires an excluded contribution, planning fails with a canonical planning diagnostic instead of silently reactivating it.

The current public Wist facade does not implement base-dialect inheritance, so `exclude` is a fail-closed constraint on the current definition rather than a hidden profile-inheritance mechanism.

### `backend`

Declares one or more backend ids:

```text
backend interpreter
backend cil,interpreter
```

Currently documented user-facing modes are:

- `cil`, which maps to the CIL backend;
- `interpreter`, which maps to the interpreter backend.

A declared backend becomes executable only if canonical planning resolves its backend contribution and artifact route.

### `enable` / `disable`

Enables or disables an optimizer alias:

```text
enable ArithmeticOptimization
disable EGraphOptimization
```

Enabled optimizer aliases become typed selected features. Ordering and placement belong to `LanguageCompiler` and the planned route.

### `allow` / `forbid`

Allows or forbids one intrinsic name:

```text
allow add_i32
forbid reflect-call
```

These are translated to typed intrinsic policy directives before planning.

### `security`

Declares the intended security profile:

```text
security restricted
```

or:

```text
security trusted
```

The profile is translated to typed runtime policy/features. A restricted dialect is a composition/host-interop constraint, not a process-isolation guarantee.

### `capability`

Declares a supported Wist configuration capability:

```text
capability unsafe-interop
capability composition-restricted
```

Supported capability names map to typed policy/features. Unknown names fail rather than becoming hidden activation switches. `unsafe-interop` requires `security trusted`.

## Shipped dialect examples

Examples are located under `UniversalToolchain/Dialects/examples/wist`:

| Directory | Purpose |
|---|---|
| `full-default` | Standard Wist profile over `cil` and `interpreter`. |
| `full-default-native` | Native arithmetic/type profile over `cil` and `interpreter`. |
| `function-calls-safe-math` | Function calls plus SafeMath profile without rule declarations. |
| `minimal-arithmetic` | Small interpreter arithmetic profile. |
| `minimal-arithmetic-native` | Small native arithmetic profile over `cil`. |
| `pricing-restricted` | Restricted pricing profile. |
| `composition-restricted` | Composition-constrained profile; not an isolation guarantee. |

## How it fits into the runtime

For public execution, `WistEngine.Create` resolves the dialect source to one `LanguageDefinition`, compiles it once to one `LanguagePlan`, and creates one `LanguageRuntime` from exact component sources. `Evaluate`, `Validate` and `Compile` reuse that canonical plan/runtime instead of invoking another composition workflow.

Runtime materialization may instantiate only the components selected by the plan. It does not select extra features, reorder contributions or infer a second backend plan.

## Rules and constraints

- `LanguageCompiler` is the only semantic planner.
- Do not document `DialectBuildPlan`, `SelectedRuntimePlan`, manifest-backed Wist runtime selection or `WistDialectExecutionWorkflow` as current production architecture; those owners are retired in S11.
- Keep aliases stable and map them to typed feature/contribution IDs.
- Treat `exclude` as a canonical planning constraint, not as text-only documentation.
- Do not treat `composition-restricted` as a process-isolation guarantee.
- Do not add raw-source parsing workarounds for missing language features. Syntax must be owned by lexer/parser/AST/module code.
- Do not document removed rule-schema/rule-run surfaces as public runtime features.
- Keep shipped examples, CLI behavior and this documentation aligned with the same Wist configuration frontend and generic planning/runtime contracts.
