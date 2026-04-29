# UniversalToolchain

Build formulas, rules, and mini-languages for .NET applications.

UniversalToolchain is an embeddable .NET DSL/runtime framework for the moment when a plain expression evaluator is no longer enough.

Wist is the reference language in this repository. It demonstrates the framework through shipped dialect profiles, a
pricing demo, manifest-backed dialect composition, and compiler/interpreter execution modes.

> Rules are temporarily removed from the public runtime surface. `rule-schema`/`rule-run` and raw-source RuleSet MVP parsing were removed and will return only after an AST-owned rule declaration rewrite.
> `RuleDeclarationsModule` is also removed from runtime-visible modules; do not reintroduce marker-only rule capabilities before parser-owned implementation exists.

## Run the pricing demo

```bash
dotnet run --project UniversalToolchain/Example/Example.csproj
```

This runs a pricing formula through hardcoded C#, the shipped `full-default-native` Wist preset, and the shipped
`pricing-restricted` dialect. It shows the same calculation executed through compiler, interpreter, and fast native
invocation paths, plus rejection of a formula that the restricted dialect composition does not allow.

Code: [UniversalToolchain/Example/Scenarios/PricingDiscountScenario.cs](UniversalToolchain/Example/Scenarios/PricingDiscountScenario.cs) and [UniversalToolchain/Example/Scenarios/DslPricingCalculator.cs](UniversalToolchain/Example/Scenarios/DslPricingCalculator.cs).

## What this demo shows

The pricing demo compares three ways to own the same business rule:

- hardcoded C# pricing logic,
- the shipped `full-default-native` Wist preset for the formula,
- a restricted pricing dialect with a narrower runtime surface,
- compiler, interpreter, and fast native invocation paths for the same calculation,
- rejection of a formula shape that the restricted dialect does not allow.

## Tiny CLI quick start

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --mode compiler
```

Expected output:

```text
12
```

## Programmatic example

A small Wist facade example:

```csharp
using var wist = WistRuntimeFacadeBuilder
    .CreateDefault()
    .Build();

var result = wist.Run(
    "price * 0.9 + fee",
    new Dictionary<string, object?>
    {
        ["price"] = 100.0,
        ["fee"] = 5.0
    },
    mode: "compiler");

var compiledResult = (double)result!; // 95.0
```

Use an explicit shipped dialect preset when the runtime surface must be composition-constrained:

```csharp
using UniversalToolchain.Dialects.Wist.Presets;

using var wist = WistRuntimeFacadeBuilder
    .CreateDefault()
    .WithShippedDialectPreset(WistShippedDialectPresets.PricingRestricted)
    .Build();

var attempt = wist.TryCompile(
    "let discount = 0.9\n price * discount + fee",
    new Dictionary<string, Type>
    {
        ["price"] = typeof(double),
        ["fee"] = typeof(double)
    },
    mode: "interpreter");
```

## When to use UniversalToolchain

Use UniversalToolchain when:

- a normal expression evaluator is too narrow for your rules or formulas,
- users need a syntax that matches your domain instead of C# syntax,
- you need a dialect profile that allows only selected language features,
- the same language should support compiler and interpreter modes,
- you need an inspectable execution pipeline for validation, diagnostics, or backend work,
- you want configurable business logic without hardcoding every rule into the application.

Typical scenarios include pricing formulas, routing rules, internal workflow rules, and DSL experiments inside .NET applications.

## When not to use UniversalToolchain

Do not start here when:

- you only need trivial arithmetic formulas,
- you only need a subset of C# expressions,
- you only need parser generation,
- you do not need a DSL or runtime story,
- a simple library call can already evaluate all rules you plan to support.

For those cases, a smaller evaluator, a rules library, or a parser generator is usually easier to own.

## Comparison

For a practical positioning guide against the nearest alternatives, see [UniversalToolchain vs Nearby Alternatives](docs/alternatives.md).

At a high level:

- **NCalc / Dynamic Expresso** are strong when you only need expression evaluation.
- **RulesEngine** is strong when you need JSON-defined business rules in a .NET application.
- **ANTLR / csly / Irony** are strong when parser construction is the main problem.
- **JetBrains MPS** is strong when the target is a full language workbench with richer tooling.
- **UniversalToolchain** becomes relevant when you need a restricted, embeddable DSL/runtime stack for .NET rather than only a parser, evaluator, or workbench.

## Why this project exists

Many language projects repeatedly rebuild the same layers: parsing, AST/IR transforms, runtime composition, and execution. UniversalToolchain focuses on reusable composition so capabilities can be assembled from modules instead of hardcoded into one implementation path. See [Why this exists](docs/why-this-exists.md) for the longer product and architecture rationale.

This repository contains:

- **UniversalToolchain**: reusable framework infrastructure.
- **Wist**: a reference language used to validate and evolve the framework architecture.

For a stricter boundary between framework, reference language, and current limitations, see [Project positioning](docs/project-positioning.md).

## Architecture at a glance

At a high level:

```text
Source -> Lexer/Parser -> AST -> Bytecode/IR -> Optimization -> Compiler/Interpreter -> Execution
```

Key repository architecture concepts:

- framework-first, composition-based pipeline design,
- dual execution modes (`compiler`, `interpreter`),
- dialect-driven runtime composition via `.wistdialect` files,
- manifest-backed runtime selection before host creation,
- bytecode/AIR as semantic pipeline layers,
- CLI and programmatic entry points for validation and integration.

## Canonical Wist runtime path

Normal Wist dialect execution follows this path:

```text
dialect source -> dialect compilation -> build plan -> manifest-backed runtime selection -> host creation -> execution
```

Composition and host creation are separate stages. `ComposeText`/`ComposeFile` compile the dialect DSL, build a
deterministic plan, and resolve selected modules, optimizers, and backends from runtime manifests. `CreateHost` then
builds the runtime provider for that resolved selection and activates only the selected backend registrars.

The selection-driven path is the main dialect execution story. Runtime activation in that path uses targeted, exact loading
of selected runtime component and registrar types from manifests. Shipped profiles do not require explicit backend imports in
normal CLI, facade, or example usage. Broad eager discovery and compatibility helpers still exist, but they are not the
canonical path for running shipped dialect profiles.

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

Module authoring is convention-heavy. Before adding or changing modules, read [Module authoring guide](docs/guides/module-authoring.md) and [Module contracts](docs/contracts/module-contracts.md).

## CLI usage (`Wistc`)

Available verbs:

- `run`
- `repl`
- `dialect-inspect`
- `dialect-demo`
- `features`

Common options:

- `--mode <compiler|interpreter>`
- `--dialect-file <path>`

The user-facing `compiler` mode selects the canonical `cil` backend when a dialect declares `backend cil` or the
`compiler` alias.

Examples:

```bash ci-timeout=240
# Run a .wist file with an explicit dialect definition
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/full-default/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/full-default/program.wist --mode interpreter

