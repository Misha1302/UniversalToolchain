---
title: Runtime Manifests
description: Explain retained runtime-manifest metadata and its boundary with canonical Wist authoring.
---

# Runtime Manifests

Runtime manifests are retained deterministic metadata artifacts. They are **not** how current public Wist execution chooses modules, optimizers or backends.

For Wist S11, the authoritative chain is:

```text
Wist feature/contribution descriptors
  → LanguageDefinition
  → LanguageCompiler
  → LanguagePlan
  → LanguageRuntime
```

## What manifests are still for

The repository keeps a generic runtime-manifest emitter/serializer and tests for compatibility, tooling and generic integration scenarios. If a project participates in that subsystem, its generated metadata must remain deterministic and exact.

A generated manifest does not automatically make a component part of a Wist `LanguagePlan`.

## Built-in Wist module authoring

A built-in Wist module needs typed canonical registration. For a new module such as `MyFeature`, the relevant owners are:

1. the module implementation (`IFrontendCoreModule`);
2. `WistFeatureIds` and `WistContributionIds`;
3. `WistRuntimeComponentCatalog.Modules`, which binds the canonical contribution to its implementation factory and alias;
4. `WistLanguageFeaturePackage.CreateFeatures()`, which exposes the feature/contribution descriptor;
5. `GetRequiredFeatures(...)` when the feature has typed dependencies;
6. tests proving the resulting `LanguagePlan` and runtime behavior.

Attributes such as `DialectModuleAlias`, `DialectRuntimeExport` or generated runtime-manifest metadata may still exist for compatibility/metadata consumers. They are not a substitute for the typed Wist LanguagePack registration path.

## Why this boundary matters

A dialect can say:

```text
dialect Demo
use MyFeature
backend interpreter
```

The Wist configuration frontend resolves `MyFeature` through the canonical Wist component catalog and translates it to a typed feature id. `LanguageCompiler` then closes feature dependencies and resolves the contribution graph. `LanguageRuntime` materializes exactly that plan.

No runtime-manifest catalog gets a second chance to add, remove or reorder Wist features.

## If you maintain the generic manifest emitter

Some projects/tests intentionally exercise the retained manifest format. In that context:

- generate metadata; do not maintain duplicate hand-written JSON unless the contract explicitly requires a fixture;
- keep assembly/type identities structured and deterministic;
- test build-target output paths on supported platforms;
- reject malformed, duplicate or ambiguous entries;
- do not introduce type-name/reflection fallbacks that silently guess activation;
- do not route public Wist execution back through manifest-selected runtime composition.

See [Runtime Manifest Format](/runtime-manifest-format) and [Runtime Manifest Activation Model](/runtime-manifest-activation-model) for the compatibility boundary.

## What to test for a Wist module

For a built-in Wist feature, tests should prove:

- the feature/contribution appears in the canonical package descriptor;
- a dialect alias translates to the expected typed feature;
- required feature dependencies are closed by `LanguageCompiler`;
- `exclude` prevents an excluded required contribution from being silently reintroduced;
- selected syntax works when the module is planned;
- omitted syntax fails when the module is not planned;
- both backends agree when the feature supports both routes;
- a minimal plan does not require unrelated module assemblies merely because they exist in the catalog.

## Troubleshooting

If a Wist dialect says an alias is unknown, check canonical registration first:

1. `WistRuntimeComponentCatalog` contains the alias with the right kind.
2. The descriptor points at the intended `WistFeatureIds`/`WistContributionIds` pair.
3. `WistLanguageFeaturePackage.CreateFeatures()` exposes that feature.
4. Typed dependencies in `GetRequiredFeatures(...)` are correct.
5. The selected backend is supported by the feature/contribution.
6. The resulting `LanguagePlan` contains the expected contribution and route.

Only inspect generated runtime-manifest artifacts when you are specifically debugging the retained generic manifest subsystem; they are not the source of truth for Wist semantic selection.
