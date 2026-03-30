# UniversalToolchain

UniversalToolchain is a modular .NET framework for building extensible language toolchains.

This repository contains:
- **UniversalToolchain** — reusable framework infrastructure.
- **Wist** — a reference language used to validate and evolve framework architecture.

Wist is intentionally a proving ground, not a limitation of the framework’s scope.

## Why this project exists

Many language projects repeatedly rebuild the same layers: parsing, AST/IR transforms, runtime composition, and execution.
UniversalToolchain focuses on reusable composition so capabilities can be assembled from modules instead of hardcoded into one implementation path.

## Scope and architecture

At a high level:

```text
Source -> Lexer/Parser -> AST -> Bytecode/IR -> Optimization -> Compiler/Interpreter -> Execution
```

Key repository architecture concepts:
- framework-first, composition-based pipeline design,
- dual execution modes (`compiler`, `interpreter`),
- dialect-driven runtime composition via `.wistdialect`,
- CLI and programmatic entry points for validation and integration.

## Architectural priorities

The repository is maintained with these priorities:
- universality first,
- no hardcoded behavior where composition/abstraction is viable,
- low coupling to concrete implementations, dialects, and modules,
- DRY, KISS, SOLID, and OOP-oriented design,
- explicit avoidance of technical debt and legacy-preserving shortcuts.

## Requirements

- .NET SDK `10.0.103`
- SDK policy in `UniversalToolchain/global.json`:
  - `rollForward: latestMajor`
  - `allowPrerelease: true`
- Targets: `net10.0`

## Quick start

From repository root:

```bash
dotnet restore UniversalToolchain/Wist.sln
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore
dotnet test UniversalToolchain/Tests/Tests.csproj -c Release --no-build
dotnet test UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj -c Release --no-build
```

## Quick demo

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --mode compiler
```

Expected output:

```text
12
```

## CLI usage (`Wistc`)

Available verbs:
- `run`
- `repl`
- `dialect-inspect`
- `dialect-demo`

Common options:
- `--mode <compiler|interpreter>`
- `--dialect-file <path>`

Examples:

```bash
# Run a .wist file
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --file UniversalToolchain/Dialects/examples/wist/full-default/program.wist --mode interpreter

# Evaluate one expression
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --mode compiler

# Start REPL
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- repl --mode compiler
```

## Programmatic usage

```csharp
using UniversalToolchain.Dialects.Wist;

var services = new ServiceCollection();
services.AddWistDialectServices();

using var provider = services.BuildServiceProvider();
var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

var dialect = workflow.ComposeFile("./Dialects/examples/wist/full-default/dialect.wistdialect");
if (!dialect.IsSuccess) return;

using var host = workflow.CreateHost(dialect);
var result = host.Run("(2 + 2) * 3", "compiler");
Console.WriteLine(result);
```

See `UniversalToolchain/Example/Program.cs` for a full composition + diagnostics flow.

## Dialect examples

Located under `UniversalToolchain/Dialects/examples/wist`:
- `full-default`
- `full-default-native`
- `minimal-arithmetic`
- `restricted-sandbox` *(composition-constrained profile, not OS-level sandboxing)*

## Security note

The repository does **not** claim hardened sandboxing for untrusted code. Use process/environment isolation for untrusted execution scenarios.
See `SECURITY.md` for the trust model.

## Canonical documentation map

- Project overview: `readme.md`
- Coding standards: `PROJECT_RULES.md`
- Contribution workflow: `CONTRIBUTING.md`
- AI agent instructions: `AGENTS.md`
- Security policy: `SECURITY.md`
- Release notes: `CHANGELOG.md`

## License

Licensed under Apache License 2.0. See [LICENSE](LICENSE).
