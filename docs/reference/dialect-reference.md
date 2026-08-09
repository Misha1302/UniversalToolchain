---
title: Dialect Reference
description: Document the current runtime .wistdialect format used by shipped Wist profiles.
---

# Dialect Reference

This page documents the current public `.wistdialect` format used by the Wist runtime path.

The public execution path is:

```text
.wistdialect source
  -> DialectDslCompiler
  -> Wist LanguageDefinition translation
  -> LanguageCompiler
  -> one immutable LanguagePlan
  -> LanguageRuntime
  -> planned backend route
```

`DialectDslCompiler` parses the configuration syntax. `WistFacadeLanguageDefinitionFactory` only translates Wist-facing names and policy into generic language contracts; `LanguageCompiler` is the sole owner of dependency closure, contribution/provider selection, exclusions, ordering and backend routes. The shipped profiles under `UniversalToolchain/Dialects/examples/wist` use this format and are executable references.

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

This requests arithmetic semantics and the interpreter backend. The canonical planner closes any typed feature dependencies required by the selected features; runtime activation follows the resulting `LanguagePlan`, not the textual order of `use` entries.

## Full profile example

A broader Wist profile can request many modules and both built-in backend ids:

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

The name is retained in language metadata for diagnostics and provenance.

### `use`

Requests one or more canonical Wist module aliases:

```text
use Arithmetic,Numbers,Scopes,Whitespaces
```

You may split module selection across several `use` lines:

```text
use Arithmetic,Numbers
use Scopes,Whitespaces
```

`use` is an input to typed planning, not a second runtime plan. A requested feature may declare required features; `LanguageCompiler` closes those dependencies deterministically. Therefore the final `LanguagePlan` can contain required contributions that were not redundantly repeated in the source file.

### `exclude`

Marks one or more module contributions as explicitly unavailable:

```text
exclude CSharpInterop
```

Exclusions are translated to `LanguageDefinition.ExcludedContributions`. If a selected feature or dependency requires an excluded contribution, canonical planning fails closed rather than silently re-enabling it. A dialect may not both `use` and `exclude` the same expanded module alias.

The current Wist facade does not implement base-dialect inheritance. `exclude` is therefore a constraint on the current definition and dependency closure, not a hidden inheritance/subtraction mechanism.

### `requires`, `before`, `after`

Declares ordering constraints over a comma-separated module chain:

```text
requires Variables,Scopes
before Conditions,Labels
after Loops,Labels
```

The Wist translation layer converts these aliases to typed `LanguageContributionOrderConstraint` values. `LanguageCompiler` owns validation and final ordering. Groups cannot be targeted as one contribution; order their expanded module aliases explicitly.

### `backend`

Declares one or more backend ids available to this dialect:

```text
backend interpreter
backend cil,interpreter
```

Currently shipped Wist backends include:

| Dialect backend id | User-facing mode |
|---|---|
| `interpreter` | `interpreter` |
| `cil` | `cil` |

A backend is executable only when canonical planning resolves its backend contribution and produces a route from the language entry artifact to the backend input contract. Runtime materialization then binds the exact executor selected by that plan.

### `enable` / `disable`

Enables or disables one optimizer alias:

```text
enable ArithmeticOptimization
disable EGraphOptimization
```

Enabled optimizer aliases become selected optimization features. Optimizer order and applicability are resolved by `LanguageCompiler` and the planned artifact route; optimizer directives must not be used to hide missing base semantics.

### `allow` / `forbid`

Allows or forbids one intrinsic name:

```text
allow add_i32
forbid reflect-call
```

These directives become typed intrinsic policy entries on `LanguageDefinition`. Backend-specific intrinsics must not leak into backend-agnostic runtime surfaces without explicit capability support.

### `security`

Declares the intended security profile:

```text
security restricted
```

or:

```text
security trusted
```

The profile is translated to typed runtime policy/features before planning. A restricted dialect constrains composition and host interop; it is not a process-isolation guarantee.

### `capability`

Declares supported Wist configuration capabilities:

```text
capability unsafe-interop
capability composition-restricted
```

The public Wist translator maps supported capability names to typed policy/features. Unknown capabilities fail instead of becoming hidden runtime activation switches. `unsafe-interop` requires `security trusted`.

## Current syntax boundary

The repository also contains `DialectDefinitionParser`, which accepts a stricter parser-specific shape such as:

```text
backend interpreter enable
capability supports-floats = true
```

That parser is not the public `.wistdialect` configuration contract used by shipped Wist profiles. Do not document its syntax as canonical for Wist execution unless the public Wist configuration frontend is intentionally migrated to it.

## Compatibility rule

Public documentation, shipped examples and CLI onboarding must describe the same single-planner path: dialect syntax is translated to `LanguageDefinition`; `LanguageCompiler` produces the only semantic `LanguagePlan`; `LanguageRuntime` materializes and executes that exact plan. Do not reintroduce the retired manifest-backed selected-runtime workflow as a compatibility explanation.

## Related pages

- [Dialect Files](/build-dsls/dialect-files)
- [Module Reference](/reference/module-reference)
- [Backend Contracts](/reference/backend-contracts)
