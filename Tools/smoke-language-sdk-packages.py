#!/usr/bin/env python3
from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path
from xml.sax.saxutils import escape


def run(command: list[str], *, cwd: Path | None = None, env: dict[str, str] | None = None) -> None:
    print("+", " ".join(command), flush=True)
    subprocess.run(command, cwd=cwd, env=env, check=True)


def child_text(root: ET.Element, name: str) -> str | None:
    for element in root.iter():
        if element.tag.rsplit("}", 1)[-1] == name and element.text:
            return element.text.strip()
    return None


def project_version(project: Path) -> str:
    root = ET.parse(project).getroot()
    value = child_text(root, "PackageVersion") or child_text(root, "Version")
    if not value:
        raise RuntimeError(f"No package version in {project}")
    return value


def restore_args(config_file: Path) -> list[str]:
    return ["--configfile", str(config_file)]


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path.cwd())
    parser.add_argument("--packages", type=Path, default=Path("artifacts/packages"))
    parser.add_argument("--dotnet", default=os.environ.get("DOTNET", "dotnet"))
    parser.add_argument(
        "--dependency-source",
        default=os.environ.get("WIST_DEPENDENCY_SOURCE", "https://api.nuget.org/v3/index.json"),
    )
    args = parser.parse_args()

    root = args.root.resolve()
    package_dir = args.packages if args.packages.is_absolute() else (root / args.packages)
    package_dir = package_dir.resolve()
    sdk_version = project_version(
        root / "UniversalToolchain/UniversalToolchain.LanguageAuthoring/UniversalToolchain.LanguageAuthoring.csproj"
    )
    template_package = package_dir / f"UniversalToolchain.Templates.{sdk_version}.nupkg"
    if not template_package.is_file():
        raise RuntimeError(f"Template package not found: {template_package}")

    with tempfile.TemporaryDirectory(prefix="ut-language-sdk-smoke-") as temporary:
        temp = Path(temporary)
        config_file = temp / "NuGet.Config"
        config_file.write_text(
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
            "<configuration><packageSources><clear />"
            f"<add key=\"release-packages\" value=\"{escape(str(package_dir))}\" />"
            f"<add key=\"dependencies\" value=\"{escape(args.dependency_source)}\" />"
            "</packageSources><config>"
            f"<add key=\"globalPackagesFolder\" value=\"{escape(str(temp / 'packages'))}\" />"
            "<add key=\"signatureValidationMode\" value=\"accept\" />"
            "</config></configuration>\n",
            encoding="utf-8",
        )
        env = os.environ.copy()
        env.update(
            {
                "DOTNET_CLI_HOME": str(temp / "dotnet-home"),
                "NUGET_PACKAGES": str(temp / "packages"),
                "NUGET_HTTP_CACHE_PATH": str(temp / "http-cache"),
                "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
                "DOTNET_NOLOGO": "1",
                "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1",
                "NuGetAudit": "false",
            }
        )
        hive = temp / "template-hive"
        generated = temp / "Contoso.RuleLanguage"
        run([
            args.dotnet,
            "new",
            "install",
            str(template_package),
            "--force",
            "--debug:custom-hive",
            str(hive),
        ], env=env)
        run([
            args.dotnet,
            "new",
            "ut-language",
            "-n",
            "Contoso.RuleLanguage",
            "-o",
            str(generated),
            "--debug:custom-hive",
            str(hive),
        ], env=env)
        run([
            args.dotnet,
            "restore",
            str(generated / "Contoso.RuleLanguage.csproj"),
            "--disable-parallel",
            "--disable-build-servers",
            "-p:NuGetAudit=false",
            *restore_args(config_file),
        ], env=env)
        run([
            args.dotnet,
            "run",
            "--project",
            str(generated / "Contoso.RuleLanguage.csproj"),
            "-c",
            "Release",
            "--no-restore",
            "--disable-build-servers",
        ], env=env)

        consumer = temp / "CrossPackage.Consumer"
        consumer.mkdir()
        (consumer / "CrossPackage.Consumer.csproj").write_text(
            f'''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="UniversalToolchain.LanguageAuthoring" Version="{sdk_version}" />
    <PackageReference Include="UniversalToolchain.LanguageSdk" Version="{sdk_version}" />
    <PackageReference Include="UniversalToolchain.Runtime" Version="{sdk_version}" />
  </ItemGroup>
</Project>
''',
            encoding="utf-8",
        )
        (consumer / "Program.cs").write_text(
            '''using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

var syntax = new LanguageArtifactKind<int>("consumer.syntax");
var backend = new BackendId("consumer");
var traits = LanguageRuntimeComponentTraits.DeterministicNoHostInterop;
var frontend = LanguagePackageBuilder.Create("Consumer.Frontend", "1")
    .AddFeature("consumer.frontend", feature => feature.AddTransformer(
        "consumer.parse",
        LanguageSlots.FrontendParser,
        StandardLanguageArtifactKinds.SourceText,
        syntax,
        static (source, _) => int.Parse(source),
        traits))
    .Build();
var execution = LanguagePackageBuilder.Create("Consumer.Execution", "1")
    .AddBackend(
        backend.Value,
        "consumer.execute",
        syntax,
        static (value, _) => value + 1,
        traits)
    .UseRouteRuntime("consumer.runtime", "1")
    .Build();
var registry = new UniversalToolchain.FeatureSdk.LanguagePackageRegistry()
    .AddPackage(frontend)
    .AddPackage(execution);
var plan = new LanguageCompiler(registry).Compile(
    LanguageDefinitionBuilder.Create("Consumer.Language", "1")
        .UseFeature("consumer.frontend")
        .EnableBackend(backend)
        .UseRuntimeProvider("consumer.runtime", "1")
        .Build()).GetRequiredPlan();
using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { frontend, execution });
var value = runtime.Run(new LanguageExecutionRequest("41", backend)).Value;
if (!Equals(value, 42))
    throw new InvalidOperationException($"Expected 42, got {value}.");
Console.WriteLine("cross-package-consumer: 42");
''',
            encoding="utf-8",
        )
        run([
            args.dotnet,
            "restore",
            str(consumer / "CrossPackage.Consumer.csproj"),
            "--disable-parallel",
            "--disable-build-servers",
            "-p:NuGetAudit=false",
            *restore_args(config_file),
        ], env=env)
        run([
            args.dotnet,
            "run",
            "--project",
            str(consumer / "CrossPackage.Consumer.csproj"),
            "-c",
            "Release",
            "--no-restore",
            "--disable-build-servers",
        ], env=env)

    print("language-sdk-package-smoke: passed")


if __name__ == "__main__":
    main()
