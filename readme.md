# UniversalToolchain

[![NuGet](https://img.shields.io/nuget/vpre/UniversalToolchain.Wist.svg)](https://www.nuget.org/packages/UniversalToolchain.Wist)
[![NuGet Downloads](https://img.shields.io/nuget/dt/UniversalToolchain.Wist.svg)](https://www.nuget.org/packages/UniversalToolchain.Wist)

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

using var rules = WistEngine.CreateRestrictedArithmetic();

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

The restricted arithmetic profile intentionally starts narrow. Statement-style bindings are not part of that restricted surface:

```csharp
using UniversalToolchain.Wist;

using var rules = WistEngine.CreateRestrictedArithmetic();

var validation = rules.Validate(
    "let score = usage * 0.7\nscore",
    new
    {
        usage = 100.0,
        reliability = 90.0,
        incidents = 1.0
    });

Console.WriteLine(validation.IsValid); // false
Console.WriteLine(validation.Message); // Prints the restricted-surface validation error.
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
| Restricted arithmetic/formula preset | available through `CreateRestrictedArithmetic()` |
| One-off evaluation | available through `Evaluate<T>()` |
| Non-throwing validation | available through `Validate()` and `TryCompile<TDelegate>()` |
| Typed compiled hot path | available through `Compile<TDelegate>()` |
| Interpreter backend | available for diagnostics, fallback, and semantic parity work |
| Dialect composition | available through shipped `.wistdialect` profiles and lower-level APIs |
| Experimental SSA route | opt-in through `WistEngineOptions.Optimization.Ssa`, with an observable report and controlled `Prefer` fallback |
| Full business-rule DSL | direction, not a stable 1.0 claim |
| Hardened sandboxing | not claimed |

## Install

After the preview package is published, install `UniversalToolchain.Wist` `0.1.0-alpha.1` from NuGet: <https://www.nuget.org/packages/UniversalToolchain.Wist/0.1.0-alpha.1>. Until that package exists, use a local package produced by `dotnet pack` or a source checkout.

```bash ci-run=false
dotnet add package UniversalToolchain.Wist --version 0.1.0-alpha.1
```

Use source checkout when developing framework internals, modules, dialects, or repository documentation.

Requirements:

- .NET SDK `10.0.103` or a compatible prerelease SDK selected by `UniversalToolchain/global.json`.
- Target framework: `net10.0`.
- SDK policy: `rollForward: latestFeature`, `allowPrerelease: true`.

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

using var wist = WistEngine.CreateRestrictedArithmetic();

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

using var wist = WistEngine.CreateRestrictedArithmetic();

double result = wist.Evaluate<double>(
    "usage * 0.7 + reliability * 0.3",
    new
    {
        usage = 100.0,
        reliability = 90.0
    });
```

`Evaluate` is a convenience path. It is not the primary performance claim.

## Experimental SSA route

SSA is optional and remains an experimental compiler feature. It can be enabled without a physical dialect file:

```csharp
using var wist = WistEngine.Create(new WistEngineOptions
{
    Preset = WistPreset.RestrictedArithmetic,
    Optimization = new WistOptimizationOptions
    {
        Ssa = new WistSsaOptions
        {
            Policy = WistSsaPolicy.Prefer,
            DiagnosticLevel = WistSsaDiagnosticLevel.Detailed
        }
    }
});

var program = wist.Compile<Func<int, int>>("value * 2 + 3", "value");
Console.WriteLine(program.CompiledDelegate(20)); // 43
Console.WriteLine(program.Metadata.OptimizationReport.Ssa.UsedSsa);
```

`Prefer` may return to the original AIR only for known unsupported-route diagnostics. `Require` and `Debug` fail instead; unexpected optimizer defects are never converted into silent fallback. The route is `AIR -> SSA -> AIR`, not an SSA-native backend or a performance guarantee.

## Presets

```csharp
using UniversalToolchain.Wist;

using var restrictedArithmetic = WistEngine.CreateRestrictedArithmetic();
using var fullNativePreview = WistEngine.CreateFullNative();
using var trustedInterop = WistEngine.Create(new WistEngineOptions
{
    Preset = WistPreset.FullNative,
    AllowedAssemblies = [typeof(Math).Assembly]
});
```

`CreateRestrictedArithmetic` is the recommended first-contact preset. It maps to the shipped `pricing-restricted` profile in this preview.

`CreateFullNative` maps to the broad native Wist preview profile, but CLR interop remains empty except for the shipped standard library until the host supplies `AllowedAssemblies`. It must not be used for untrusted input.

The facade intentionally exposes only the two explicit presets above. Product-specific policy belongs in a reviewed `WistEngineOptions` composition, not in ambiguous trust or business-rule aliases.

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
- `composition-restricted`: composition-constrained profile, not a hardened sandbox guarantee.

## Technical story

UniversalToolchain also has a compiler/runtime research angle: language features are composed as modules, selected semantics lower through Bytecode and AIR, and supported hot paths become typed CIL operations handed to the .NET JIT.

Conference-oriented material:

- [LangDev 2026 proposal](docs/talks/langdev-2026/README.md)
- [Module-to-CIL lowering walkthrough](docs/talks/langdev-2026/lowering-walkthrough.md)
- [Semantic parity regression](docs/talks/langdev-2026/parity-regression.md)
- [Benchmark evidence and limitations](docs/talks/langdev-2026/benchmark-evidence.md)

## Build and test from source

Use the canonical repository entrypoint from the repository root:

```bash ci-run=false
./build.sh
```

The wrapper performs serial restore/build for the large project graph, runs the three test projects, packs the public facade, validates package-surface growth, builds the documentation with `npm ci`, and runs Markdown checks. Useful bounded variants:

```bash ci-run=false
./build.sh --skip-docs
./build.sh --skip-docs --skip-pack
```

On Windows PowerShell, use `./build.ps1` with `-SkipDocs` or `-SkipPack`. Set `DOTNET` to an explicit host path when the SDK is supplied by an offline sidecar.

Do not replace this entrypoint in release evidence with an ad-hoc parallel solution build: the current .NET 10 project-reference graph is intentionally restored and built serially for deterministic behavior.

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
