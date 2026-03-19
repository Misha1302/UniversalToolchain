# Runtime resolution and apply seams

## Runtime resolution seam

`IDialectRuntimeCompositionResolver` resolves a validated `DialectBuildPlan` against an explicit
`DialectRuntimeDescriptorRegistry`.

Result: `DialectRuntimeComposition`.

Properties of this seam:

- explicit inputs only
- deterministic output shape
- explicit unresolved-descriptor diagnostics
- no hidden global mutation

## Apply seam

`DialectApplyDescriptionBuilder` converts a **resolved** `DialectRuntimeComposition` into `DialectApplyDescription`.

Result: a deterministic, read-only description of what future runtime wiring would apply.

Properties of this seam:

- opt-in only
- rejects unresolved runtime composition
- does not modify DI container
- does not instantiate modules/backends

## Why both seams exist

Separating resolution from apply description keeps concerns reviewable:

- resolution = "what runtime entities are valid for this dialect"
- apply description = "what would be wired if caller chooses to apply"

Concrete host activation remains a future integration layer.
