#!/usr/bin/env python3
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from collections.abc import Callable
from pathlib import Path


DIALECT_TEST_PROJECT = Path(
    "UniversalToolchain/UniversalToolchain.Dialects.Tests/"
    "UniversalToolchain.Dialects.Tests.csproj"
)
FRESH_PROCESS_HOST_PROJECT = Path(
    "UniversalToolchain/UniversalToolchain.Dialects.Tests/FreshProcess/"
    "RuntimeSharedAssemblyFreshProcessHost/RuntimeSharedAssemblyFreshProcessHost.csproj"
)
LANGUAGE_PACK_PROJECT = Path(
    "UniversalToolchain/UniversalToolchain.Wist.LanguagePack/"
    "UniversalToolchain.Wist.LanguagePack.csproj"
)
EMITTER_PROJECT = Path(
    "UniversalToolchain/UniversalToolchain.FeatureManifestEmitter/"
    "UniversalToolchain.FeatureManifestEmitter.csproj"
)
WIST_PROJECT = Path(
    "UniversalToolchain/UniversalToolchain.Wist/UniversalToolchain.Wist.csproj"
)
DOTNET_WORKFLOW = Path(".github/workflows/dotnet-ci.yml")
SUPPORT_FILES = (
    Path("build.sh"),
    Path("build.ps1"),
    Path("Tools/test-build-topology-runtime.py"),
    DOTNET_WORKFLOW,
)


def run_checker(checker: Path, root: Path, expected_fragment: str | None = None) -> None:
    completed = subprocess.run(
        [sys.executable, str(checker), "--root", str(root)],
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
    )
    if expected_fragment is None:
        if completed.returncode != 0:
            raise AssertionError(f"valid build topology was rejected:\n{completed.stdout}")
        return
    if completed.returncode == 0 or expected_fragment not in completed.stdout:
        raise AssertionError(
            f"mutant was not rejected with {expected_fragment!r}:\n{completed.stdout}"
        )


def copy_inputs(source: Path, destination: Path) -> None:
    project_files = sorted((source / "UniversalToolchain").rglob("*.csproj"))
    if not project_files:
        raise AssertionError("no project files found for topology fixture")
    for source_path in [*(source / relative for relative in SUPPORT_FILES), *project_files]:
        relative = source_path.relative_to(source)
        target = destination / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source_path, target)


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8-sig")
    if old not in text:
        raise AssertionError(f"mutation anchor not found in {path}: {old!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8")


def find_target(tree: ET.ElementTree, target_name: str) -> ET.Element:
    matches = [
        element
        for element in tree.getroot().iter()
        if element.tag.rsplit("}", 1)[-1] == "Target"
        and element.attrib.get("Name") == target_name
    ]
    if len(matches) != 1:
        raise AssertionError(f"expected exactly one target {target_name!r}")
    return matches[0]


def mutate_project_reference(path: Path, include_fragment: str) -> None:
    tree = ET.parse(path)
    matches = [
        element
        for element in tree.getroot().iter()
        if element.tag.rsplit("}", 1)[-1] == "ProjectReference"
        and include_fragment in element.attrib.get("Include", "")
    ]
    if len(matches) != 1 or matches[0].attrib.get("Private", "").lower() != "false":
        raise AssertionError(f"expected one Private=false reference containing {include_fragment!r} in {path}")
    del matches[0].attrib["Private"]
    tree.write(path, encoding="unicode")


def rename_target(path: Path, old_name: str, new_name: str) -> None:
    tree = ET.parse(path)
    find_target(tree, old_name).set("Name", new_name)
    tree.write(path, encoding="unicode")


def set_target_attribute(path: Path, target_name: str, attribute: str, value: str) -> None:
    tree = ET.parse(path)
    find_target(tree, target_name).set(attribute, value)
    tree.write(path, encoding="unicode")


