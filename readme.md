# UniversalToolchain

UniversalToolchain is a modular .NET framework for embeddable DSLs, expression engines, and rule engines. Wist is the reference language in this repository and demonstrates the framework through a working CLI, dialect composition, and two execution backends.

It is designed for cases where you want:

- configurable formulas inside a .NET application,
- a small domain-specific language instead of hardcoded rules,
- a reusable toolchain pipeline with interpreter and compiler execution modes.

## Where it can be used

Typical scenarios:

- pricing and discount formulas,
- validation and routing rules,
- configurable business logic in internal tools,
- educational and research projects around compilers and DSLs.

## Why not just an expression evaluator?

UniversalToolchain is not just a string-to-number evaluator.
It is designed as a reusable language toolchain with modular composition, dialect configuration, and multiple execution
modes.

## Quick start in 30 seconds

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --mode compiler
```

Expected output:

```text
12
```

## Programmatic example

A fuller scenario is available in `UniversalToolchain/Example/Program.cs`.

```csharp
var compiler = host.GetArtifactCompiler<DynamicMethod>("compiler");
var declaredBindings = new OrderedDictionary<string, Type>
{
    ["price"] = typeof(double),
    ["fee"] = typeof(double)
};

var artifact = compiler.Compile("price * 0.9 + fee", declaredBindings);
var calculate = artifact.AsFunc<double, double, double>();
var compiledResult = calculate(100.0, 5.0); // 95.0

var interpreter = host.GetArtifactCompiler<IAbstractIR>("interpreter");
var interpretedArtifact = interpreter.Compile("price * 0.9 + fee", declaredBindings);
var session = interpretedArtifact.CreateSession();
session.SetArgument("price", 100.0);
session.SetArgument("fee", 5.0);
var interpretedResult = (double)session.Run().NotNull(); // 95.0
```

## Why this project exists

Many language projects repeatedly rebuild the same layers: parsing, AST/IR transforms, runtime composition, and
execution.
UniversalToolchain focuses on reusable composition so capabilities can be assembled from modules instead of hardcoded
into one implementation path.

This repository contains:

- **UniversalToolchain** — reusable framework infrastructure.
- **Wist** — a reference language used to validate and evolve the framework architecture.

## Compared to language platforms focused on their own runtime model

UniversalToolchain is focused on explicit backend-oriented execution inside the .NET ecosystem.
The design priority here is composable integration into .NET applications rather than a separate runtime universe.

## Architecture at a glance

At a high level:

```text
Source -> Lexer/Parser -> AST -> Bytecode/IR -> Optimization -> Compiler/Interpreter -> Execution
```

Key repository architecture concepts:

- framework-first, composition-based pipeline design,
- dual execution modes (`compiler`, `interpreter`),
- dialect-driven runtime composition via `.wistdialect`,
- CLI and programmatic entry points for validation and integration.

For detailed architecture context (execution model, dialect workflow, and repository entry points), see
`docs/architecture-overview.md`.

## How features plug into the pipeline

Framework features are introduced through extension points instead of one monolithic compiler path.

For example, a frontend module can participate in several stages:

- text preprocessing,
- lexer initialization,
- lexeme post-processing,
- parser initialization,
- AST post-processing,
- bytecode post-processing,
- AST-to-bytecode translator initialization.

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

## Dialect usage

```bash
# Run code with a dialect definition
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/full-default/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/full-default/program.wist --mode interpreter

# Inspect a dialect file
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- dialect-inspect --file UniversalToolchain/Dialects/examples/wist/full-default/dialect.wistdialect

# Run the dialect demo workflow
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- dialect-demo --file UniversalToolchain/Dialects/examples/wist/full-default/dialect.wistdialect
```

## Dialect examples

Located under `UniversalToolchain/Dialects/examples/wist`:

- `full-default`
- `full-default-native`
- `minimal-arithmetic`
- `restricted-sandbox` *(composition-constrained profile, not OS-level sandboxing)*

## Why .NET 10 right now?

The current validation baseline is .NET 10 (`net10.0`) with SDK `10.0.103`.
Active runtime and backend work is being verified there first, so older target frameworks are not the current compatibility target.

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
dotnet test UniversalToolchain/UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj -c Release --no-build
dotnet test UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj -c Release --no-build
```

## Security note

The repository does **not** claim hardened sandboxing for untrusted code. Use process/environment isolation for
untrusted execution scenarios.
See `SECURITY.md` for the trust model.

## Known limitations

This repository is actively evolving, and some areas are intentionally treated as design-in-progress:

- some bootstrap/runtime wiring is still concrete rather than fully descriptor-driven,
- reflection-based interop/discovery helpers still exist and are being cleaned up,
- constrained dialect composition is not equivalent to hardened sandboxing,
- `ParametersSetter` is not yet exported into dialect composition,
- coverage tests for `ParametersSetter` dialect export are pending,
- the reference language Wist is still the main proving ground for framework decisions.

## Canonical documentation map

- Project overview: `readme.md`
- Architecture overview: `docs/architecture-overview.md`
- Coding standards: `PROJECT_RULES.md`
- Contribution workflow: `CONTRIBUTING.md`
- Security policy: `SECURITY.md`

## License

Licensed under Apache License 2.0. See [LICENSE](LICENSE).
