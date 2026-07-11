---
title: Runtime Profiles
description: Source-level runtime profile defaults and builder API.
---

# Runtime Profiles

Status: public preview API.

Runtime profiles apply deterministic source-level defaults before dialect composition. They do not replace runtime
component manifests and they are not deployment profiles.

## Public API

Use `RuntimeProfileDefinitionBuilder` for application-facing configuration:

```csharp
var profile = RuntimeProfileDefinitionBuilder
    .Create("release")
    .Describe("Release defaults")
    .UseModules("Arithmetic", "Numbers")
    .EnableBackend("cil")
    .EnableOptimizer("Ssa")
    .Security(SecurityProfile.Restricted)
    .Capability("safe-math")
    .Build();
```

Use `RuntimeProfileCatalogBuilder` when multiple profiles must be exposed by name:

```csharp
var catalog = RuntimeProfileCatalogBuilder
    .Create()
    .Add(RuntimeProfileDefinitionBuilder.Create("debug").EnableBackend("interpreter").Build())
    .Add(profile)
    .Build();
```

## Applied Defaults

Profiles can provide:

| Default | Dialect directive emitted when missing |
|---|---|
| modules | `use {module}` |
| backends | `backend {backend}` |
| optimizers | `enable optimizer {optimizer} for any` |
| security | `security {profile}` |
| capabilities | `capability {name} = true/false` |

The applicator records provenance for every source and profile directive. Strict mode reports conflicts such as a source
excluding a module that the selected profile would add.

## Boundaries

The generic profile model uses canonical directives. Wist applies the same structured profile through its Wist-aware applicator, which preserves compact Wist syntax such as `use A,B` and `enable Ssa` rather than reparsing generated generic DSL text.

Profiles are source defaults only. They do not:

- install assemblies;
- bypass runtime manifest selection;
- prove backend support;
- hide dialect diagnostics.
