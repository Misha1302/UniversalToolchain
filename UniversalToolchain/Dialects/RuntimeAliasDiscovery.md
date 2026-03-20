# Runtime alias discovery

Dialect runtime aliases are now declared on the runtime component itself instead of inside a provider-side alias table.

## Declaration rules

- Frontend/runtime modules use `DialectModuleAliasAttribute`.
- Optimizers use `DialectOptimizerAliasAttribute`.
- Backends use `DialectBackendAliasAttribute` on a `DialectBackendDeclaration` type.
- Attributes may declare one or many aliases and may be applied multiple times.

## Discovery rules

- `DialectRuntimeDescriptorRegistryBuilder` discovers aliases only from explicitly provided assemblies or types.
- Discovery is deterministic: candidate assemblies and types are ordered with `StringComparer.Ordinal`.
- Duplicate aliases in the same category fail fast and report the alias plus both conflicting metadata owner types.

## Adding a new component

1. Implement the runtime component or backend declaration type.
2. Add the appropriate alias attribute on that declaration site.
3. Ensure the containing assembly or type is included in the dialect runtime composition path.

No central alias dictionary should be edited for new modules, optimizers, or backends.
