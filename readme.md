# UniversalToolchain

UniversalToolchain is a modular .NET framework for building extensible domain-specific languages.

This repository contains the **UniversalToolchain framework** and **Wist**, its reference language.
Wist is used as a proving ground to validate and evolve the framework’s composable pipeline, grammar-driven frontend,
and execution backends.

## Why this project exists

Many DSL projects start as one-off implementations and end up rebuilding the same infrastructure: lexer/parser, AST/IR
transforms, runtime wiring, and execution.

UniversalToolchain focuses on reusing that infrastructure through composable stages on .NET, so language features can be
assembled and evolved without rewriting the entire toolchain each time.

## What it supports

- Modular language feature composition across frontend and runtime stages.
- Dual execution modes: `compiler` and `interpreter`.
- Dialect-based runtime composition via `.wistdialect` files.
- CLI and programmatic usage.
- Tests, runnable examples, and benchmark projects in-repo.

## Quick demo

From repository root:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --mode compiler
```

Expected output:

```text
12
```

## Architecture at a glance

```text
Source -> Lexer/Parser -> AST -> Bytecode/IR -> Optimization -> Compiler/Interpreter -> Execution
```

With dialects, a composition step resolves a `.wistdialect` definition into an executable Wist runtime host.

## Why this approach?

This project is not only about parsing expressions. It is about **reusable language infrastructure**:

- compose language capabilities from modules instead of hardcoding one pipeline,
- keep one frontend model while switching execution backend (`compiler` or `interpreter`),
- define runtime shape declaratively with dialect files.

## Requirements

- .NET SDK `10.0.103`
- SDK policy in `UniversalToolchain/global.json`:
    - `rollForward: latestMajor`
    - `allowPrerelease: true`
- Projects target `net10.0`

## Quick start: build and test

From repository root:

```bash
dotnet restore UniversalToolchain/Wist.sln
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore
dotnet test UniversalToolchain/Tests/Tests.csproj -c Release --no-build
dotnet test UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj -c Release --no-build
```

## CLI usage (`Wistc`)

Available verbs:

- `run`
- `repl`
- `dialect-inspect`
- `dialect-demo`

Common runtime options:

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

## Dialect usage

```bash
# Run code with a dialect definition
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/full-default/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/full-default/program.wist --mode interpreter

# Inspect a dialect file
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- dialect-inspect --file UniversalToolchain/Dialects/examples/wist/full-default/dialect.wistdialect

# Run the framework-native dialect demo workflow
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- dialect-demo --file UniversalToolchain/Dialects/examples/wist/full-default/dialect.wistdialect
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

See `UniversalToolchain/Example/Program.cs` for the full example with composition diagnostics handling.

## Repository examples

Dialect examples are under `UniversalToolchain/Dialects/examples/wist`:

- `full-default`
- `full-default-native`
- `minimal-arithmetic`
- `restricted-sandbox`

Profile roles:

- `full-default` — richest universal profile
- `full-default-native` — richest native arithmetic profile
- `minimal-arithmetic` — smallest runnable arithmetic profile
- `restricted-sandbox` — restricted interpreter-only expression profile, not OS-level sandbox

## Current limitations

- This repository does **not** claim hardened sandboxing for untrusted code.
- Constrained dialect composition is **not** equivalent to OS/process-level isolation.
- Security boundaries for untrusted execution must be treated cautiously.
- Parts of architecture and composition ergonomics are still evolving.

## License

Licensed under Apache License 2.0. See [LICENSE](LICENSE).

## Project rules and contribution docs

- [PROJECT_RULES.md](PROJECT_RULES.md)
- [CONTRIBUTING.md](CONTRIBUTING.md)
- [project info.md](project%20info.md)
