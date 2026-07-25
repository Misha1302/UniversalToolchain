# Migration notes

## Obsolete APIs and replacements

| Legacy | Replacement | Gate |
|---|---|---|
| `LanguageRuntimePackReference` | `LanguageRuntimeProviderReference` | UTL-DEP-001 |
| `LanguageDefinitionBuilder.UseRuntimePack` | `UseRuntimeProvider` | UTL-DEP-002 |
| `ILanguageRuntimePack` | `ILanguageRuntimeProvider` + registry | UTL-DEP-003 |
| `LanguageRuntime.Create(..., ILanguageRuntimePack, ...)` | provider/registry overload | UTL-DEP-004 |
| `WistLanguageRuntimePack` | `WistLanguageRuntimeProvider` | UTL-DEP-005 |
| `WistContributionIds.ConditionsModule` | split conditions contributions | UTL-DEP-006 |
| untyped route constructors | typed `LanguageArtifactContract` constructors | UTL-DEP-007/008 |
| `EnumGenerator` | `ExtensibleEnum<TTag>` or scoped catalog | UTL-DEP-009 |

Canonical machine-readable owner: `LEGACY_DEPRECATION_REGISTRY.json`. Extended guide: `docs/migration/WIST_LEGACY_MIGRATION_RU.md`.

## Behavioral changes

- `LanguagePlan` is no longer publicly constructible as an executable artifact; compile and verify through Language SDK.
- Wist contribution metadata cannot select modules/backends or inject dialect statements.
- Ambiguous runtime-provider constructors are rejected rather than selected by declaration order.
- Invalid runtime conversions throw classified failures instead of silently returning/falling back.
- `SetAndList.Add` is idempotent for an existing value.
- `EnumGenerator.GetName(int)` no longer exposes insertion-order identity.

## Plan/provenance verification changes

- Package registry returns an opaque registration identity.
- Canonical hash is always recomputed over normalized plan content.
- Runtime verification requires matching registry identity, manifest digest, selected contributions and routes.
- A serialized plan/envelope is not trusted after deserialization; it must be recompiled/reverified against the current registry.

## Diagnostic configuration changes

- `Strict`: typed contract exception blocks execution.
- `Warn`: requires an observable `IModuleContractDiagnosticSink`.
- `Off`: explicitly disables verification and is the only mode where a null sink is allowed.
- Per-execution override wins; otherwise validated host defaults are used.

## Runtime/lifecycle changes

- Runtime state transitions `Running -> Disposing -> Disposed`.
- New operations are rejected once disposal begins.
- External concurrent disposals share the same completion.
- Disposal from a context that owns an in-flight lease throws immediately to prevent self-deadlock.

## Compatibility shims

- `wist.conditions` remains a legacy aggregate over comparisons, boolean logic and conditional control flow.
- Legacy runtime-pack APIs remain available with stable deprecation IDs.
- Legacy `compiler` backend alias remains accepted; public typed backend ID is `cil`.
- Debug/explain text rendering remains available, but is not fed back into execution.

## Removal gates and target versions

- Warning-as-error: not before `0.5.0`, and only after usage assessment.
- Removal: not before `1.0.0`, and only after all exit criteria in the registry.
- Generic Wist replacement is currently blocked: only `minimal-arithmetic` is `Equivalent`; other shipped presets remain `Partial` and optimizers are `Missing`.
