# UniversalToolchain

Build formulas, embeddable DSLs, and restricted mini-languages for .NET applications.

UniversalToolchain is an embeddable .NET DSL/runtime framework for the moment when a plain expression evaluator is no
longer enough.

Wist is the reference language in this repository. `UniversalToolchain.Wist` is the intended first-contact facade for
.NET developers who want formula execution without starting from the lower-level runtime contracts.

**Compiler-first. Interpreter-supported.**

Wist can compile selected formula code into typed CIL-backed execution paths. The performance-oriented path is
compiled typed invocation, not full convenience evaluation.

## Current release scope

This repository state is positioned as a scoped public preview of the Wist facade and runtime foundations, not as a
finalized 1.0 platform. The releaseable claim is formula evaluation, validation, restricted/full shipped Wist presets,
CLI smoke coverage, package smoke coverage, and typed compiled invocation for supported shapes.

Neutral runtime host extraction, structured JSON trace, FunctionCalls/SafeMath, and the SSA route are active foundations
with documented preview limits. They are not advertised here as a stable standalone runtime package family, a full trace
viewer, a complete function authoring system, or a production SSA optimizer/backend layer.

<!-- langdev-2026:start -->

> **Featured technical story — LangDev 2026 proposal**
>
> **Build the Language, Then Make the Abstractions Disappear: Extensible Programming on .NET**
>
> UniversalToolchain composes language features as independent modules,
> progressively lowers selected semantics through Bytecode and AIR into
> concrete runtime or typed CIL operations, and checks that interpreter
> and compiled execution preserve one language.
>
> [Read the proposal and run the reproducible demo](docs/talks/langdev-2026/README.md)

### Why this is interesting

- Language features remain modular while the language is being constructed.
- For supported compiled paths, per-operation module dispatch is removed from the prepared hot invocation path.
- Typed CIL is handed to the .NET JIT for further optimization.
- Cross-backend parity tests guard against one DSL silently becoming two languages.
- Current benchmarks are split into hot prepared execution, convenience `Evaluate`, and cold compilation stories; publish numbers only with raw BenchmarkDotNet artifacts and environment metadata.

