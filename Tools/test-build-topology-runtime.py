#!/usr/bin/env python3
from __future__ import annotations

import argparse
import os
import platform
import re
import shutil
import subprocess
import sys
import tempfile
from dataclasses import dataclass
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
WIST = Path("UniversalToolchain/UniversalToolchain.Wist")
CORE_TESTS = Path("UniversalToolchain/Tests/Tests.csproj")
WISTC = Path("UniversalToolchain/Wistc")
DOTNET_CI = Path(".github/workflows/dotnet-ci.yml")
CI_AGGREGATE = Path(".github/workflows/ci-aggregate.yml")


@dataclass(frozen=True)
class LayoutCase:
    name: str
    properties: tuple[str, ...] = ()
    external_output_root: Path | None = None
    requires_restore: bool = False
    build_project_references: bool = False


def remove_configuration_outputs(project_directory: Path, configuration: str) -> None:
    configuration_key = configuration.casefold()
    for output_root_name in ("bin", "obj"):
        output_root = project_directory / output_root_name
        if not output_root.is_dir():
            continue
        candidates = sorted(
            (
                path
                for path in output_root.rglob("*")
                if path.is_dir() and path.name.casefold() == configuration_key
            ),
            key=lambda path: len(path.parts),
            reverse=True,
        )
        for candidate in candidates:
            shutil.rmtree(candidate)


def verify_cleanup_scope() -> None:
    with tempfile.TemporaryDirectory(prefix="wist-topology-cleanup-") as temporary:
        project = Path(temporary) / "project"
        debug_sentinels = (
            project / "bin" / "Debug" / "sentinel.txt",
            project / "bin" / "x64" / "Debug" / "sentinel.txt",
            project / "obj" / "Debug" / "sentinel.txt",
            project / "obj" / "x64" / "Debug" / "sentinel.txt",
        )
        release_sentinels = (
            project / "bin" / "Release" / "delete.txt",
            project / "bin" / "x64" / "Release" / "delete.txt",
            project / "obj" / "Release" / "delete.txt",
            project / "obj" / "x64" / "Release" / "delete.txt",
        )
        for sentinel in (*debug_sentinels, *release_sentinels):
            sentinel.parent.mkdir(parents=True, exist_ok=True)
            sentinel.write_text("sentinel", encoding="utf-8")

        remove_configuration_outputs(project, "Release")

        missing_debug = [str(path) for path in debug_sentinels if not path.is_file()]
        surviving_release = [str(path) for path in release_sentinels if path.exists()]
        if missing_debug or surviving_release:
            raise RuntimeTopologyError(
                "configuration-scoped cleanup changed protected outputs: "
                f"missing_debug={missing_debug}, surviving_release={surviving_release}"
            )


def verify_ci_contract(root: Path) -> None:
    dotnet_ci = (root / DOTNET_CI).read_text(encoding="utf-8-sig")
    if not re.search(r"(?m)^  workflow_dispatch:\s*$", dotnet_ci):
        raise RuntimeTopologyError(".NET CI must expose workflow_dispatch")
    if re.search(
        r"(?m)^    if:\s*github\.event_name\s*!=\s*['\"]workflow_dispatch['\"]\s*$",
        dotnet_ci,
    ):
        raise RuntimeTopologyError(
            ".NET CI canonical jobs must execute, not skip, under workflow_dispatch"
        )

    aggregate = (root / CI_AGGREGATE).read_text(encoding="utf-8-sig")
    required_block = re.search(
        r"const requiredWorkflows = new Set\(\[(.*?)\]\);",
        aggregate,
        flags=re.DOTALL,
    )
    if required_block is None:
        raise RuntimeTopologyError("CI aggregate lacks the requiredWorkflows contract")
    if "Deploy documentation to GitHub Pages" in required_block.group(1):
        raise RuntimeTopologyError(
            "CI aggregate must not make the external GitHub Pages deployment a code-verification gate"
        )

    required_markers = (
        "const requiredRuns = runs.filter(",
        "requiredWorkflows.has(run.name)",
        "const names = new Set(requiredRuns.map(run => run.name));",
        "const active = requiredRuns.filter(run => run.status !== 'completed');",
        "const completedIds = requiredRuns.map(run => run.id)",
        "const failed = requiredRuns.filter(run => !acceptableConclusions.has(run.conclusion));",
        "const summary = requiredRuns.map(run =>",
    )
    missing = [marker for marker in required_markers if marker not in aggregate]
    if missing:
        raise RuntimeTopologyError(
            "CI aggregate must evaluate only explicitly required workflows; missing markers: "
            + ", ".join(missing)
        )


