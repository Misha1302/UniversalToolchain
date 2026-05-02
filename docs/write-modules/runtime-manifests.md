---
title: Runtime Manifests
description: Explain how frontend modules become visible to dialect runtime composition.
---

# Runtime Manifests

Runtime manifests are how dialect composition discovers selectable runtime components such as frontend modules, optimizers and backends.

For normal module authoring, do not create these JSON files by hand. They are generated during build from attributes on compiled types.

## Why manifests exist

A dialect file names modules by alias:

```text
dialect Demo
use Whitespaces,Numbers,Scopes,Arithmetic
backend interpreter
```

Runtime composition then needs to resolve aliases such as `Arithmetic` to concrete assemblies and activation types.

That lookup should not depend on hardcoded module lists in the dialect compiler. The manifest is the build-time metadata bridge between declarative dialect files and concrete runtime components.

## The current authoring rule

For a frontend module, provide metadata in code:

```csharp
[DialectModuleAlias("MyFeature")]
[DialectRuntimeExport("FrontendModule", "MyFeature")]
[AutoRegisterService]
public class MyFeatureModuleImpl : IFrontendCoreModule
{
    // lexer, parser and translator registrations
}
```

Then make sure the containing project emits runtime manifests:

```xml
<PropertyGroup>
    <EmitDialectRuntimeManifest>true</EmitDialectRuntimeManifest>
</PropertyGroup>
```

During build, the manifest emitter writes a generated file named after the assembly:

```text
MyFeatureModule.dialect.runtime.json
```

The file is produced in the build output directory. It is a generated artifact, not a normal source file.

## Existing project vs new project

If you add a module to an existing module project that already has `EmitDialectRuntimeManifest` enabled, no project-file change is needed.

For example, adding `TextualAddition` inside `ArithmeticModule` does not require a new JSON file because `ArithmeticModule` already emits a runtime manifest.

If you create a new standalone module project, add `EmitDialectRuntimeManifest` to that project. Without it, the assembly may compile, but manifest-backed runtime composition will not discover the new alias.

## What not to do

Do not:

- commit a hand-written `.dialect.runtime.json` for a normal module;
- maintain duplicate alias lists in JSON and C# attributes;
- patch dialect composition with a hardcoded special case for the new module;
- rely on a module being referenced by the solution alone as proof that dialect runtime selection can see it.

The source of truth should be the module/export attributes plus generated manifest output.

## What to test

A module is visible only if composition can resolve it by alias. Add tests that prove both sides:

- a dialect selecting the module can execute its syntax;
- a dialect omitting the module rejects the same syntax;
- compiler and interpreter agree when both backends are selected.

For the first-module tutorial, this is covered by `TextualAdditionModuleTests`.

## Troubleshooting

If a dialect says a module alias cannot be resolved, check these in order:

1. The module type has `[DialectModuleAlias("...")]`.
2. The module type has `[DialectRuntimeExport("FrontendModule", "...")]`.
3. The project containing the module has `<EmitDialectRuntimeManifest>true</EmitDialectRuntimeManifest>`.
4. The project is built before the host tries to compose the dialect.
5. The generated `.dialect.runtime.json` file appears in the output directory.
6. Referenced manifests are copied into the consuming output directory.

If the manifest exists but activation fails, inspect the activation type name and service registration rather than editing the generated JSON.

## Relation to the tutorial

[Create Your First Module](/write-modules/create-your-first-module) adds `TextualAddition` inside `ArithmeticModule`, so no hand-written runtime JSON is needed. The existing module project already participates in manifest generation.

A future tutorial for a separate module assembly should include the `.csproj` manifest flag explicitly as part of the setup.
