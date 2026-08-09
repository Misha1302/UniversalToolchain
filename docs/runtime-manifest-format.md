# Runtime Manifest Format

Runtime manifests are deterministic metadata artifacts for the repository's retained generic dialect runtime-manifest infrastructure.

> They are **not** the canonical semantic-selection mechanism for public Wist execution. Current Wist execution is owned by `LanguageDefinition -> LanguageCompiler -> LanguagePlan -> LanguageRuntime`.

## Manifest role

A runtime manifest can describe runtime components exported by an assembly for compatibility, tooling or generic integration scenarios. The serializer/emitter contracts keep this metadata explicit and deterministic.

For current Wist, built-in feature/contribution selection is declared through the typed Wist LanguagePack/catalog and resolved by `LanguageCompiler`; `LanguageRuntime` then binds exact component sources from the resulting plan. A runtime manifest must not override that plan.

## Component entry model

Each retained manifest component entry conceptually declares:

- `kind` such as `FrontendModule`, `Optimizer` or `Backend`;
- `canonicalAlias` and optional aliases;
- stable `componentId`;
- owner `assemblySimpleName`;
- optional activation metadata.

## Structured type references

Activation metadata uses structured type references:

- `assemblySimpleName`;
- `typeFullName`.

This applies to activation types and, where the generic manifest model supports them, backend registrar types.

Structured references prevent ambiguous unqualified type lookup.

## Canonical emission and exact loading

The retained emitter writes structured activation references. The serializer requires the current structured representation with explicit assembly/component/type identity; unsupported older shapes must fail or be migrated explicitly rather than guessed.

Tests for this subsystem should verify:

- deterministic serialization/emission;
- exact structured identities;
- duplicate/malformed entry rejection;
- no guessed output paths or silent type-name fallbacks;
- cross-platform build-target behavior.

## Boundary with Wist S11

Do not use this format to reintroduce any of the retired Wist sequence:

```text
build plan -> selected runtime plan -> manifest-selected backend/module activation -> Wist execution host
```

For Wist, the authoritative selected set and route order are in `LanguagePlan`. Runtime factories may use ordinary typed implementation metadata to construct the planned components, but no manifest catalog gets a second chance to change the language semantics.