# Evaluate one expression
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --mode compiler
```

```bash ci-run=false
# Start REPL
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- repl --mode compiler
```

## Dialect usage

```bash ci-timeout=240
# Run code with a dialect definition
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/full-default/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/full-default/program.wist --mode interpreter

# Inspect a dialect file
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- dialect-inspect --file UniversalToolchain/Dialects/examples/wist/full-default/dialect.wistdialect

# Run the dialect demo workflow
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- dialect-demo --file UniversalToolchain/Dialects/examples/wist/full-default/dialect.wistdialect
```

## Dialect examples

Located under `UniversalToolchain/Dialects/examples/wist`:

- `full-default`: standard Wist profile over `cil` and `interpreter`.
- `full-default-native`: native arithmetic/type profile over `cil` and `interpreter`.
- `function-calls-safe-math`: neutral FunctionCalls + SafeMath profile without rule declarations.
- `minimal-arithmetic`: smallest interpreter arithmetic profile.
- `minimal-arithmetic-native`: smallest native arithmetic profile over `cil`.
- `pricing-restricted`: composition-constrained pricing profile with a restricted runtime surface.
- `restricted-sandbox`: composition-constrained profile, not a hardened sandbox guarantee.

These directories are runnable canonical references for the manifest-driven dialect path. Each README includes
repository-root CLI commands, expected behavior, and the capabilities intentionally excluded by that profile.

## Why .NET 10 right now?

The current validation baseline is .NET 10 (`net10.0`) with SDK `10.0.103`.
Active runtime and backend work is being verified there first, so older target frameworks are not the current compatibility target.

## Requirements

- .NET SDK `10.0.103`
- SDK policy in `UniversalToolchain/global.json`:
  - `rollForward: latestMajor`
  - `allowPrerelease: true`
- Targets: `net10.0`

## Build and test from source

From repository root:

```bash ci-run=false
dotnet restore UniversalToolchain/Wist.sln
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore
dotnet test UniversalToolchain/Tests/Tests.csproj -c Release --no-build
dotnet test UniversalToolchain/UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj -c Release --no-build
dotnet test UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj -c Release --no-build
```

## Security note

The repository does **not** claim hardened sandboxing for untrusted code. Use process/environment isolation for
untrusted execution scenarios.
See [docs/SECURITY.md](docs/SECURITY.md) for the trust model.

## Known limitations

This repository is actively evolving, and some areas are intentionally treated as design-in-progress:

- some bootstrap/runtime wiring is still concrete rather than fully descriptor-driven,
- reflection-based interop/discovery helpers still exist for compatibility and bootstrap scenarios,
- constrained dialect composition is not equivalent to hardened sandboxing,
- bytecode tags and module authoring contracts are being formalized,
- backend-agnostic artifact handling is still an active architecture area,
- the reference language Wist is still the main proving ground for framework decisions.

See [Current limitations](docs/limitations.md) for the explicit limitation and wording guide.

## Canonical documentation map

- Project overview: [readme.md](readme.md)
- Project positioning and public wording: [docs/project-positioning.md](docs/project-positioning.md)
- Current limitations: [docs/limitations.md](docs/limitations.md)
- Current runtime pipeline: [docs/current-canonical-runtime-pipeline.md](docs/current-canonical-runtime-pipeline.md)
- Runtime manifest activation model: [docs/runtime-manifest-activation-model.md](docs/runtime-manifest-activation-model.md)
- Runtime manifest format: [docs/runtime-manifest-format.md](docs/runtime-manifest-format.md)
- Bytecode and AIR architecture: [docs/architecture/bytecode-and-air.md](docs/architecture/bytecode-and-air.md)
- Backend and semantic parity contracts: [docs/architecture/backends-and-parity.md](docs/architecture/backends-and-parity.md)
- Module authoring guide: [docs/guides/module-authoring.md](docs/guides/module-authoring.md)
- Module hidden contracts: [docs/contracts/module-contracts.md](docs/contracts/module-contracts.md)
- Architecture guardrails: [docs/ARCHITECTURE_RULES.md](docs/ARCHITECTURE_RULES.md)
- Why this exists: [docs/why-this-exists.md](docs/why-this-exists.md)
- Nearby alternatives: [docs/alternatives.md](docs/alternatives.md)
- Coding standards: [docs/PROJECT_RULES.md](docs/PROJECT_RULES.md)
- Contribution workflow: [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md)
- Security policy: [docs/SECURITY.md](docs/SECURITY.md)

## License

Licensed under Apache License 2.0. See [LICENSE](LICENSE).
