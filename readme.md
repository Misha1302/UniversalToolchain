# UniversalToolchain

**Do not execute AI-generated code. Execute tiny rules in a language your .NET app controls.**

UniversalToolchain is a compiler/runtime framework for restricted formulas and application DSLs.

Start with small numeric rules. Validate them before execution. Interpret them when diagnostics matter. Compile hot paths into typed .NET delegates when throughput matters.

```text
admin / config / LLM suggestion
        -> tiny rule text
        -> restricted Wist formula surface
        -> validation or rejection
        -> interpreter for diagnostics
        -> CIL-backed typed delegate for hot paths
        -> your application decides the side effect
```

Wist is the reference language in this repository. `UniversalToolchain.Wist` is the first-contact facade for .NET developers.

## 30-second demo

A product manager, admin UI, config file, or LLM can suggest a rollout score formula:

```text
usage * 0.7 + reliability * 0.3 - incidents * 15.0
```

Your application owns the inputs, compiles the approved formula once, and decides what the score means:

```csharp
using UniversalToolchain.Wist;

using var rules = WistEngine.CreateSafeFormulas();

var rolloutScore = rules.Compile<Func<double, double, double, double>>(
    "usage * 0.7 + reliability * 0.3 - incidents * 15.0",
    "usage",
    "reliability",
    "incidents");

double score = rolloutScore.CompiledDelegate(100.0, 90.0, 1.0);
bool enableNewDashboard = score >= 80.0;
```

The rule returns data. Your .NET application performs the action.

## The important part: rejection before execution

The preview safe-formula profile intentionally starts narrow. Statement-style bindings are not part of that restricted surface:

```csharp
using UniversalToolchain.Wist;

using var rules = WistEngine.CreateSafeFormulas();

var validation = rules.Validate(
    "let score = usage * 0.7\nscore",
    new
    {
        usage = 100.0,
        reliability = 90.0,
        incidents = 1.0
    });

Console.WriteLine(validation.IsValid); // false
Console.WriteLine(validation.Message); // Feature 'let' is not enabled by this preset.
```

That is the core product idea: your app does not accept an arbitrary programming language just because somebody wants configurable logic.

## Why this exists

Most applications eventually move through this path:

```text
hardcoded C# logic
        -> configurable formulas
        -> user/admin/LLM-suggested rules
        -> restricted application DSL
        -> compiled hot path
```

Without a language/runtime layer, teams usually choose one of two bad extremes:

- encode logic as unreadable JSON trees;
- expose a broad scripting language and hope nothing surprising happens.

UniversalToolchain is for the middle ground: a language surface your application can own.

## What is real in this preview

The current public preview is intentionally scoped:

| Capability | Current status |
|---|---|
| `WistEngine` facade | available in `UniversalToolchain.Wist` |
| Restricted arithmetic/formula preset | available through `CreateSafeFormulas()` / `CreateRestrictedArithmetic()` |
| One-off evaluation | available through `Evaluate<T>()` |
| Non-throwing validation | available through `Validate()` and `TryCompile<TDelegate>()` |
| Typed compiled hot path | available through `Compile<TDelegate>()` and `CompileFunc(...)` |
| Interpreter backend | available for diagnostics, fallback, and semantic parity work |
| Dialect composition | available through shipped `.wistdialect` profiles and lower-level APIs |
| Full business-rule DSL | direction, not a stable 1.0 claim |
| Hardened sandboxing | not claimed |

## Install

The package metadata in this repository is prepared for `UniversalToolchain.Wist` `0.1.0-preview.2`.

```bash ci-run=false
dotnet add package UniversalToolchain.Wist --version 0.1.0-preview.2
```

For the current repository state, source checkout is still the authoritative path.

Requirements:

- .NET SDK `10.0.103` or a compatible prerelease SDK selected by `UniversalToolchain/global.json`.
- Target framework: `net10.0`.
- SDK policy: `rollForward: latestMajor`, `allowPrerelease: true`.

## Run the included formula demo

From the repository root:

```bash ci-timeout=240
dotnet run --project UniversalToolchain/Example/Example.csproj
```

The current demo exercises formula execution through shipped Wist profiles. It shows compiler, interpreter and fast native invocation paths plus rejection of a formula shape that the restricted dialect composition does not allow.

## Fast path: compile once, invoke many times

Use `Compile<TDelegate>` when a formula is invoked repeatedly:

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateSafeFormulas();

var formula = wist.Compile<Func<double, double, double>>(
    "usage * weight",
    "usage",
    "weight");

double result = formula.CompiledDelegate(100.0, 0.7);
```

This is the intended hot path:

- compile once;
- keep the returned program;
- invoke the typed delegate repeatedly;
- benchmark compiled delegate invocation, not `Evaluate` in a tight loop.

## One-off Evaluate

Use `Evaluate` for onboarding, admin previews, tests, validation UI, and non-hot paths:

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateSafeFormulas();

double result = wist.Evaluate<double>(
    "usage * 0.7 + reliability * 0.3",
    new
    {
        usage = 100.0,
        reliability = 90.0
    });
```

