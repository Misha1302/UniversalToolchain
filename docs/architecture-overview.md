# Architecture Overview

## Purpose

UniversalToolchain is a reusable framework for building modular language toolchains.
Wist is the reference language in this repository and serves as a proving ground for framework architecture decisions.

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

## Dialect workflow

Typical dialect execution flow:
1. Parse dialect source.
2. Build semantic plan.
3. Resolve runtime composition descriptors.
4. Create execution host.
5. Run code.

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
