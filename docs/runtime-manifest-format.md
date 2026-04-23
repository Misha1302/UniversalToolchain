# Runtime Manifest Format

Runtime manifests declare runtime components that can participate in dialect composition.

## Manifest role

A manifest file provides deterministic runtime component metadata for one assembly. The catalog loads manifests, validates entries, and exposes alias-based lookup for modules, optimizers, and backends.

## Component entry model (conceptual)

Each component entry declares:

- `kind` (`FrontendModule`, `Optimizer`, `Backend`),
- `canonicalAlias` and optional aliases,
- stable `componentId`,
- owner `assemblySimpleName`,
- optional `activation` metadata.

## Structured type references

Activation metadata uses structured type references:

- `assemblySimpleName`
- `typeFullName`

This applies to:

- `activationType` (runtime component type),
- `registrarType` (backend registrar type, backend entries only).

Structured references make activation deterministic and unambiguous across assemblies.

## Canonical emission and compatibility fallback

Canonical manifest emission writes structured activation references for both activation and registrar types.
The runtime serializer still accepts older manifest forms (legacy type fields) as compatibility input and normalizes them into the runtime activation model.

## What runtime infrastructure uses it for

- catalog loading and deterministic alias maps,
- selected-component resolution from the dialect build plan,
- exact runtime component activation,
- exact backend registrar resolution for selected backends.
