# UniversalToolchain & Wist

## 1. What this repository is
UniversalToolchain is a modular compiler/interpreter framework implemented in .NET.
Wist is the reference language used to validate and evolve that framework.
The repository uses composable modules for lexer, parser, translation, optimization, and execution stages.
Current solution projects target `net10.0` (.NET 10). The repository pins .NET SDK `10.0.103` with `rollForward: latestMajor` and `allowPrerelease: true`.

## 2. Repository scope
The current repository includes:
- the core UniversalToolchain compilation/execution pipeline
- the Wist CLI (`Wistc`) with file execution, eval, and REPL
- the dialect definition DSL subsystem and composition pipeline
- dialect examples under `UniversalToolchain/Dialects/examples/wist`
- core tests and dedicated dialect tests
- benchmark projects (including `NCalcWist.Benchmarks`)
- `ConfigurationEditor` (React/TypeScript frontend for config editing)

## 3. Main capabilities
- Modular language feature composition through frontend and IR modules.
- Dual execution modes: `compiler` and `interpreter`.
- Dialect-based runtime composition path (`.wistdialect` -> composition -> execution host).
- Runnable examples and automated tests for core and dialect paths.
- Programmatic dialect execution via `WistDialectExecutionWorkflow`.

## 4. Requirements
- Requires .NET SDK `10.0.103`.
- SDK policy in this repository: `rollForward: latestMajor`, `allowPrerelease: true`.
- Current build/test/runtime projects target `net10.0` (.NET 10).

## 5. Build and test
From repository root:

```bash
dotnet restore UniversalToolchain/Wist.sln
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore
dotnet test UniversalToolchain/Tests/Tests.csproj -c Release --no-build
dotnet test UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj -c Release --no-build
```

## 6. CLI usage
The `Wistc` entrypoint exposes these verbs:
- `run`
- `repl`
- `dialect-inspect`
- `dialect-demo`

Key options used by runtime execution:
- `--dialect-file <path>` to compose and run with a dialect definition
- `--mode <compiler|interpreter>` to select backend

## 7. Default runtime examples
Run a file:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --file UniversalToolchain/Dialects/examples/wist/full-default/program.wist --mode interpreter
```

Run a one-liner:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --mode compiler
```

Start REPL:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- repl --mode compiler
```

## 8. Dialect runtime examples
Run code with a dialect file:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/full-default/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/full-default/program.wist --mode interpreter
```

Inspect a dialect file:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- dialect-inspect --file UniversalToolchain/Dialects/examples/wist/full-default/dialect.wistdialect
```

Run dialect framework demo using a real dialect file:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- dialect-demo --file UniversalToolchain/Dialects/examples/wist/full-default/dialect.wistdialect
```

## 9. Programmatic usage
`UniversalToolchain/Example/Program.cs` shows programmatic composition/execution:

```csharp
using UniversalToolchain.Dialects.Wist;

var services = new ServiceCollection();
services.AddWistDialectServices();

using var provider = services.BuildServiceProvider();
var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

var dialect = workflow.ComposeFile("./Dialects/examples/wist/full-default/dialect.wistdialect");
if (!dialect.IsSuccess)
{
    Console.WriteLine(dialect.ToDeterministicText());
    return;
}

using var host = workflow.CreateHost(dialect);
var result = host.Run("(2 + 2) * 3", "compiler");
Console.WriteLine(result);
```

## 10. Repository examples
- `full-default`: practical default-style runtime composition with both `cil` and `interpreter` backends plus local-variable optimization.
- `minimal-arithmetic`: smallest useful arithmetic-focused composition (`Arithmetic`, `Numbers`, `Scopes`, `Whitespaces`) on interpreter backend.
- `restricted-sandbox`: constrained composition example focused on arithmetic-only execution profile.

## 11. Architecture at a glance
- Modular pipeline stages: lexing/parsing, AST translation, IR processing, backend execution.
- Same language frontend can run in compiler or interpreter mode.
- Dialect composition path resolves declarative runtime descriptors into executable Wist hosts.

## 12. Current limitations
- The repository includes constrained runtime compositions, but does **not** claim fully hardened sandbox guarantees for untrusted code.
- Security boundaries for untrusted execution remain an area that must be treated cautiously.
- Parts of architecture and composition ergonomics are still evolving.
- Dialect examples can constrain available capabilities, but constrained composition is not equivalent to complete process-level isolation.

## 13. License
Licensed under Apache License 2.0. See `LICENSE`.

## 14. Project rules
Contributor coding/documentation rules: [PROJECT_RULES.md](PROJECT_RULES.md).
