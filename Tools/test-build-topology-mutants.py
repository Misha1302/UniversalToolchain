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


def remove_project_reference(path: Path, include_fragment: str) -> None:
    tree = ET.parse(path)
    parents = [element for element in tree.getroot().iter()]
    matches: list[tuple[ET.Element, ET.Element]] = []
    for parent in parents:
        for child in list(parent):
            if child.tag.rsplit("}", 1)[-1] == "ProjectReference" and \
               include_fragment in child.attrib.get("Include", ""):
                matches.append((parent, child))
    if len(matches) != 1:
        raise AssertionError(f"expected one ProjectReference containing {include_fragment!r} in {path}")
    matches[0][0].remove(matches[0][1])
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
            "emitter-build-order-copy-local",
            lambda path: mutate_project_reference(
                path / LANGUAGE_PACK_PROJECT,
                "$(FeatureManifestEmitterProjectPath)",
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
            "emitter-target-redirected",
            lambda path: set_msbuild_attribute(
                path / LANGUAGE_PACK_PROJECT,
                "ResolveLanguagePackBuildProviders",
                "$(FeatureManifestEmitterProjectPath)",
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
            "facade-language-pack-reference-removed",
            lambda path: remove_project_reference(
                path / WIST_PROJECT,
                "UniversalToolchain.Wist.LanguagePack.csproj",
            ),
            "facade must declare exactly one LanguagePack ProjectReference",
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
            "language-pack-reintroduces-wist-project-path",
            lambda path: add_top_level_property(
                path / LANGUAGE_PACK_PROJECT,
                "WistProjectPath",
                "$(MSBuildThisFileDirectory)..\\UniversalToolchain.Wist\\UniversalToolchain.Wist.csproj",
            ),
            "must not declare legacy WistProjectPath",
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
            "bash-retired-surface-check-disabled",
            lambda path: replace_once(
                path / "build.sh",
                "Tools/check-retired-surface.py",
                "Tools/check-retired-surface.disabled.py",
            ),
            "build.sh retired surface checker invocation",
        ),
        (
            "bash-retired-surface-mutants-disabled",
            lambda path: replace_once(
                path / "build.sh",
                "Tools/test-retired-surface-mutants.py",
                "Tools/test-retired-surface-mutants.disabled.py",
            ),
            "build.sh retired surface mutant invocation",
        ),
        (
            "powershell-retired-surface-check-disabled",
            lambda path: replace_once(
                path / "build.ps1",
                "Tools/check-retired-surface.py",
                "Tools/check-retired-surface.disabled.py",
            ),
            "build.ps1 retired surface checker invocation",
        ),
        (
            "powershell-retired-surface-mutants-disabled",
            lambda path: replace_once(
                path / "build.ps1",
                "Tools/test-retired-surface-mutants.py",
                "Tools/test-retired-surface-mutants.disabled.py",
            ),
            "build.ps1 retired surface mutant invocation",
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