def find_msbuild_task(path: Path, target_name: str, project: str) -> tuple[ET.ElementTree, ET.Element]:
    tree = ET.parse(path)
    target = find_target(tree, target_name)
    tasks = [
        element
        for element in target
        if element.tag.rsplit("}", 1)[-1] == "MSBuild"
        and element.attrib.get("Projects") == project
    ]
    if len(tasks) != 1:
        raise AssertionError(f"expected one MSBuild task for {project!r} in {target_name!r}")
    return tree, tasks[0]


def set_msbuild_attribute(
    path: Path,
    target_name: str,
    project: str,
    attribute: str,
    value: str,
) -> None:
    tree, task = find_msbuild_task(path, target_name, project)
    task.set(attribute, value)
    tree.write(path, encoding="unicode")


def remove_msbuild_output(path: Path, target_name: str, project: str) -> None:
    tree, task = find_msbuild_task(path, target_name, project)
    outputs = [element for element in task if element.tag.rsplit("}", 1)[-1] == "Output"]
    if len(outputs) != 1:
        raise AssertionError(f"expected one MSBuild Output for {project!r} in {target_name!r}")
    task.remove(outputs[0])
    tree.write(path, encoding="unicode")


def add_top_level_property(path: Path, name: str, value: str) -> None:
    tree = ET.parse(path)
    property_groups = [
        element
        for element in tree.getroot()
        if element.tag.rsplit("}", 1)[-1] == "PropertyGroup"
    ]
    if not property_groups:
        raise AssertionError("project has no top-level PropertyGroup")
    element = ET.SubElement(property_groups[0], name)
    element.text = value
    tree.write(path, encoding="unicode")


def fixture(root: Path, checker: Path) -> tuple[tempfile.TemporaryDirectory[str], Path]:
    temporary = tempfile.TemporaryDirectory(prefix="wist-build-topology-")
    path = Path(temporary.name)
    copy_inputs(root, path)
    run_checker(checker, path)
    return temporary, path


