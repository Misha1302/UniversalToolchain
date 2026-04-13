# Architecture Overview

## Purpose

UniversalToolchain is a modular .NET framework for embeddable DSLs, expression engines, and rule engines.
Wist is the reference language in this repository and demonstrates the framework through a working CLI, dialect composition, and two execution backends.

## Architecture split

The current architecture is organized into three layers:

1. **Core runtime pipeline**
    - Shared compilation/execution infrastructure (frontend, translation, optimization, and backend execution).
2. **Dialect subsystem**
    - `UniversalToolchain.Dialects.*` projects that parse, validate, and resolve dialect definitions.
3. **Integration layer**
    - `UniversalToolchain.Dialects.Wist`, which maps dialect composition results into executable Wist hosts.

## Execution model

Execution supports two modes:

- `compiler`
- `interpreter`

Runtime composition paths:

- **Default composition**: uses built-in/default service registrations.
- **Dialect-based composition**: uses a `.wistdialect` file to resolve runtime composition before execution.

Compile-artifact surface:

- Host-level artifact compilation is exposed through
  `WistDialectExecutionHost.GetArtifactCompiler<TCompilationOutput>(mode)`.
- Backends that support artifact-oriented execution return `IArtifactCompiler<TCompilationOutput>` and produce
  `ICompiledArtifact<TCompilationOutput>`.
- Backend-specific helpers/extensions (for example, `DynamicMethod` convenience APIs) are optional layers and should
  be treated as convenience/performance paths, not mandatory framework behavior.

## Dialect workflow

Typical dialect execution flow:

1. Parse dialect source.
2. Build semantic plan.
3. Resolve runtime composition descriptors.
4. Create execution host.
5. Run code.

Runtime component resolution builds cached per-assembly component indexes and uses a loadable-types fallback when
reflection encounters partial type-load failures.

## Dialect extensibility boundary

The `.wistdialect` syntax remains closed in the current dialect series. New custom semantics should plug into semantic
binding through directive handlers and builder extensions, not through grammar changes or a meta-DSL.

`DialectDefinition` remains the typed immutable v1 model. Its typed properties are the canonical access path for v1
policies such as modules, backends, intrinsics, optimizers, security, capabilities, and order rules. `Extensions` is a
controlled storage bag for future custom semantic results only; canonical v1 policies should not be moved into it.

## Repository entry points

- Solution: `UniversalToolchain/Wist.sln`
- CLI entry: `UniversalToolchain/Wistc/Program.cs`
- Programmatic example: `UniversalToolchain/Example/Program.cs`
- Dialect examples: `UniversalToolchain/Dialects/examples/wist/*`
- Core tests: `UniversalToolchain/Tests/Tests.csproj`
- Dialect tests: `UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj`

## Limitations and risks

- Composition constraints are useful but are **not** equivalent to hardened sandboxing.
- Running untrusted code remains high risk without process/environment isolation.
- Some architecture areas are still evolving and should be treated as active design surface.
