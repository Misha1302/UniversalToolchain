#!/usr/bin/env python3
"""Install and execute a packed Wist facade from an isolated consumer workspace."""
from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import tempfile
from pathlib import Path
from xml.sax.saxutils import escape

PROGRAM = r'''using UniversalToolchain.Wist;

var presets = new[]
{
    new Preset("full-default", "cil", new[] { "cil", "interpreter" }),
    new Preset("full-default-native", "cil", new[] { "cil", "interpreter" }),
    new Preset("function-calls-safe-math", "cil", new[] { "cil", "interpreter" }),
    new Preset("minimal-arithmetic", "interpreter", new[] { "interpreter" }),
    new Preset("minimal-arithmetic-grouped", "interpreter", new[] { "interpreter" }),
    new Preset("minimal-arithmetic-native", "cil", new[] { "cil" }),
    new Preset("pricing-restricted", "cil", new[] { "cil", "interpreter" }),
    new Preset("ssa", "cil", new[] { "cil", "interpreter" }),
    new Preset("composition-restricted", "interpreter", new[] { "interpreter" })
};

foreach (var preset in presets)
{
    var defaults = WistEngineOptions.FromPresetId(preset.Id);
    Equal(preset.DefaultBackend, defaults.BackendId, $"{preset.Id} default backend");
    Exercise(preset.Id, defaults.BackendId);
    ExercisePresetSemantics(preset.Id, defaults.BackendId);

    foreach (var backend in preset.SupportedBackends)
    {
        Exercise(preset.Id, backend);
        ExercisePresetSemantics(preset.Id, backend);
    }

    foreach (var backend in new[] { "cil", "interpreter" }.Except(preset.SupportedBackends, StringComparer.Ordinal))
    {
        var rejected = false;
        try
        {
            using var invalid = WistEngine.Create(new WistEngineOptions
            {
                DialectSource = WistDialectSource.FromShippedPreset(preset.Id),
                BackendId = backend
            });
        }
        catch (ArgumentOutOfRangeException)
        {
            rejected = true;
        }

        if (!rejected)
            throw new InvalidOperationException($"Preset '{preset.Id}' accepted unsupported backend '{backend}'.");
    }
}

static void Exercise(string presetId, string backend)
{
    using var engine = WistEngine.Create(new WistEngineOptions
    {
        DialectSource = WistDialectSource.FromShippedPreset(presetId),
        BackendId = backend
    });

    Close(5.0, engine.Evaluate<double>("2 + 3"), $"{presetId}/{backend} evaluate");
    var program = engine.Compile<Func<double>>("2 + 3");
    Equal(backend, program.Metadata.Backend, $"{presetId}/{backend} metadata backend");
    Close(5.0, program.CompiledDelegate(), $"{presetId}/{backend} compiled delegate");

    var validated = engine.Validate("2 + 3");
    if (!validated.IsValid)
        throw new InvalidOperationException($"{presetId}/{backend} validation failed: {string.Join(" | ", validated.Diagnostics.Select(x => x.Message))}");
}

static void ExercisePresetSemantics(string presetId, string backend)
{
    using var engine = WistEngine.Create(new WistEngineOptions
    {
        DialectSource = WistDialectSource.FromShippedPreset(presetId),
        BackendId = backend
    });

    var boxed = engine.Evaluate<object>("2 + 3");
    if (string.Equals(boxed.GetType().Assembly.GetName().Name, "NumbersModule", StringComparison.Ordinal))
        throw new InvalidOperationException($"{presetId}/{backend} leaked NumbersModule implementation type through object result.");

    switch (presetId)
    {
        case "full-default":
            Close(5.0, engine.Evaluate<double>("let x = 2.0\nx + 3.0"), $"{presetId}/{backend} variable semantics");
            ParameterContract(engine, presetId, backend);
            TrustedInteropContract(presetId, backend);
            break;
        case "full-default-native":
            ParameterContract(engine, presetId, backend);
            break;
        case "pricing-restricted":
            ParameterContract(engine, presetId, backend);
            break;
        case "ssa":
            ParameterContract(engine, presetId, backend);
            RestrictedInteropContract(presetId, backend);
            break;
        case "function-calls-safe-math":
            Close(100.0, engine.Evaluate<double>("clamp(120.0, 0.0, 100.0)"), $"{presetId}/{backend} safe math");
            ParameterContract(engine, presetId, backend);
            break;
        case "minimal-arithmetic-grouped":
            Close(10.0, engine.Evaluate<double>("(2 + 3) * 2"), $"{presetId}/{backend} grouping");
            ExpectInvalid(engine, "let x = 2.0\nx + 3.0", presetId, backend, "variables");
            break;
        case "minimal-arithmetic":
        case "minimal-arithmetic-native":
        case "composition-restricted":
            ExpectInvalid(engine, "let x = 2.0\nx + 3.0", presetId, backend, "variables");
            break;
        default:
            throw new InvalidOperationException($"No semantic contract is declared for shipped preset '{presetId}'.");
    }
}

static void TrustedInteropContract(string presetId, string backend)
{
    using var engine = WistEngine.Create(new WistEngineOptions
    {
        DialectSource = WistDialectSource.FromShippedPreset(presetId),
        BackendId = backend,
        AllowedAssemblies = [typeof(Math).Assembly]
    });
    Close(5.0, engine.Compile<Func<double>>("System.Math.Sqrt(16.0) + 1.0").CompiledDelegate(), $"{presetId}/{backend} trusted interop");
}

static void RestrictedInteropContract(string presetId, string backend)
{
    using var engine = WistEngine.Create(new WistEngineOptions
    {
        DialectSource = WistDialectSource.FromShippedPreset(presetId),
        BackendId = backend,
        AllowedAssemblies = [typeof(Math).Assembly]
    });
    ExpectInvalid(engine, "System.Math.Sqrt(16.0)", presetId, backend, "CLR interop");
}

static void ParameterContract(WistEngine engine, string presetId, string backend)
{
    Close(5.0, engine.Evaluate<double>("x + 3.0", new { x = 2.0d }), $"{presetId}/{backend} CLR parameter evaluate");
    var validation = engine.Validate("x + 3.0", new { x = 2.0d });
    if (!validation.IsValid)
        throw new InvalidOperationException($"{presetId}/{backend} CLR parameter validation failed: {string.Join(" | ", validation.Diagnostics.Select(x => x.Message))}");
    Close(5.0, engine.Compile<Func<double, double>>("x + 3.0", "x").CompiledDelegate(2d), $"{presetId}/{backend} CLR parameter compile");
}

static void ExpectInvalid(WistEngine engine, string source, string presetId, string backend, string feature)
{
    var validation = engine.Validate(source);
    if (validation.IsValid)
        throw new InvalidOperationException($"{presetId}/{backend} unexpectedly enabled excluded feature '{feature}'.");
}

static void Equal(string expected, string actual, string label)
{
    if (!string.Equals(expected, actual, StringComparison.Ordinal))
        throw new InvalidOperationException($"Expected {label} '{expected}', got '{actual}'.");
}

static void Close(double expected, double actual, string label)
{
    if (Math.Abs(expected - actual) > 1e-9)
        throw new InvalidOperationException($"Expected {label} {expected}, got {actual}.");
}

internal sealed record Preset(string Id, string DefaultBackend, IReadOnlyList<string> SupportedBackends);
'''


