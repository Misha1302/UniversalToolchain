---
title: External Language Authoring Quickstart
description: Run the independent sample from a clean checkout, then create a package-based language project.
audience: external-language-author
status: current-alpha
lastVerifiedAgainst: language-authoring-p0-p1-hardening-2026-07-23.1
---

# External Language Authoring quickstart

This page has two entry modes:

1. **repository sample** — the fastest way to observe the complete non-Wist route from a clean checkout;
2. **package/template consumer** — the correct boundary for an independently developed language.

The SDK is a low-level alpha. You provide the syntax model, parser, transformations and backend executors; UniversalToolchain owns typed composition, deterministic planning, route validation and runtime lifecycle.

## Prerequisites

- .NET SDK 10;
- a clean checkout or extracted release archive;
- no prior build is required for the first command.

## 1. Run the independent sample

From the repository root:

```bash
dotnet run --project samples/Acme.PricingLanguage/Acme.PricingLanguage.csproj
```

Expected output:

```text
35.0:35.0
```

The first value is produced by the interpreter backend; the second is produced by the compiled backend. Equality is the observable parity check for this sample.

The complete source is in [`samples/Acme.PricingLanguage/Program.cs`](https://github.com/Misha1302/Wist2/blob/master/samples/Acme.PricingLanguage/Program.cs). The sample references the generic Language Authoring packages and does not reference Wist.

## 2. Understand the five objects

| Object | Responsibility |
|---|---|
| `LanguageArtifactKind<T>` | stable typed identity of an artifact flowing through a route |
| `LanguagePackageBuilder` | one package-owned source of descriptor metadata and runtime component factories |
| `LanguageDefinitionBuilder` | selects features, backends, runtime provider and policy for one language configuration |
| `LanguageCompiler` | resolves contributions and produces an immutable deterministic plan |
| `LanguageRuntime` | assembles exact package implementations and executes the selected backend route |

The generic SDK does not require Wist AST, Bytecode or AIR. A language may use its own artifact types, as the pricing sample does.

## 3. Define artifact kinds and backend identities

```csharp
var interpreter = new BackendId("interpreter");
var compiled = new BackendId("compiled");

var syntax = new LanguageArtifactKind<PricingExpression>(
    "acme.pricing.syntax");
var executable = new LanguageArtifactKind<Func<decimal>>(
    "acme.pricing.executable");
```

A typed artifact kind combines a stable contract identity with the CLR type expected by the runtime. Independently versioned packages must keep the identity stable or introduce an explicit compatibility/migration boundary.

## 4. Author one package

```csharp
var package = LanguagePackageBuilder.Create("Acme.PricingLanguage", "1.0.0")
    .AddFeature("acme.pricing.core", feature => feature
        .AddTransformer(
            "acme.pricing.parse",
            LanguageSlots.FrontendParser,
            StandardLanguageArtifactKinds.SourceText,
            syntax,
            static (source, _) => PricingExpression.Parse(source),
            LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
            cost: 1)
        .AddTransformer(
            "acme.pricing.compile",
            LanguageSlots.Lowering,
            syntax,
            executable,
            static (expression, _) => expression.Compile(),
            LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
            cost: 1,
            supportedBackends: [compiled])
        .AddBackend(
            interpreter,
            new LanguageContributionId("acme.pricing.interpreter"),
            syntax,
            static (expression, _) => expression.Evaluate(),
            LanguageRuntimeComponentTraits.DeterministicNoHostInterop)
        .AddBackend(
            compiled,
            new LanguageContributionId("acme.pricing.compiled"),
            executable,
            static (program, _) => program(),
            LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
    .UseRouteRuntime("acme.pricing.runtime", "1.0.0")
    .Build();
```

`Build()` produces two coupled immutable outputs:

- package descriptor metadata used by the planner;
- a runtime component catalog containing the typed registrations and factories.

This prevents a descriptor from advertising a route whose implementation has a different CLR contract.

## 5. Compile a language definition

```csharp
var registry = new LanguagePackageRegistry()
    .AddPackage(package);

var definition = LanguageDefinitionBuilder
    .Create("Acme.PricingLanguage", "1.0.0")
    .UseFeature("acme.pricing.core")
    .EnableBackend(interpreter)
    .EnableBackend(compiled)
    .UseRuntimeProvider("acme.pricing.runtime", "1.0.0")
    .WithRuntimePolicy(new LanguageRuntimePolicy(
        RequireDeterminism: true,
        MaximumSourceLength: 256))
    .Build();

var compilation = new LanguageCompiler(registry).Compile(definition);
if (!compilation.IsSuccess)
{
    foreach (var diagnostic in compilation.Diagnostics)
        Console.Error.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
    return;
}

var plan = compilation.GetRequiredPlan();
```

`LanguageCompiler.Compile` performs package/contribution planning. It does not parse the end-user source program. A UI or service should inspect structured diagnostics instead of immediately calling `GetRequiredPlan()` on an untrusted definition.

## 6. Create and run the runtime

```csharp
using var runtime = LanguageRuntime.Create(
    plan,
    new ILanguageRouteComponentSource[] { package });

const string source = "12.5 * 3 - 2.5";

var interpreted = runtime.Run(
    new LanguageExecutionRequest(source, interpreter));
var compiledValue = runtime.Run(
    new LanguageExecutionRequest(source, compiled));

Console.WriteLine($"interpreter = {interpreted.Value}");
Console.WriteLine($"compiled    = {compiledValue.Value}");
Console.WriteLine($"parity      = {Equals(interpreted.Value, compiledValue.Value)}");
```

Runtime assembly verifies the exact package identity captured by the plan, contribution contracts, provider identity and runtime policy before a session is created.

## 7. Create a project from release packages

The generic SDK package family is currently verified as release artifacts at version `0.3.0-alpha.1`; only the Wist facade is documented as published on NuGet.org. Build or obtain the release package directory first.

After the canonical repository build, install the template from the produced package:

```bash ci-run=false
dotnet new install ./artifacts/packages/UniversalToolchain.Templates.0.3.0-alpha.1.nupkg
dotnet new ut-language -n MyCompany.MyLanguage
cd MyCompany.MyLanguage
dotnet run
```

To create a project without the template:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="UniversalToolchain.LanguageAuthoring" Version="0.3.0-alpha.1" />
    <PackageReference Include="UniversalToolchain.LanguageSdk" Version="0.3.0-alpha.1" />
    <PackageReference Include="UniversalToolchain.Runtime" Version="0.3.0-alpha.1" />
  </ItemGroup>
</Project>
```

Configure `artifacts/packages` (or the extracted release package directory) as a NuGet source before restore. The release smoke test verifies a clean consumer that restores only from the produced package set.

## Next steps

1. [Packages and contributions](/language-authoring/package-model)
2. [Contribution planning and diagnostics](/language-authoring/contribution-planning)
3. [Typed artifact routing](/language-authoring/artifact-routing)
4. [Runtime lifecycle and policy](/language-authoring/runtime-lifecycle)
5. [Testing and templates](/language-authoring/testing-and-templates)
