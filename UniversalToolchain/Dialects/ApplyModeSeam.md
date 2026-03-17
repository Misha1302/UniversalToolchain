# Apply-mode seam for Dialect Definition DSL

## Why this seam exists

Dialect parsing, semantic normalization, and runtime descriptor resolution already produce a deterministic `DialectRuntimeComposition`.

The new apply-mode seam introduces one explicit extra step:

- `IDialectApplyDescriptionBuilder`
- `DialectApplyDescriptionBuilder`
- `DialectApplyDescription`

This step converts a **resolved** composition into a pure apply description that says what would be wired later (frontend modules, IR modules, optimizers, backends, intrinsic permissions).

The seam is intentionally explicit and opt-in. Existing compilation/interpreter flows do not change unless a caller chooses to build and use apply-mode output.

## Why it is intentionally limited

The seam does **not**:

- mutate the DI container,
- auto-register services,
- scan assemblies,
- activate modules,
- enforce runtime behavior in unrelated systems.

It is a deterministic description object only. This keeps architecture boundaries clear while creating a clean integration point for future runtime activation work.

## What future real integration still needs

A future production apply system can consume `DialectApplyDescription` and add concrete wiring policies, for example:

- explicit service registration strategy,
- lifecycle and ordering enforcement,
- conflict handling with existing runtime options,
- security/profile gate enforcement at activation time,
- audit/logging and dry-run support.

Those concerns are intentionally not implemented in this step.
