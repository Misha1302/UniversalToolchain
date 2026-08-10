# Writing Backends

A backend is not a closed built-in choice in Wist. It is a runtime component selected by a dialect file, resolved through the runtime catalog, and activated through a backend registrar declared in the backend manifest.

This page describes the contract for adding a third backend without modifying the core Wist runtime pipeline.

## Required shape

A backend package needs three things:

1. A backend declaration exported as a runtime component.
2. A backend runtime registrar.
3. A runtime manifest deployed next to the backend assembly or into another configured runtime artifact search root.

The core pipeline must not learn a new enum value, switch case, or hardcoded backend name for the backend.

## Backend declaration

The declaration is metadata. It gives the backend a stable canonical id, optional aliases, and the registrar type that knows how to wire the backend into DI.

```csharp
[DialectRuntimeExport("Backend", "my-backend")]
[DialectRuntimeAlias("my")]
[DialectBackendRegistrarType(typeof(MyBackendRuntimeRegistrar))]
public sealed class MyBackendDeclaration : DialectBackendDeclaration
{
    public override DialectBackendId BackendId => new("my-backend");
}
```

The canonical alias in `DialectRuntimeExport` is the runtime backend id. Aliases are convenience names and must not become separate backend identities.

## Backend registrar

The registrar is the activation point for a selected backend. It receives the backend-specific runtime configuration and registers the runtime services required by that backend.

```csharp
public sealed class MyBackendRuntimeRegistrar : IDialectBackendRuntimeRegistrar
{
    public DialectBackendId BackendId => new("my-backend");

    public IReadOnlyList<string> SupportedIntrinsics => [];

    public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
    {
        // Register backend cil, executor, runtime wrappers, and backend-specific services here.
    }
}
```

Do not build a backend by recreating an end-to-end BasicCore pipeline. Backends should implement the backend-specific runtime/route contract and be selected through the language plan; shared frontend/lowering mechanics remain separate stage services.

## Manifest emission

Runtime manifests are emitted from assemblies that opt into manifest emission:

```xml
<PropertyGroup>
    <EmitDialectRuntimeManifest>true</EmitDialectRuntimeManifest>
</PropertyGroup>
```

The emitted `*.dialect.runtime.json` file must travel with the backend assembly. The runtime catalog reads manifests, builds deterministic alias maps, and resolves selected backend entries from dialect build plans.

## Dialect selection

A dialect file selects the backend by canonical id or alias:

```text
dialect my-dialect
backend my-backend enable
```

or:

```text
dialect my-dialect
backend my enable
```

The runtime selection should still resolve to the canonical backend id `my-backend`.

## What must not be required

Adding a third backend must not require any of the following:

- adding a value to a central backend enum;
- editing parser switches for a specific backend id;
- adding `if backend == "cil"` / `if backend == "interpreter"` style branches;
- registering every possible backend eagerly in the Wist service provider;
- treating aliases as independent backend identities.

## Diagnostics to expect

If a selected backend is not present in the runtime catalog, composition should fail with a runtime resolution diagnostic similar to:

```text
R002 Runtime backend descriptor 'my-backend' was not registered.
```

If the manifest declares a registrar type that does not implement `IDialectBackendRuntimeRegistrar`, backend activation should fail fast with a clear error.

If the registrar returns a backend id different from the manifest canonical alias, activation should fail. The manifest and registrar must describe the same backend identity.
