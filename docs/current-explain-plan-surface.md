# Current explain-plan surface

`UniversalToolchain.Dialects.Integration` now exposes a generic explainability pipeline:

1. `DialectFrameworkCompositionResult`
2. `DialectCompositionExplanationProjector`
3. `DialectCompositionExplanation`
4. `DialectCompositionExplanationFormatter`

## What this layer does

- Projects immutable explanation snapshots from existing composition artifacts.
- Reuses existing domain types (`DialectBuildPlan`, `IDialectRuntimeSelection`, diagnostics, directives, runtime entries).
- Formats deterministic engineering text from the explanation snapshot.
- Preserves canonical producer order for ordered module and diagnostics sequences.

## Runtime selection projection model

- Runtime selections are always projected with `SelectionKind`, `IsResolved`, and diagnostics.
- Resolved runtime components are projected only when selection implements `IDialectResolvedRuntimeSelection`.
- Unknown/non-component runtime selections are preserved without invented module/backend/optimizer entries.

## Scope boundary

This surface is the current explainability and deterministic projection state.
It is not a feature graph, not a language constructor model, and not a replacement source of truth for composition.
