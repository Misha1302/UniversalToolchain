# UniversalToolchain & Wist

**UniversalToolchain is a modular .NET meta-system for building extensible DSLs with context-free-grammar-based frontends and composable compilation stages.**

This repository contains two closely related parts:
- **UniversalToolchain** — the framework (pipeline, modules, runtime composition, execution backends).
- **Wist** — the reference language used to validate, stress, and evolve that framework.

If you want to understand how to assemble a language pipeline instead of rebuilding lexer/parser/IR/runtime infrastructure from scratch, this is the project’s main proving ground.

## Why this project exists

Building a DSL usually means re-implementing the same core machinery: tokenization, parsing, AST/IR transforms, runtime wiring, and execution.

UniversalToolchain addresses this with a **composition-oriented pipeline on .NET** where language features are modular and execution can target different backends (compiler or interpreter) without redesigning the whole stack.

## What it supports

- **Modular language feature composition** across frontend and runtime stages.
- **Dual execution modes**: `compiler` and `interpreter`.
- **Dialect-based composition** via `.wistdialect` files.
- **CLI and programmatic usage** for running code and composing dialect runtimes.
- **Tests, examples, and benchmark projects** in-repo.

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
Source code
  -> Lexer / Parser
  -> AST
  -> Bytecode / IR
  -> Optimization
  -> Backend (Compiler or Interpreter)
  -> Execution
```

With dialects, an additional composition flow resolves declarative runtime descriptors (`.wistdialect`) into an executable Wist host.

## Requirements

- .NET SDK **10.0.103**
- Repository SDK policy (`UniversalToolchain/global.json`):
  - `rollForward: latestMajor`
  - `allowPrerelease: true`
- Projects target `net10.0`.

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

Run a `.wist` file:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --file UniversalToolchain/Dialects/examples/wist/full-default/program.wist --mode interpreter
```

Evaluate one expression:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --mode compiler
```

Start REPL:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- repl --mode compiler
```

## Dialect usage

Run code with a dialect definition:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/full-default/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/full-default/program.wist --mode interpreter
```

Inspect a dialect file:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- dialect-inspect --file UniversalToolchain/Dialects/examples/wist/full-default/dialect.wistdialect
```

Run the framework-native dialect demo workflow:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- dialect-demo --file UniversalToolchain/Dialects/examples/wist/full-default/dialect.wistdialect
```

## Programmatic usage

See `UniversalToolchain/Example/Program.cs` for end-to-end service registration, dialect composition, host creation, and execution.

## Repository examples

Dialect examples live under `UniversalToolchain/Dialects/examples/wist`:

- `full-default` — default-style composition with `cil` and `interpreter` backends and local-variable optimization.
- `minimal-arithmetic` — minimal arithmetic-oriented composition.
- `restricted-sandbox` — constrained capability profile for demonstration purposes.

## Current limitations

- This repository does **not** claim hardened sandboxing for untrusted code execution.
- Constrained dialect composition is **not** equivalent to OS/process-level isolation.
- Security boundaries for untrusted execution should be treated cautiously.
- Some architecture/composition ergonomics are still evolving.

## License

Licensed under Apache License 2.0. See [LICENSE](LICENSE).

## Project rules and contribution docs

- Project rules: [PROJECT_RULES.md](PROJECT_RULES.md)
- Contribution guide: [CONTRIBUTING.md](CONTRIBUTING.md)
- Additional project context: [project info.md](project%20info.md)