`Evaluate` is a convenience path. It is not the primary performance claim.

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

`CreateRestrictedArithmetic` is the recommended first-contact preset. It maps to the shipped `pricing-restricted` profile in this preview.

`CreateFullNativePreview` maps to the broad native Wist preview profile and must not be used for untrusted input.

`CreateSafeFormulas` remains a compatibility alias for `CreateRestrictedArithmetic`. `CreateBusinessRules` and `CreateTrusted` remain compatibility aliases for `CreateFullNativePreview`; they do not represent a separate stable business-rules runtime or a hardened trust boundary.

## What this is not

UniversalToolchain is not:

- a hardened sandbox for arbitrary untrusted code;
- a replacement for C#;
- a finished general-purpose language workbench;
- only a calculator;
- only a parser generator.

Restricted presets and dialects limit selected language/runtime surface. They are not process isolation, resource isolation, or a security boundary by themselves. For untrusted execution, use process/environment isolation with appropriate OS permissions and resource limits. See [docs/SECURITY.md](docs/SECURITY.md).

## When to use UniversalToolchain

Use it when:

- JSON configuration is starting to become logic;
- expression evaluators are too small for the direction of your product;
- C# scripting is too broad for the surface you want to expose;
- you want user/admin/LLM-suggested formulas to pass through validation before execution;
- you need both interpreter and compiler paths for the same language surface;
- you want configurable .NET logic without hardcoding every formula into the application.

Typical scenarios:

- scoring and rollout formulas;
- alerting and monitoring formulas;
- workflow decision scores;
- LMS/autograding scores;
- pricing and commission formulas;
- restricted DSL experiments inside .NET applications.

## Architecture at a glance

```text
Source
  -> Lexer / Parser
  -> AST
  -> Bytecode
  -> AIR
  -> Optimizers
  -> Compiler / Interpreter
  -> Execution
```

Key project ideas:

- framework-first, composition-based pipeline design;
- Wist as the reference language, not the only product direction;
- dialect-driven runtime composition through `.wistdialect` files;
- dual backend model: `compiler` and `interpreter`;
- semantic parity checks so one DSL does not silently become two languages.

## CLI quick start

```bash ci-timeout=240
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --backend compiler
```

Expected output:

```text
12
```

## Dialect examples

Located under `UniversalToolchain/Dialects/examples/wist`:

- `full-default`: standard Wist profile over `cil` and `interpreter`.
- `full-default-native`: native arithmetic/type profile over `cil` and `interpreter`.
- `function-calls-safe-math`: neutral FunctionCalls + SafeMath profile without rule declarations.
- `minimal-arithmetic`: smallest interpreter arithmetic profile.
- `minimal-arithmetic-native`: smallest native arithmetic profile over `cil`.
- `pricing-restricted`: composition-constrained formula profile with a restricted runtime surface.
- `restricted-sandbox`: composition-constrained profile, not a hardened sandbox guarantee.

## Technical story

UniversalToolchain also has a compiler/runtime research angle: language features are composed as modules, selected semantics lower through Bytecode and AIR, and supported hot paths become typed CIL operations handed to the .NET JIT.

Conference-oriented material:

- [LangDev 2026 proposal](docs/talks/langdev-2026/README.md)
- [Module-to-CIL lowering walkthrough](docs/talks/langdev-2026/lowering-walkthrough.md)
- [Semantic parity regression](docs/talks/langdev-2026/parity-regression.md)
- [Benchmark evidence and limitations](docs/talks/langdev-2026/benchmark-evidence.md)

## Build and test from source

From repository root:

```bash ci-run=false
dotnet restore UniversalToolchain/Wist.sln -p:Platform="Any CPU"
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore -p:Platform="Any CPU"
dotnet test UniversalToolchain/Wist.sln -c Release --no-build -p:Platform="Any CPU"
```

## Documentation map

- Start: [docs/index.md](docs/index.md)
- Installation: [docs/start/installation.md](docs/start/installation.md)
- First program: [docs/start/first-program.md](docs/start/first-program.md)
- Project positioning: [docs/project-positioning.md](docs/project-positioning.md)
- Performance model: [docs/public/performance-model.md](docs/public/performance-model.md)
- Preview stability: [docs/public/what-is-stable-in-preview.md](docs/public/what-is-stable-in-preview.md)
- Current limitations: [docs/limitations.md](docs/limitations.md)
- Architecture guardrails: [docs/ARCHITECTURE_RULES.md](docs/ARCHITECTURE_RULES.md)
- Contribution workflow: [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md)
- Security policy: [docs/SECURITY.md](docs/SECURITY.md)

## License

Licensed under Apache License 2.0. See [LICENSE](LICENSE).