**Conference evidence:** [one-command demo](docs/talks/langdev-2026/README.md#reproducible-command) · [module-to-CIL lowering](docs/talks/langdev-2026/lowering-walkthrough.md) · [semantic-parity regression](docs/talks/langdev-2026/parity-regression.md) · [benchmarks and limitations](docs/talks/langdev-2026/benchmark-evidence.md)

<!-- langdev-2026:end -->

## Requirements

- .NET SDK `10.0.103` or a compatible prerelease SDK selected by `UniversalToolchain/global.json`.
- Target framework: `net10.0`.
- SDK policy: `rollForward: latestMajor`, `allowPrerelease: true`.

## Install from NuGet

The package metadata in this repository is prepared for `UniversalToolchain.Wist` `0.1.0-preview.2`. The package-first
command works when that version is available from NuGet.org or another configured package source:

```bash ci-run=false
dotnet add package UniversalToolchain.Wist --version 0.1.0-preview.2
```

For the current repository state, the source workflow below is the authoritative path.

## Fast path: compile once, invoke many times

Use `WistEngine.Compile<TDelegate>` for code that will be invoked repeatedly:

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateSafeFormulas();

var formula = wist.Compile<Func<double, double, double>>(
    "price * 0.9 + fee",
    "price",
    "fee");

double result = formula.CompiledDelegate(100.0, 5.0);
```

This is the intended hot path:

- compile once;
- invoke many times;
- benchmark compiled delegate invocation, not `Evaluate` in a tight loop.

## One-off Evaluate

Use `Evaluate` for one-off execution, onboarding, tests, validation UI, admin tools, and non-hot paths:

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateSafeFormulas();

double result = wist.Evaluate<double>(
    "price * 0.9 + fee",
    new
    {
        price = 100.0,
        fee = 5.0
    });
```

`Evaluate` is for convenience. It is not the primary performance path.

## Validation

Use `Validate` when a UI, import flow, or configuration pipeline needs non-throwing validation before execution:

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateSafeFormulas();

var validation = wist.Validate(
    "price * 0.9 + fee",
    new
    {
        price = 100.0,
        fee = 5.0
    });

if (!validation.IsValid)
{
    Console.WriteLine(validation.Message);
}
```

## Presets

```csharp
using UniversalToolchain.Wist;

using var restrictedArithmetic = WistEngine.CreateRestrictedArithmetic();
using var fullNativePreview = WistEngine.CreateFullNativePreview();

// Compatibility aliases:
using var safeFormulas = WistEngine.CreateSafeFormulas();
using var businessRules = WistEngine.CreateBusinessRules();
using var trusted = WistEngine.CreateTrusted();
```

`CreateRestrictedArithmetic` is the recommended first-contact preset. It maps to the shipped `pricing-restricted` profile.
`CreateFullNativePreview` maps to the broad native Wist preview profile and must not be used for untrusted input.

`CreateSafeFormulas` remains a compatibility alias for `CreateRestrictedArithmetic`. `CreateBusinessRules` and
`CreateTrusted` remain compatibility aliases for `CreateFullNativePreview`; they do not represent a separate stable
business-rules runtime or a hardened trust boundary.

`Safe` means a restricted language/runtime surface. It does not mean arbitrary untrusted code is safe to execute inside
the current process.

## Interpreter backend

The interpreter is a correctness, diagnostics, debugging, fallback, and semantic parity backend. The performance-oriented
path is compiled typed CIL-backed invocation.

## Security and trust

Restricted presets and dialects limit selected language/runtime surface. They are not hardened sandboxes, and compiled
execution is a performance feature rather than a sandbox boundary. For untrusted code, use process/environment isolation
with appropriate OS permissions and resource limits. See [docs/SECURITY.md](docs/SECURITY.md).

## Run the pricing demo

```bash
dotnet run --project UniversalToolchain/Example/Example.csproj
```

This runs a pricing formula through hardcoded C#, the shipped `full-default-native` Wist preset, and the shipped
`pricing-restricted` dialect. It shows the same calculation executed through compiler, interpreter, and fast native
invocation paths, plus rejection of a formula that the restricted dialect composition does not allow.

Code: [UniversalToolchain/Example/Scenarios/PricingDiscountScenario.cs](UniversalToolchain/Example/Scenarios/PricingDiscountScenario.cs) and [UniversalToolchain/Example/Scenarios/DslPricingCalculator.cs](UniversalToolchain/Example/Scenarios/DslPricingCalculator.cs).

## What this demo shows

The pricing demo compares three ways to own the same pricing calculation:

- hardcoded C# pricing logic,
- the shipped `full-default-native` Wist preset for the formula,
- a restricted pricing dialect with a narrower runtime surface,
- compiler, interpreter, and fast native invocation paths for the same calculation,
- rejection of a formula shape that the restricted dialect composition does not allow.

## Tiny CLI quick start

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --backend compiler
```

Expected output:

```text
12
```

## Advanced runtime integration example

Use `WistRuntimeFacadeBuilder` only for advanced/lower-level Wist runtime and dialect integration scenarios.

```csharp
using UniversalToolchain.Dialects.Wist.Presets;

using var wist = WistRuntimeFacadeBuilder
    .CreateDefault()
    .WithShippedDialectPreset(WistShippedDialectPresets.PricingRestricted)
    .Build();

var attempt = wist.TryCompile(
    """
    let discount = 0.9
    price * discount + fee
    """,
    new Dictionary<string, Type>
    {
        ["price"] = typeof(double),
        ["fee"] = typeof(double)
    },
    backend: "interpreter");
```

## When to use UniversalToolchain

Use UniversalToolchain when:

- a normal expression evaluator is too narrow for your formulas or DSL code,
- users need a syntax that matches your domain instead of C# syntax,
- you need a dialect profile that allows only selected language features,
- the same language should support compiler and interpreter backend aliases,
- you need an inspectable execution pipeline for validation, diagnostics, or backend work,
- you want configurable business logic without hardcoding every formula or policy into the application.

Typical scenarios include pricing formulas, routing policies, internal workflow calculations, and DSL experiments inside
.NET applications.

## When not to use UniversalToolchain

Do not start here when:

- you only need trivial arithmetic formulas,
- you only need a subset of C# expressions,
- you only need parser generation,
- you do not need a DSL or runtime story,
- a simple library call can already evaluate all formulas or policies you plan to support.

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

For a stable project map, see [Global project overview](docs/global-project-overview.md). For a stricter boundary between framework, reference language, and current limitations, see [Project positioning](docs/project-positioning.md). For an external-style evaluative architecture review, see [Technical due diligence review](docs/reviews/technical-due-diligence.md).

## Architecture at a glance

At a high level:

```text
Source -> Lexer/Parser -> AST -> Bytecode -> AIR -> Optimization -> Compiler/Interpreter -> Execution
```

Key repository architecture concepts:

- framework-first, composition-based pipeline design,
- dual backends (`compiler`, `interpreter`),
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

- `--backend <compiler|interpreter>`
- `--dialect-file <path>`
- `--list-modules`

`run` options:

- `--file <path>`
- `--eval`

`dialect-demo` options:

- `--file <path>`
- `--scenario <valid|invalid-syntax|semantic-conflict|unresolved-module>`

The user-facing `compiler` backend alias selects the canonical `cil` backend when a dialect declares `backend cil` or the
`compiler` alias. `--eval` is a flag; the source expression itself is passed as the positional code argument.

Examples:

```bash ci-timeout=240
# Run a .wist file with an explicit dialect definition
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/full-default/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/full-default/program.wist --backend interpreter

# Evaluate one expression
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --backend compiler
```

```bash ci-run=false
# Start REPL
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- repl --backend compiler
```

## Dialect usage

```bash ci-timeout=240
# Run code with a dialect definition
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/full-default/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/full-default/program.wist --backend interpreter

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

Public dialect documentation should follow the `.wistdialect` shape used by these shipped profiles. Secondary parser experiments must not be treated as the runtime dialect contract unless the runtime path is intentionally migrated to them.

## Why .NET 10 right now?

The current validation baseline is .NET 10 (`net10.0`) with SDK `10.0.103`.
Active runtime and backend work is being verified there first, so older target frameworks are not the current compatibility target.

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
- Global project overview: [docs/global-project-overview.md](docs/global-project-overview.md)
- Project positioning and public wording: [docs/project-positioning.md](docs/project-positioning.md)
- Performance model: [docs/public/performance-model.md](docs/public/performance-model.md)
- Preview stability: [docs/public/what-is-stable-in-preview.md](docs/public/what-is-stable-in-preview.md)
- Current limitations: [docs/limitations.md](docs/limitations.md)
- Technical due diligence review: [docs/reviews/technical-due-diligence.md](docs/reviews/technical-due-diligence.md)
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


## Development validation

See [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) for required release commands and manual smoke checks.
