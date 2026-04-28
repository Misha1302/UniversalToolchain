# Dialect Groups

Dialect groups are compile-time configuration aliases.
They reduce dialect file repetition without changing the canonical runtime activation model.

A group expands into ordinary module and capability directives before a `DialectBuildPlan` is created.
Runtime selection still uses the normalized build plan and runtime manifests.

## Design rules

Dialect groups are intentionally small:

- they may include module aliases;
- they may enable or disable boolean capabilities;
- they do not select backends;
- they do not define security policy;
- they do not activate services;
- they are not runtime components;
- they are not emitted as runtime manifest entries.

This keeps groups as an optional convenience layer instead of a hidden source of framework behavior.

## Example

Instead of writing:

```wist
use Arithmetic,Numbers,Whitespaces
```

A dialect can write:

```wist
use ArithmeticCore
```

The group is expanded before runtime selection into the concrete module aliases declared by the group provider.
If a `use` alias does not resolve to a known group, it remains a normal module alias and is resolved by the existing runtime selection path.

## Shipped Wist groups

The Wist integration currently provides these data-only groups:

- `ArithmeticCore` -> `Arithmetic`, `Numbers`, `Whitespaces`
- `ConditionsCore` -> `BooleanConditions`, `ComparisonConditions`, `Conditions`, `Equality`
- `VariablesCore` -> `Identifier`, `Variables`
- `BlocksCore` -> `Scopes`, `SemicolonAsNewLine`
- `ControlFlowCore` -> `Loops`, `Labels`

## Conflict behavior

If a group capability conflicts with an explicit capability directive, dialect composition reports a deterministic diagnostic instead of silently overriding the value.

For example, if a group enables `Arithmetic = true` and the dialect explicitly sets `Arithmetic = false`, the build plan becomes invalid.

## Non-goals

Dialect groups are not the full Feature System described in roadmap documents.
They are a smaller polish step that improves dialect ergonomics while keeping runtime activation manifest-backed and selected from the normalized build plan.
