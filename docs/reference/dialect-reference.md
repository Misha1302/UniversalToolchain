---
title: Dialect Reference
description: Document the current runtime .wistdialect format used by shipped Wist profiles.
---

# Dialect Reference

This page documents the current public `.wistdialect` format used by the Wist runtime path.

The source of truth for public Wist dialect execution is the manifest-backed runtime workflow:

```text
dialect source -> dialect compilation -> build plan -> manifest-backed runtime selection -> host creation -> execution
```

The compiler used by that path is `DialectDslCompiler` through the Wist dialect execution workflow. The shipped dialect profiles under `UniversalToolchain/Dialects/examples/wist` use this format and should be treated as executable references.

## Document shape

A dialect file starts with a required header:

```text
dialect <Name>
```

Directives then follow one per line. Most selection directives accept comma-separated identifier lists.

## Minimal runtime dialect

The shipped minimal arithmetic dialect uses this shape:

```text
dialect MinimalArithmetic
use Arithmetic,Numbers,Scopes,Whitespaces
backend interpreter
```

This selects only the modules required for basic arithmetic and exposes only the interpreter backend.

## Full profile example

A broader Wist profile can select many modules and both built-in backend ids:

```text
dialect FullDefault
use Arithmetic,BooleanConditions,Comments,ComparisonConditions,Conditions,CSharpInterop,Equality,Identifier,Labels,Loops,Numbers,Scopes,SemicolonAsNewLine,Variables,Whitespaces
backend cil,interpreter
enable BooleanOptimization
enable ComparisonIntrinsicOptimization
security trusted
capability unsafe-interop
```

The user-facing CLI alias `cil` selects the `cil` backend when the active dialect exposes it. Dialect files should still declare the backend id `cil`.

## Directive reference

### `dialect`

Declares the dialect name:

```text
dialect PricingRestricted
```

The name is used in diagnostics, composition output and execution configuration.

### `use`

Selects one or more module aliases:

```text
use Arithmetic,Numbers,Scopes,Whitespaces
```

You may also split module selection across several `use` lines when that is clearer:

```text
use Arithmetic,Numbers
use Scopes,Whitespaces
```

A module must be selected for the syntax and runtime behavior it owns to exist.

### `exclude`

Excludes one or more module aliases from the composition:

```text
exclude CSharpInterop
```

Use this when building from a broader base/profile and intentionally removing a runtime surface.

### `requires`, `before`, `after`

Declares ordering constraints over a comma-separated module chain:

```text
requires Variables,Scopes
before Conditions,Labels
after Loops,Labels
```

The runtime frontend lowers adjacent items in the list into pairwise order directives. Use these directives sparingly; prefer coherent module selections and deterministic module metadata when possible.

### `backend`

Selects one or more backend ids:

```text
backend interpreter
backend cil,interpreter
```

Currently shipped Wist backends include:

| Dialect backend id | User-facing mode |
|---|---|
| `interpreter` | `interpreter` |
| `cil` | `cil` |

Backend identifiers are not a closed conceptual model. A backend is usable only when the runtime catalog contains a matching backend manifest entry and registrar.

### `enable` / `disable`

Enables or disables one optimizer alias:

```text
enable ArithmeticOptimization
disable EGraphOptimization
```

Optimizer directives target all applicable backends in the current runtime dialect frontend. Do not use an optimizer to hide missing base semantics.

### `allow` / `forbid`

Allows or forbids one intrinsic name:

```text
allow add_i32
forbid reflect-call
```

Intrinsic policy should match backend capability expectations. Backend-specific intrinsics must not leak into backend-agnostic runtime surfaces without an explicit capability story.

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
capability unsafe-interop
capability pricing-formula,user-authored
```

Capabilities explain selected composition. They must not be treated as hidden runtime activation mechanisms.

## Current syntax boundary

The repository also contains `DialectDefinitionParser`, which accepts a stricter parser-specific shape such as:

```text
backend interpreter enable
capability supports-floats = true
```

That parser is not the current public runtime `.wistdialect` contract used by shipped Wist profiles. Do not document its syntax as canonical for Wist runtime execution unless the runtime path is intentionally migrated to it.

## Compatibility rule

Public documentation, shipped examples and CLI onboarding must use the same dialect shape as the manifest-backed Wist runtime path. If the runtime parser changes in the future, update shipped `.wistdialect` files and this reference together.

## Related pages

- [Dialect Files](/build-dsls/dialect-files)
- [Module Reference](/reference/module-reference)
- [Backend Contracts](/reference/backend-contracts)
