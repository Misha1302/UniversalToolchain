# Dialect Groups

Dialect groups are compile-time aliases for lists of Wist module aliases. They reduce `.wistdialect` repetition without becoming runtime components or a second planning layer.

A group is expanded by the Wist configuration frontend before `LanguageDefinition` is created. `LanguageCompiler` then performs the real feature dependency closure and contribution/backend planning.

## Current model

The canonical path is:

```text
group alias
  -> concrete Wist module aliases
  -> typed selected features
  -> LanguageDefinition
  -> LanguageCompiler
  -> LanguagePlan
```

The current `WistDialectGroupCatalog` is data-only: each group maps to an ordered list of module aliases. Groups do not carry capability, backend, security or runtime-service behavior.

## Design rules

Dialect groups:

- may include module aliases;
- do not select backends;
- do not define security policy;
- do not activate services;
- do not become contributions themselves;
- do not bypass typed feature dependencies;
- do not decide runtime order independently of `LanguageCompiler`.

This keeps them as an ergonomic input shorthand instead of a hidden source of framework behavior.

## Example

Instead of writing:

```wist
use Arithmetic,Numbers,Whitespaces
```

A dialect can write:

```wist
use ArithmeticCore
```

`ArithmeticCore` expands to the same three aliases before Wist translates them to typed features. If an alias is not a known group, it remains a normal module alias and is resolved through the canonical Wist component catalog.

## Shipped Wist groups

The current built-in groups are:

- `ArithmeticCore` -> `Arithmetic`, `Numbers`, `Whitespaces`
- `ConditionsCore` -> `BooleanConditions`, `ComparisonConditions`, `Conditions`, `Equality`
- `VariablesCore` -> `Identifier`, `Variables`
- `BlocksCore` -> `Scopes`, `SemicolonAsNewLine`
- `ControlFlowCore` -> `Loops`, `Labels`

These values are owned by `UniversalToolchain.Wist.LanguagePack/WistDialectGroupCatalog.cs`.

## Interaction with dependencies

Group expansion is not dependency closure.

For example, a group may request `Variables`, while the typed `Variables` feature declares additional required features. Those requirements are closed by `LanguageCompiler` after group expansion.

Do not encode transitive planner logic into the group catalog merely to mirror the final plan. Keep groups readable and let typed feature descriptors own dependency semantics.

## Interaction with `exclude`

`exclude` group/module aliases are expanded through the same group catalog. The resulting module contribution ids are placed in `LanguageDefinition.ExcludedContributions`.

A dialect cannot both request and exclude the same expanded module alias. If dependency closure later requires an excluded contribution, canonical planning fails closed.

## Non-goals

Dialect groups are not:

- a runtime manifest;
- a selected runtime plan;
- a capability-policy language;
- backend configuration;
- the full generic Feature SDK.

They are a Wist-facing shorthand that ends before `LanguageCompiler` begins semantic planning.