def build_server_arguments() -> list[str]:
    disabled = (
        os.environ.get("DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER") == "1"
        or os.environ.get("MSBUILDDISABLENODEREUSE") == "1"
    )
    return ["--disable-build-servers", "-p:UseSharedCompilation=false"] if disabled else []


def run_dotnet(
    command: list[str],
    *,
    root: Path,
    description: str,
    timeout: int = 420,
) -> str:
    completed = subprocess.run(
        command,
        cwd=root,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
        timeout=timeout,
    )
    if completed.returncode != 0:
        raise RuntimeTopologyError(f"{description} failed:\n{completed.stdout}")
    return completed.stdout


def run_restore(
    dotnet: str,
    root: Path,
    project: Path,
    properties: tuple[str, ...],
) -> None:
    run_dotnet(
        [
            dotnet,
            "restore",
            str(root / project),
            *build_server_arguments(),
            *properties,
            "-p:NuGetAudit=false",
        ],
        root=root,
        description=f"restore for {project} with properties {properties}",
    )


def run_build(
    dotnet: str,
    root: Path,
    project: Path,
    configuration: str,
    *,
    build_project_references: bool,
    properties: tuple[str, ...] = (),
) -> str:
    return run_dotnet(
        [
            dotnet,
            "build",
            str(root / project),
            "-c",
            configuration,
            "--no-restore",
            "-m:1",
            f"-p:BuildProjectReferences={'true' if build_project_references else 'false'}",
            *build_server_arguments(),
            *properties,
            "-p:NuGetAudit=false",
        ],
        root=root,
        description=(
            f"isolated build for {project} "
            f"(BuildProjectReferences={build_project_references}, properties={properties})"
        ),
    )


def verify_language_pack_design_time(
    dotnet: str,
    root: Path,
    configuration: str,
) -> None:
    run_dotnet(
        [
            dotnet,
            "msbuild",
            str(root / LANGUAGE_PACK),
            "-t:_GetCopyToOutputDirectoryItemsFromThisProject",
            f"-p:Configuration={configuration}",
            "-p:DesignTimeBuild=true",
            "-p:BuildProjectReferences=false",
            "-p:NuGetAudit=false",
        ],
        root=root,
        description="LanguagePack design-time copy-item evaluation",
        timeout=180,
    )


def matching_outputs(
    search_roots: tuple[Path, ...],
    pattern: str,
    *,
    configuration: str | None = None,
) -> list[Path]:
    matches: set[Path] = set()
    for search_root in search_roots:
        if not search_root.is_dir():
            continue
        for path in search_root.rglob(pattern):
            if not path.is_file():
                continue
            if configuration is not None and configuration.lower() not in {
                part.lower() for part in path.parts
            }:
                continue
            matches.add(path.resolve())
    return sorted(matches)


def require_output(
    search_roots: tuple[Path, ...],
    pattern: str,
    case_name: str,
    *,
    configuration: str | None = None,
) -> Path:
    matches = matching_outputs(search_roots, pattern, configuration=configuration)
    if not matches:
        raise RuntimeTopologyError(
            f"expected {pattern} for {case_name} under: "
            + ", ".join(str(path) for path in search_roots)
        )
    return matches[0]


def runtime_identifier() -> str:
    system = platform.system().lower()
    machine = platform.machine().lower()
    architecture = "arm64" if machine in {"arm64", "aarch64"} else "x64"
    if system == "windows":
        return f"win-{architecture}"
    if system == "darwin":
        return f"osx-{architecture}"
    return f"linux-{architecture}"


def language_pack_cases(temporary_root: Path) -> tuple[LayoutCase, ...]:
    output_path = temporary_root / "output-path"
    base_output_path = temporary_root / "base-output-path"
    artifacts_path = temporary_root / "artifacts-output"
    return (
        LayoutCase(
            "platform",
            ("-p:Platform=x64",),
            build_project_references=True,
        ),
        LayoutCase(
            "output-path",
            (f"-p:OutputPath={output_path}{os.sep}",),
            external_output_root=output_path,
            build_project_references=True,
        ),
        LayoutCase(
            "base-output-path",
            (f"-p:BaseOutputPath={base_output_path}{os.sep}",),
            external_output_root=base_output_path,
            build_project_references=True,
        ),
        LayoutCase(
            "runtime-identifier",
            (f"-p:RuntimeIdentifier={runtime_identifier()}",),
            requires_restore=True,
            build_project_references=True,
        ),
        LayoutCase(
            "no-target-framework-segment",
            ("-p:AppendTargetFrameworkToOutputPath=false",),
            build_project_references=True,
        ),
        LayoutCase(
            "artifacts-output",
            (
                "-p:UseArtifactsOutput=true",
                f"-p:ArtifactsPath={artifacts_path}",
            ),
            external_output_root=artifacts_path,
            requires_restore=True,
            build_project_references=True,
        ),
        # The canonical layout remains the dedicated IDE-style regression where
        # project references were built by the preceding solution build.
        LayoutCase("default", requires_restore=True),
    )