def main() -> int:
    parser = argparse.ArgumentParser(description="Exercise negative build-topology mutants.")
    parser.add_argument("--root", type=Path, default=Path.cwd())
    args = parser.parse_args()

    root = args.root.resolve()
    checker = root / "Tools" / "check-build-topology.py"
    if not checker.is_file():
        print(f"BUILD_TOPOLOGY_MUTANTS=FAIL: checker does not exist: {checker}", file=sys.stderr)
        return 1

    Mutation = tuple[str, Callable[[Path], None], str]
    mutations: tuple[Mutation, ...] = (
        (
            "dialect-build-order-copy-local",
            lambda path: mutate_project_reference(
                path / DIALECT_TEST_PROJECT,
                "RuntimeSharedAssemblyFreshProcessHost.csproj",
            ),
            "repository build-order ProjectReference",
        ),
        (
            "fixture-build-order-copy-local",
            lambda path: mutate_project_reference(
                path / FRESH_PROCESS_HOST_PROJECT,
                "CanonicalRuntimeFixture.csproj",
            ),
            "repository build-order ProjectReference",
        ),
        (
            "provider-resolver-renamed",
            lambda path: rename_target(
                path / LANGUAGE_PACK_PROJECT,
                "ResolveLanguagePackBuildProviders",
                "DisabledResolveLanguagePackBuildProviders",
            ),
            "lacks ResolveLanguagePackBuildProviders",
        ),
        (
            "emitter-project-redirected",
            lambda path: set_msbuild_attribute(
                path / LANGUAGE_PACK_PROJECT,
                "ResolveLanguagePackBuildProviders",
                "$(FeatureManifestEmitterProjectPath)",
                "Projects",
                "wrong-emitter.csproj",
            ),
            "unexpected project set",
        ),
        (
            "wist-provider-target-redirected",
            lambda path: set_msbuild_attribute(
                path / LANGUAGE_PACK_PROJECT,
                "ResolveLanguagePackBuildProviders",
                "$(WistProjectPath)",
                "Targets",
                "Build",
            ),
            "invokes the wrong target",
        ),
        (
            "emitter-output-not-captured",
            lambda path: remove_msbuild_output(
                path / LANGUAGE_PACK_PROJECT,
                "ResolveLanguagePackBuildProviders",
                "$(FeatureManifestEmitterProjectPath)",
            ),
            "capture TargetOutputs",
        ),
        (
            "resolver-forwards-evaluated-output-path",
            lambda path: set_msbuild_attribute(
                path / LANGUAGE_PACK_PROJECT,
                "ResolveLanguagePackBuildProviders",
                "$(FeatureManifestEmitterProjectPath)",
                "Properties",
                "BuildProjectReferences=true;OutputPath=$(OutputPath)",
            ),
            "force only BuildProjectReferences=true",
        ),
        (
            "emitter-returns-guessed-path",
            lambda path: set_target_attribute(
                path / EMITTER_PROJECT,
                "GetBuiltFeatureManifestEmitterTargetPath",
                "Returns",
                "$(TargetDir)guessed.dll",
            ),
            "return the evaluated $(TargetPath)",
        ),
        (
            "wist-returns-guessed-directory",
            lambda path: set_target_attribute(
                path / WIST_PROJECT,
                "GetBuiltWistOutputDirectory",
                "Returns",
                "$(MSBuildProjectDirectory)\\bin\\Release\\net10.0\\",
            ),
            "return the evaluated $(TargetDir)",
        ),
        (
            "language-pack-reintroduces-emitter-layout-guess",
            lambda path: add_top_level_property(
                path / LANGUAGE_PACK_PROJECT,
                "FeatureManifestEmitterProjectBinDll",
                "$(MSBuildThisFileDirectory)..\\Emitter\\bin\\Release\\net10.0\\Emitter.dll",
            ),
            "instead of guessing output layout",
        ),
        (
            "language-pack-reintroduces-wist-layout-guess",
            lambda path: add_top_level_property(
                path / LANGUAGE_PACK_PROJECT,
                "WistRuntimeOutputDirectory",
                "$(MSBuildThisFileDirectory)..\\UniversalToolchain.Wist\\$(OutputPath)",
            ),
            "instead of guessing output layout",
        ),
        (
            "runtime-target-path-root-redirected",
            lambda path: replace_once(
                path / LANGUAGE_PACK_PROJECT,
                'RootFolder="$(WistRuntimeOutputDirectory)"',
                'RootFolder="$(TargetDir)"',
            ),
            "preserve file-relative target paths",
        ),
        (
            "powershell-package-gate-disabled",
            lambda path: replace_once(
                path / "build.ps1",
                '"Tools/package_metadata.py"',
                '"Tools/package_metadata.disabled.py"',
            ),
            "build.ps1 package metadata checker",
        ),
        (
            "bash-package-mutants-disabled",
            lambda path: replace_once(
                path / "build.sh",
                "Tools/test-package-metadata-mutants.py",
                "Tools/test-package-metadata-mutants.disabled.py",
            ),
            "build.sh package metadata mutants",
        ),
        (
            "bash-runtime-topology-disabled",
            lambda path: replace_once(
                path / "build.sh",
                "Tools/test-build-topology-runtime.py",
                "Tools/test-build-topology-runtime.disabled.py",
            ),
            "build.sh runtime topology invocation",
        ),
        (
            "windows-powershell-job-disabled",
            lambda path: replace_once(
                path / DOTNET_WORKFLOW,
                "run: ./build.ps1 -SkipDocs -SkipPack",
                "run: Write-Host 'PowerShell build disabled'",
            ),
            "Windows canonical PowerShell CI job",
        ),
    )

    try:
        for name, mutate, expected in mutations:
            temporary, path = fixture(root, checker)
            with temporary:
                mutate(path)
                run_checker(checker, path, expected)
                print(f"SURVIVOR=0 mutant={name}")
    except (AssertionError, OSError) as exc:
        print(f"BUILD_TOPOLOGY_MUTANTS=FAIL: {exc}", file=sys.stderr)
        return 1

    print("BUILD_TOPOLOGY_MUTANTS=PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
