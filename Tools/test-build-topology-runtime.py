#!/usr/bin/env python3
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
from pathlib import Path


class RuntimeTopologyError(RuntimeError):
    pass


DIALECT_TESTS = Path("UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj")
FRESH_PROCESS_PROJECTS = (
    Path("UniversalToolchain/UniversalToolchain.Dialects.Tests/FreshProcess/HostOnlyContractFixture"),
    Path("UniversalToolchain/UniversalToolchain.Dialects.Tests/FreshProcess/HostileRuntimeFixture"),
    Path("UniversalToolchain/UniversalToolchain.Dialects.Tests/FreshProcess/CanonicalRuntimeFixture"),
    Path("UniversalToolchain/UniversalToolchain.Dialects.Tests/FreshProcess/UnregisteredDependencyRuntimeFixture"),
    Path("UniversalToolchain/UniversalToolchain.Dialects.Tests/FreshProcess/RuntimeSharedAssemblyFreshProcessHost"),
)
LANGUAGE_PACK = Path("UniversalToolchain/UniversalToolchain.Wist.LanguagePack/UniversalToolchain.Wist.LanguagePack.csproj")
FEATURE_EMITTER = Path("UniversalToolchain/UniversalToolchain.FeatureManifestEmitter")
CORE_TESTS = Path("UniversalToolchain/Tests/Tests.csproj")
WISTC = Path("UniversalToolchain/Wistc")


def remove_configuration_outputs(project_directory: Path, configuration: str) -> None:
    for output_root_name in ("bin", "obj"):
        output_root = project_directory / output_root_name
        if not output_root.is_dir():
            continue
        candidates = sorted(
            (path for path in output_root.rglob(configuration) if path.is_dir()),
            key=lambda path: len(path.parts),
            reverse=True,
        )
        for candidate in candidates:
            shutil.rmtree(candidate)


def run_build(
    dotnet: str,
    root: Path,
    project: Path,
    configuration: str,
    *,
    build_project_references: bool,
) -> None:
    command = [
        dotnet,
        "build",
        str(root / project),
        "-c",
        configuration,
        "--no-restore",
        "-m:1",
        f"-p:BuildProjectReferences={'true' if build_project_references else 'false'}",
        "-p:NuGetAudit=false",
    ]
    completed = subprocess.run(
        command,
        cwd=root,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
        timeout=240,
    )
    if completed.returncode != 0:
        raise RuntimeTopologyError(
            f"isolated build failed for {project} (BuildProjectReferences={build_project_references}):\n"
            f"{completed.stdout}"
        )


def matching_outputs(project_directory: Path, configuration: str, file_name: str) -> list[Path]:
    output_root = project_directory / "bin"
    if not output_root.is_dir():
        return []
    return sorted(
        path
        for path in output_root.rglob(file_name)
        if configuration.lower() in (part.lower() for part in path.parts)
    )


def require_output(project_directory: Path, configuration: str, file_name: str) -> None:
    matches = matching_outputs(project_directory, configuration, file_name)
    if not matches:
        raise RuntimeTopologyError(
            f"expected {file_name} under {project_directory}/bin for {configuration}"
        )


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Reproduce IDE-style BuildProjectReferences=false builds against real projects."
    )
    parser.add_argument("--root", type=Path, default=Path.cwd())
    parser.add_argument("--dotnet", default="dotnet")
    parser.add_argument("--configuration", default="Release")
    args = parser.parse_args()

    root = args.root.resolve()
    configuration = args.configuration
    try:
        dialect_directory = (root / DIALECT_TESTS).parent
        remove_configuration_outputs(dialect_directory, configuration)
        for relative in FRESH_PROCESS_PROJECTS:
            remove_configuration_outputs(root / relative, configuration)
        run_build(args.dotnet, root, DIALECT_TESTS, configuration, build_project_references=False)
        require_output(
            root / FRESH_PROCESS_PROJECTS[-1],
            configuration,
            "UniversalToolchain.Dialects.FreshProcessHost.dll",
        )

        language_pack_directory = (root / LANGUAGE_PACK).parent
        remove_configuration_outputs(language_pack_directory, configuration)
        remove_configuration_outputs(root / FEATURE_EMITTER, configuration)
        run_build(args.dotnet, root, LANGUAGE_PACK, configuration, build_project_references=False)
        require_output(
            language_pack_directory,
            configuration,
            "UniversalToolchain.Wist.LanguagePack.toolchain.feature.json",
        )
        require_output(
            root / FEATURE_EMITTER,
            configuration,
            "UniversalToolchain.FeatureManifestEmitter.dll",
        )

        tests_directory = (root / CORE_TESTS).parent
        remove_configuration_outputs(tests_directory, configuration)
        remove_configuration_outputs(root / WISTC, configuration)
        run_build(args.dotnet, root, CORE_TESTS, configuration, build_project_references=False)
        require_output(tests_directory, configuration, "Tests.dll")
        if matching_outputs(root / WISTC, configuration, "Wistc.dll"):
            raise RuntimeTopologyError(
                "BuildProjectReferences=false unexpectedly rebuilt the build-only Wistc reference"
            )
        run_build(args.dotnet, root, WISTC / "Wistc.csproj", configuration, build_project_references=True)
    except (OSError, subprocess.SubprocessError, RuntimeTopologyError) as exc:
        print(f"BUILD_TOPOLOGY_RUNTIME=FAIL: {exc}", file=sys.stderr)
        return 1

    print("BUILD_TOPOLOGY_RUNTIME=PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