def run(command: list[str], *, cwd: Path, env: dict[str, str], expect_success: bool = True) -> subprocess.CompletedProcess[str]:
    result = subprocess.run(command, cwd=cwd, env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if expect_success and result.returncode != 0:
        raise RuntimeError(f"command failed ({result.returncode}): {' '.join(command)}\n{result.stdout}")
    return result


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--package-dir", required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--dotnet", default=os.environ.get("DOTNET", "dotnet"))
    parser.add_argument("--dependency-source", default=os.environ.get("WIST_DEPENDENCY_SOURCE", "https://api.nuget.org/v3/index.json"))
    args = parser.parse_args()

    package_dir = Path(args.package_dir).resolve()
    package = package_dir / f"UniversalToolchain.Wist.{args.version}.nupkg"
    if not package.is_file():
        raise FileNotFoundError(package)

    with tempfile.TemporaryDirectory(prefix="wist-clean-consumer-") as temp:
        root = Path(temp)
        consumer = root / "consumer"
        consumer.mkdir()
        config = root / "NuGet.Config"
        config.write_text(
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
            "<configuration><packageSources><clear />"
            f"<add key=\"wist-package\" value=\"{escape(str(package_dir))}\" />"
            f"<add key=\"dependencies\" value=\"{escape(args.dependency_source)}\" />"
            "</packageSources><config>"
            f"<add key=\"globalPackagesFolder\" value=\"{escape(str(root / 'packages'))}\" />"
            "<add key=\"signatureValidationMode\" value=\"accept\" />"
            "</config></configuration>\n",
            encoding="utf-8",
        )

        csproj = consumer / "Consumer.csproj"
        csproj.write_text(
            f'''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="UniversalToolchain.Wist" Version="{args.version}" />
  </ItemGroup>
</Project>
''',
            encoding="utf-8",
        )
        (consumer / "Program.cs").write_text(PROGRAM, encoding="utf-8")

        env = os.environ.copy()
        env.update(
            {
                "DOTNET_CLI_HOME": str(root / "dotnet-home"),
                "NUGET_PACKAGES": str(root / "packages"),
                "NUGET_HTTP_CACHE_PATH": str(root / "http-cache"),
                "DOTNET_NOLOGO": "1",
                "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
                "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1",
                "NuGetAudit": "false",
            }
        )
        run([args.dotnet, "restore", str(csproj), "--configfile", str(config), "--disable-parallel", "-p:NuGetAudit=false"], cwd=consumer, env=env)
        run([args.dotnet, "build", str(csproj), "-c", "Release", "--no-restore", "-m:1"], cwd=consumer, env=env)
        run([args.dotnet, "run", "--project", str(csproj), "-c", "Release", "--no-build", "--no-restore"], cwd=consumer, env=env)

        metadata = root / "packages" / "universaltoolchain.wist" / args.version / ".nupkg.metadata"
        if not metadata.is_file() or str(package_dir) not in metadata.read_text(encoding="utf-8"):
            raise RuntimeError("consumer was not restored from the requested package directory")

        # Seed an incompatible-checkout mutation. The package facade references runtime
        # identity generation 2; replacing one runtime DLL with generation 1 must fail
        # before any formula can execute.
        fake = root / "fake-integration"
        fake.mkdir()
        (fake / "Fake.csproj").write_text(
            '''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>UniversalToolchain.Dialects.Integration</AssemblyName>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
  </PropertyGroup>
</Project>
''', encoding="utf-8")
        (fake / "Placeholder.cs").write_text("namespace SeededIncompatibleCheckout; public sealed class Placeholder;\n", encoding="utf-8")
        run([args.dotnet, "restore", str(fake / "Fake.csproj"), "--configfile", str(config), "--disable-parallel", "-p:NuGetAudit=false"], cwd=fake, env=env)
        run([args.dotnet, "build", str(fake / "Fake.csproj"), "-c", "Release", "--no-restore", "-m:1"], cwd=fake, env=env)
        consumer_candidates = [
            candidate
            for candidate in (consumer / "bin").rglob("Consumer.dll")
            if "ref" not in candidate.parts
        ]
        if len(consumer_candidates) != 1:
            raise RuntimeError(f"expected one consumer assembly, found: {consumer_candidates}")
        output = consumer_candidates[0].parent
        fake_candidates = [
            candidate
            for candidate in (fake / "bin").rglob("UniversalToolchain.Dialects.Integration.dll")
            if "ref" not in candidate.parts
        ]
        if len(fake_candidates) != 1:
            raise RuntimeError(f"expected one fake integration assembly, found: {fake_candidates}")
        shutil.copy2(fake_candidates[0], output)
        incompatible = run([args.dotnet, str(consumer_candidates[0])], cwd=output, env=env, expect_success=False)
        if incompatible.returncode == 0:
            raise RuntimeError("seeded incompatible runtime checkout executed successfully")
        if "UniversalToolchain.Dialects.Integration" not in incompatible.stdout:
            raise RuntimeError(f"incompatible checkout failed without deterministic assembly owner evidence:\n{incompatible.stdout}")

    print(f"UniversalToolchain.Wist {args.version}: clean consumer, preset matrix, and incompatible-checkout rejection passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
