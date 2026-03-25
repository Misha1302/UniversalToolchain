# Project Overview

## 1. Purpose
This repository develops UniversalToolchain as a modular language toolchain and uses Wist as its primary implementation target. The purpose is to evolve reusable compilation/execution infrastructure while validating it on a real language runtime.

Current projects in this repository target `net10.0` (.NET 10), and the repository pins .NET SDK `10.0.103` with `rollForward: latestMajor` and `allowPrerelease: true`.

## 2. What UniversalToolchain is
UniversalToolchain is a composition-oriented framework where language behavior is assembled from modules. Core stages include lexing, parsing, AST translation, IR processing, and backend execution. Composition is dependency-injection based and supports both default runtime assembly and dialect-driven assembly.

## 3. What Wist is
Wist is the reference language hosted on UniversalToolchain. It is both:
- a concrete end-user CLI/runtime (`Wistc` run + REPL), and
- a proving ground for architectural patterns (module composition, dual backend execution, dialect composition).

## 4. Current architecture
Current architecture is split into:
- **Core runtime pipeline** used by default Wist execution.
- **Dialect subsystem** (`UniversalToolchain.Dialects.*`) that parses/validates dialect definitions and resolves them into runtime composition descriptors.
- **Integration layer** (`UniversalToolchain.Dialects.Wist`) that maps dialect composition to executable Wist hosts.

The dialect path is not hypothetical; it is exercised by CLI verbs, examples, and dedicated tests.

## 5. Execution model
Execution supports two modes selected via CLI option `--mode`:
- `compiler`
- `interpreter`

`Wistc run` can execute source from a file, direct argument, or eval mode. `Wistc repl` provides interactive execution. Without `--dialect-file`, runtime composition follows default service registration; with `--dialect-file`, execution uses dialect-composed runtime.

## 6. Dialect subsystem
Dialect workflow currently includes:
1. Parse dialect source.
2. Build semantic plan.
3. Resolve runtime composition descriptors.
4. Create an execution host and run code.

The repository includes three dialect examples (`full-default`, `minimal-arithmetic`, `restricted-sandbox`) and smoke tests that compose and execute each example end-to-end.

## 7. Current strengths
- Modular composition across multiple pipeline stages.
- Compiler/interpreter duality with shared language frontend concepts.
- Working dialect DSL subsystem with concrete execution path.
- CLI + programmatic entry points (`WistDialectExecutionWorkflow`).
- Dedicated test projects for core pipeline and dialect subsystem.

## 8. Current limitations and risks
- Composition constraints in examples are useful but are not equivalent to hardened sandboxing.
- Runtime execution facilities can run scripts; untrusted input must be treated as high risk.
- Architectural debt remains (deterministic composition, abstraction cleanup, long-term security hardening).
- Some internal/editor UX text and tooling are still evolving and not fully polished as external products.

## 9. Repository entry points
- Solution: `UniversalToolchain/Wist.sln`
- CLI entry: `UniversalToolchain/Wistc/Program.cs`
- Programmatic example: `UniversalToolchain/Example/Program.cs`
- Dialect examples: `UniversalToolchain/Dialects/examples/wist/*`
- Core tests: `UniversalToolchain/Tests/Tests.csproj`
- Dialect tests: `UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj`
- Configuration editor frontend: `ConfigurationEditor/`
