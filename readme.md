# UniversalToolchain

UniversalToolchain is a modular .NET framework for embeddable DSLs, expression engines, and rule engines.
It is for .NET applications that need configurable language behavior instead of one fixed evaluator.

Wist is the reference language in this repository. It demonstrates the framework through a working CLI, dialect
composition, and compiler/interpreter execution modes.

## Why not just an expression evaluator?

UniversalToolchain is useful when expressions are only one part of the problem.
It gives you a language pipeline: syntax, dialect composition, validation, translation, optimization, and execution.

Use it when you need control over what the language can express and how it runs.

## Quick start in 30 seconds

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --mode compiler
```

Expected output:

```text
12
```

## Run the pricing demo

```bash
dotnet run --project UniversalToolchain/Example/Example.csproj
```

This runs a pricing formula through hardcoded C#, a general Wist dialect, and a restricted pricing dialect. It shows the
same calculation executed through compiler, interpreter, and fast native invocation paths, plus rejection of a formula
that the restricted dialect does not allow.

Code: [UniversalToolchain/Example/Scenarios/PricingDiscountScenario.cs](UniversalToolchain/Example/Scenarios/PricingDiscountScenario.cs)
and [UniversalToolchain/Example/Scenarios/DslPricingCalculator.cs](UniversalToolchain/Example/Scenarios/DslPricingCalculator.cs).

## Programmatic example

A small direct API example:

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

## When to use UniversalToolchain

Use UniversalToolchain when:

- a normal expression evaluator is too narrow for your rules or formulas,
- users need a syntax that matches your domain instead of C# syntax,
- you need a restricted dialect that allows only selected language features,
- the same language should support compiler and interpreter modes,
- you need an inspectable execution pipeline for validation, diagnostics, or backend work,
- you want configurable business logic without hardcoding every rule into the application.

Typical scenarios include pricing formulas, validation rules, routing rules, internal workflow rules, and DSL experiments
inside .NET applications.

## When not to use UniversalToolchain

Do not start here when:

- you only need trivial arithmetic formulas,
- you only need a subset of C# expressions,
- you only need parser generation,
- you do not need a DSL or runtime story,
- a simple library call can already evaluate all rules you plan to support.

For those cases, a smaller evaluator, a rules library, or a parser generator is usually easier to own.

## Comparison

- **NCalc** is strong for evaluating compact expressions. UniversalToolchain becomes relevant when expression evaluation
  is not enough and you need dialect control, execution modes, or a reusable pipeline.
- **RulesEngine** is strong for JSON-defined business rules in .NET applications. UniversalToolchain becomes relevant
  when the rule language itself needs custom syntax, restricted capabilities, or compiler/interpreter backends.
- **ANTLR/csly** are strong for building parsers. UniversalToolchain becomes relevant when parsing is only the first
  step and you also need runtime composition, IR/bytecode translation, optimization, and execution.

## Why this project exists

Many language projects repeatedly rebuild the same layers: parsing, AST/IR transforms, runtime composition, and
execution. UniversalToolchain focuses on reusable composition so capabilities can be assembled from modules instead of
hardcoded into one implementation path.

This repository contains:

- **UniversalToolchain**: reusable framework infrastructure.
- **Wist**: a reference language used to validate and evolve the framework architecture.

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
