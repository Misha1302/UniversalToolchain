# External language composition hardening — 2026-07-22

## Why this revision exists

The previous authoring alpha made non-Wist typed languages possible, but review exposed a gap between the graph accepted by planning and the implementations used at runtime. Metadata from independently shipped packages could compose while one package-local runtime provider still lacked the other packages' transformer implementations. Several adjacent contracts were also incomplete: same-artifact compiler passes, feature ownership during capability selection, exact package-version identity, executor alternatives and legacy policy enforcement.

## Architectural delta

This revision establishes one plan as the source of truth from registration through execution:

- authored packages export immutable descriptors and immutable runtime component catalogs;
- `LanguageRouteRuntimeAssembler` gathers components from every package used by the selected route;
- each runtime source is bound to the exact package descriptor SHA-256 captured by the plan;
- the selected runtime-provider package is required even when the provider contribution has no transformer or executor;
- transformers and executors are checked against exact planned contribution, backend and artifact contracts;
- multiple executors may implement one contribution when backend/input keys differ;
- same-artifact transformations are modeled as deterministic compiler passes with `Before`/`After` constraints;
- selected passes that cannot occur on a route fail with `UTL2204` instead of disappearing;
- capability selection cannot activate contributions owned only by unselected features;
- package-level contributions support backend, optimizer and infrastructure extension packages, including featureless executable compositions;
- negative runtime limits are rejected and whitespace source is delegated to the language frontend;
- CLR-derived artifact identities recursively exclude assembly versions, while public packages may declare an explicit protocol identity;
- Wist's compatibility provider accepts only the canonical Wist route and rejects custom routes it cannot execute faithfully;
- legacy runtime paths use the same fail-closed policy requirements;
- Wist descriptor/provider/NuGet versions derive from one build metadata source;
- the Wist manifest emitter uses `$(DOTNET_HOST_PATH)` instead of a PATH-dependent `dotnet` command;
- package and lock manifests advance to schema v4 while readers retain schema v1-v3 compatibility.

## Compatibility

The generic SDK remains independent of Wist. Existing typed authoring code remains source-compatible except for the unsafe parameterless authored-package runtime-provider helper, which was removed so callers cannot bypass plan-aware cross-package assembly. Use:

```csharp
using var runtime = LanguageRuntime.Create(
    plan,
    new ILanguageRouteComponentSource[] { packageA, packageB });
```

or `LanguageRouteRuntimeAssembler.CreateProvider(plan, sources)`.

## Honest boundary

The SDK now provides a trustworthy typed contribution, pass, route and runtime-composition layer. It is still not a declarative grammar/type-system workbench and does not claim hostile extension sandboxing or stable 1.0 compatibility.