def verify_language_pack_layouts(
    dotnet: str,
    root: Path,
    configuration: str,
) -> None:
    language_pack_directory = (root / LANGUAGE_PACK).parent
    emitter_directory = root / FEATURE_EMITTER
    wist_directory = root / WIST

    with tempfile.TemporaryDirectory(prefix="wist-provider-layout-") as temporary:
        temporary_root = Path(temporary).resolve()
        for case in language_pack_cases(temporary_root):
            for project_directory in (
                language_pack_directory,
                emitter_directory,
                wist_directory,
            ):
                remove_configuration_outputs(project_directory, configuration)
            if case.external_output_root is not None:
                shutil.rmtree(case.external_output_root, ignore_errors=True)
            if case.requires_restore:
                run_restore(dotnet, root, LANGUAGE_PACK, case.properties)

            run_build(
                dotnet,
                root,
                LANGUAGE_PACK,
                configuration,
                build_project_references=case.build_project_references,
                properties=case.properties,
            )

            if case.external_output_root is None:
                language_pack_roots = (language_pack_directory,)
                emitter_roots = (emitter_directory,)
                wist_roots = (wist_directory,)
            else:
                language_pack_roots = (case.external_output_root,)
                emitter_roots = (case.external_output_root,)
                wist_roots = (case.external_output_root,)

            feature_manifest = require_output(
                language_pack_roots,
                "UniversalToolchain.Wist.LanguagePack.toolchain.feature.json",
                case.name,
            )
            emitter = require_output(
                emitter_roots,
                "UniversalToolchain.FeatureManifestEmitter.dll",
                case.name,
            )
            wist = require_output(
                wist_roots,
                "UniversalToolchain.Wist.dll",
                case.name,
            )
            runtime_manifest = require_output(
                (feature_manifest.parent,),
                "*.dialect.runtime.json",
                f"{case.name} language-pack runtime closure",
            )
            for artifact in (feature_manifest, emitter, wist, runtime_manifest):
                if artifact.stat().st_size == 0:
                    raise RuntimeTopologyError(
                        f"layout case {case.name} produced an empty artifact: {artifact}"
                    )


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Reproduce IDE-style and non-default output-layout builds against real projects."
    )
    parser.add_argument("--root", type=Path, default=Path.cwd())
    parser.add_argument("--dotnet", default="dotnet")
    parser.add_argument("--configuration", default="Release")
    args = parser.parse_args()

    root = args.root.resolve()
    configuration = args.configuration
    try:
        verify_cleanup_scope()
        verify_ci_contract(root)
        verify_language_pack_design_time(args.dotnet, root, configuration)

        dialect_directory = (root / DIALECT_TESTS).parent
        remove_configuration_outputs(dialect_directory, configuration)
        for relative in FRESH_PROCESS_PROJECTS:
            remove_configuration_outputs(root / relative, configuration)
        run_build(args.dotnet, root, DIALECT_TESTS, configuration, build_project_references=False)
        require_output(
            (root / FRESH_PROCESS_PROJECTS[-1],),
            "UniversalToolchain.Dialects.FreshProcessHost.dll",
            "dialect fresh-process host",
            configuration=configuration,
        )

        verify_language_pack_layouts(args.dotnet, root, configuration)

        tests_directory = (root / CORE_TESTS).parent
        remove_configuration_outputs(tests_directory, configuration)
        remove_configuration_outputs(root / WISTC, configuration)
        run_build(args.dotnet, root, CORE_TESTS, configuration, build_project_references=False)
        require_output(
            (tests_directory,),
            "Tests.dll",
            "core tests",
            configuration=configuration,
        )
        if matching_outputs(
            (root / WISTC,),
            "Wistc.dll",
            configuration=configuration,
        ):
            raise RuntimeTopologyError(
                "BuildProjectReferences=false unexpectedly rebuilt the build-only Wistc reference"
            )
        run_build(
            args.dotnet,
            root,
            WISTC / "Wistc.csproj",
            configuration,
            build_project_references=True,
        )
    except (OSError, subprocess.SubprocessError, RuntimeTopologyError) as exc:
        print(f"BUILD_TOPOLOGY_RUNTIME=FAIL: {exc}", file=sys.stderr)
        return 1

    print("BUILD_TOPOLOGY_RUNTIME=PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
