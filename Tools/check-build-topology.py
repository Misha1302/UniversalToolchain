#!/usr/bin/env python3
from __future__ import annotations

import argparse
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


class BuildTopologyError(ValueError):
    pass


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def project_xml_files(root: Path) -> list[Path]:
    toolchain = root / "UniversalToolchain"
    if not toolchain.is_dir():
        raise BuildTopologyError(f"toolchain directory does not exist: {toolchain}")
    return sorted(toolchain.rglob("*.csproj"))


def parse_project(path: Path) -> ET.Element:
    try:
        return ET.parse(path).getroot()
    except ET.ParseError as exc:
        raise BuildTopologyError(f"invalid project XML {path}: {exc}") from exc


def direct_children(root: ET.Element, name: str) -> list[ET.Element]:
    return [element for element in root if local_name(element.tag) == name]


def child_text(root: ET.Element, name: str) -> str | None:
    for element in root.iter():
        if local_name(element.tag) != name:
            continue
        text = (element.text or "").strip()
        if text:
            return text
    return None


def top_level_property_names(root: ET.Element) -> set[str]:
    names: set[str] = set()
    for group in direct_children(root, "PropertyGroup"):
        names.update(local_name(element.tag) for element in group)
    return names


def normalized_expression(value: str) -> str:
    return re.sub(r"\s+", "", value).lower()


def named_targets(root: ET.Element) -> dict[str, ET.Element]:
    return {
        element.attrib.get("Name", ""): element
        for element in root.iter()
        if local_name(element.tag) == "Target"
    }


def require_build_order_references_not_copy_local(root: Path) -> None:
    failures: list[str] = []
    for project in project_xml_files(root):
        project_root = parse_project(project)
        for element in project_root.iter():
            if local_name(element.tag) != "ProjectReference":
                continue
            if element.attrib.get("ReferenceOutputAssembly", "").lower() != "false":
                continue
            if element.attrib.get("Private", "").lower() == "false":
                continue
            include = element.attrib.get("Include", "<missing Include>")
            failures.append(f"{project.relative_to(root)} -> {include}")
    if failures:
        raise BuildTopologyError(
            "repository build-order ProjectReference items must set both "
            "ReferenceOutputAssembly=false and Private=false so their outputs are not "
            "treated as Copy Local payload: " + "; ".join(failures)
        )


def require_provider_target(
    project_root: ET.Element,
    *,
    target_name: str,
    expected_return: str,
    description: str,
) -> None:
    target = named_targets(project_root).get(target_name)
    if target is None:
        raise BuildTopologyError(f"{description} lacks {target_name}")
    if "Build" not in target.attrib.get("DependsOnTargets", "").split(";"):
        raise BuildTopologyError(f"{description} provider target must depend on Build")
    if target.attrib.get("Returns") != expected_return:
        raise BuildTopologyError(
            f"{description} provider target must return the evaluated {expected_return}"
        )


def require_language_pack_provider_contract(root: Path) -> None:
    language_pack = (
        root
        / "UniversalToolchain"
        / "UniversalToolchain.Wist.LanguagePack"
        / "UniversalToolchain.Wist.LanguagePack.csproj"
    )
    emitter = (
        root
        / "UniversalToolchain"
        / "UniversalToolchain.FeatureManifestEmitter"
        / "UniversalToolchain.FeatureManifestEmitter.csproj"
    )
    wist = (
        root
        / "UniversalToolchain"
        / "UniversalToolchain.Wist"
        / "UniversalToolchain.Wist.csproj"
    )
    for path in (language_pack, emitter, wist):
        if not path.is_file():
            raise BuildTopologyError(f"required project does not exist: {path}")

    language_pack_root = parse_project(language_pack)
    emitter_root = parse_project(emitter)
    wist_root = parse_project(wist)

    expected_paths = {
        "FeatureManifestEmitterProjectPath": (
            "$(MSBuildThisFileDirectory)..\\UniversalToolchain.FeatureManifestEmitter\\"
            "UniversalToolchain.FeatureManifestEmitter.csproj"
        ),
        "WistProjectPath": (
            "$(MSBuildThisFileDirectory)..\\UniversalToolchain.Wist\\"
            "UniversalToolchain.Wist.csproj"
        ),
    }
    for property_name, expected in expected_paths.items():
        if child_text(language_pack_root, property_name) != expected:
            raise BuildTopologyError(f"language pack has an unexpected {property_name}")

    prohibited_top_level_guesses = {
        "FeatureManifestEmitterTargetFramework",
        "FeatureManifestEmitterPlatformSegment",
        "FeatureManifestEmitterProjectBinDll",
        "FeatureManifestEmitterConfiguredOutputDll",
        "FeatureManifestEmitterTargetDirDll",
        "WistRuntimeOutputDirectory",
    }
    present_guesses = sorted(top_level_property_names(language_pack_root) & prohibited_top_level_guesses)
    if present_guesses:
        raise BuildTopologyError(
            "language pack must consume provider-returned paths instead of guessing output layout: "
            + ", ".join(present_guesses)
        )

    references = [
        element
        for element in language_pack_root.iter()
        if local_name(element.tag) == "ProjectReference"
    ]
    emitter_references = [
        element
        for element in references
        if element.attrib.get("Include") == "$(FeatureManifestEmitterProjectPath)"
    ]
    if len(emitter_references) != 1:
        raise BuildTopologyError("language pack must declare exactly one emitter ProjectReference")
    emitter_reference = emitter_references[0]
    if emitter_reference.attrib.get("ReferenceOutputAssembly", "").lower() != "false" or \
       emitter_reference.attrib.get("Private", "").lower() != "false":
        raise BuildTopologyError("emitter ProjectReference must be build-only and not Copy Local")

    wist_references = [
        element for element in references if element.attrib.get("Include") == "$(WistProjectPath)"
    ]
    if len(wist_references) != 1:
        raise BuildTopologyError("language pack must declare exactly one canonical Wist ProjectReference")

    require_provider_target(
        emitter_root,
        target_name="GetBuiltFeatureManifestEmitterTargetPath",
        expected_return="$(TargetPath)",
        description="feature manifest emitter",
    )
    require_provider_target(
        wist_root,
        target_name="GetBuiltWistOutputDirectory",
        expected_return="$(TargetDir)",
        description="Wist runtime",
    )

    targets = named_targets(language_pack_root)
    resolver = targets.get("ResolveLanguagePackBuildProviders")
    if resolver is None:
        raise BuildTopologyError("language pack lacks ResolveLanguagePackBuildProviders")
    required_before_targets = {
        "ResolveProjectReferences",
        "EmitToolchainFeatureManifest",
        "ExposeWistRuntimeClosureToProjectReferences",
        "CollectWistRuntimeManifests",
    }
    actual_before_targets = set(resolver.attrib.get("BeforeTargets", "").split(";"))
    if not required_before_targets.issubset(actual_before_targets):
        raise BuildTopologyError("language pack provider resolution is missing required BeforeTargets")
    expected_condition = normalized_expression("'$(DesignTimeBuild)' != 'true'")
    if normalized_expression(resolver.attrib.get("Condition", "")) != expected_condition:
        raise BuildTopologyError("language pack provider resolution must be disabled only for DesignTimeBuild=true")

    msbuild_tasks = direct_children(resolver, "MSBuild")
    if len(msbuild_tasks) != 2:
        raise BuildTopologyError("language pack provider resolver must contain exactly two direct MSBuild tasks")
    tasks_by_project = {task.attrib.get("Projects", ""): task for task in msbuild_tasks}
    expected_tasks = {
        "$(FeatureManifestEmitterProjectPath)": (
            "GetBuiltFeatureManifestEmitterTargetPath",
            "_FeatureManifestEmitterTargetPath",
        ),
        "$(WistProjectPath)": (
            "GetBuiltWistOutputDirectory",
            "_WistOutputDirectory",
        ),
    }
    if set(tasks_by_project) != set(expected_tasks):
        raise BuildTopologyError("language pack provider resolver invokes an unexpected project set")
    for project, (target_name, output_item) in expected_tasks.items():
        task = tasks_by_project[project]
        if task.attrib.get("Targets") != target_name:
            raise BuildTopologyError(f"provider resolver for {project} invokes the wrong target")
        if normalized_expression(task.attrib.get("Properties", "")) != "buildprojectreferences=true":
            raise BuildTopologyError(
                f"provider resolver for {project} must force only BuildProjectReferences=true"
            )
        outputs = direct_children(task, "Output")
        if len(outputs) != 1 or outputs[0].attrib.get("TaskParameter") != "TargetOutputs" or \
           outputs[0].attrib.get("ItemName") != output_item:
            raise BuildTopologyError(f"provider resolver for {project} must capture TargetOutputs")

    resolved_values = {
        local_name(element.tag): (element.text or "").strip()
        for group in direct_children(resolver, "PropertyGroup")
        for element in group
    }
    if resolved_values.get("FeatureManifestEmitterResolvedDll") != "@(_FeatureManifestEmitterTargetPath)":
        raise BuildTopologyError("emitter executable path must derive from captured TargetOutputs")
    if resolved_values.get("WistRuntimeOutputDirectory") != "@(_WistOutputDirectory)":
        raise BuildTopologyError("Wist runtime directory must derive from captured TargetOutputs")

    expected_error_conditions = {
        normalized_expression(
            "'$(FeatureManifestEmitterResolvedDll)' == '' or "
            "!Exists('$(FeatureManifestEmitterResolvedDll)')"
        ),
        normalized_expression(
            "'$(WistRuntimeOutputDirectory)' == '' or "
            "!Exists('$(WistRuntimeOutputDirectory)')"
        ),
    }
    actual_error_conditions = {
        normalized_expression(error.attrib.get("Condition", ""))
        for error in direct_children(resolver, "Error")
    }
    if actual_error_conditions != expected_error_conditions:
        raise BuildTopologyError("provider resolver must fail on empty or absent returned paths")

    emit_target = targets.get("EmitToolchainFeatureManifest")
    if emit_target is None:
        raise BuildTopologyError("language pack lacks EmitToolchainFeatureManifest")
    execs = direct_children(emit_target, "Exec")
    if len(execs) != 1 or "$(FeatureManifestEmitterResolvedDll)" not in execs[0].attrib.get("Command", ""):
        raise BuildTopologyError("feature manifest emission must execute the provider-returned TargetPath")

    expose_target = targets.get("ExposeWistRuntimeClosureToProjectReferences")
    if expose_target is None:
        raise BuildTopologyError("language pack lacks ExposeWistRuntimeClosureToProjectReferences")
    runtime_files = [
        element
        for element in expose_target.iter()
        if local_name(element.tag) == "WistRuntimeFileForCopy"
    ]
    expected_closure = (
        "$(WistRuntimeOutputDirectory)*.dll;"
        "$(WistRuntimeOutputDirectory)*.dialect.runtime.json"
    )
    if len(runtime_files) != 1 or runtime_files[0].attrib.get("Include") != expected_closure:
        raise BuildTopologyError("runtime closure must come from the provider-returned Wist output directory")
    assign_tasks = direct_children(expose_target, "AssignTargetPath")
    if len(assign_tasks) != 1 or assign_tasks[0].attrib.get("Files") != "@(WistRuntimeFileForCopy)" or \
       assign_tasks[0].attrib.get("RootFolder") != "$(WistRuntimeOutputDirectory)":
        raise BuildTopologyError("runtime closure must preserve file-relative target paths")
    assigned_outputs = direct_children(assign_tasks[0], "Output")
    if len(assigned_outputs) != 1 or assigned_outputs[0].attrib.get("TaskParameter") != "AssignedFiles" or \
       assigned_outputs[0].attrib.get("ItemName") != "ContentWithTargetPath":
        raise BuildTopologyError("runtime closure must publish AssignTargetPath outputs")

    collect_target = targets.get("CollectWistRuntimeManifests")
    if collect_target is None:
        raise BuildTopologyError("language pack lacks CollectWistRuntimeManifests")
    manifest_items = [
        element
        for element in collect_target.iter()
        if local_name(element.tag) == "WistRuntimeManifest"
    ]
    if len(manifest_items) != 1 or manifest_items[0].attrib.get("Include") != \
       "$(WistRuntimeOutputDirectory)*.dialect.runtime.json":
        raise BuildTopologyError("runtime manifests must come from the provider-returned Wist output directory")

    raw_language_pack = language_pack.read_text(encoding="utf-8-sig")
    if "UniversalToolchain.Wist\\$(OutputPath)" in raw_language_pack:
        raise BuildTopologyError("language pack still concatenates the Wist project path with $(OutputPath)")


def uncommented_text(path: Path) -> str:
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    return "\n".join(line for line in lines if not line.lstrip().startswith("#"))


def require_one(text: str, pattern: str, description: str) -> int:
    matches = list(re.finditer(pattern, text, flags=re.MULTILINE | re.DOTALL))
    if len(matches) != 1:
        raise BuildTopologyError(f"expected exactly one {description}, found {len(matches)}")
    return matches[0].start()


def require_canonical_gate_order(root: Path) -> None:
    runtime_test = root / "Tools" / "test-build-topology-runtime.py"
    if not runtime_test.is_file():
        raise BuildTopologyError(f"runtime topology regression does not exist: {runtime_test}")

    bash = uncommented_text(root / "build.sh")
    bash_static = require_one(
        bash,
        r"^\s*python3\s+Tools/check-build-topology\.py\b",
        "build.sh topology checker invocation",
    )
    bash_mutants = require_one(
        bash,
        r"^\s*python3\s+Tools/test-build-topology-mutants\.py\b",
        "build.sh topology mutant invocation",
    )
    bash_runtime = require_one(
        bash,
        r"^\s*python3\s+Tools/test-build-topology-runtime\.py\b",
        "build.sh runtime topology invocation",
    )
    bash_restore = bash.index('"$dotnet_command" restore')
    bash_build = bash.index('"$dotnet_command" build "$solution"')
    bash_tests = bash.index("python3 Tools/run-test-contract.py")
    if max(bash_static, bash_mutants) > bash_restore:
        raise BuildTopologyError("build.sh static topology gates must run before restore")
    if not bash_build < bash_runtime < bash_tests:
        raise BuildTopologyError("build.sh runtime topology regression must run after solution build and before tests")

    powershell = uncommented_text(root / "build.ps1")
    ps_static = require_one(
        powershell,
        r'Invoke-CheckedNative\s+"python"\s+@\("Tools/check-build-topology\.py"',
        "build.ps1 topology checker invocation",
    )
    ps_mutants = require_one(
        powershell,
        r'Invoke-CheckedNative\s+"python"\s+@\("Tools/test-build-topology-mutants\.py"',
        "build.ps1 topology mutant invocation",
    )
    ps_runtime = require_one(
        powershell,
        r'Invoke-CheckedNative\s+"python"\s+@\(\s*"Tools/test-build-topology-runtime\.py"',
        "build.ps1 runtime topology invocation",
    )
    ps_restore = powershell.index("Invoke-CheckedNative $dotnet (New-RestoreArguments $solution)")
    ps_build = powershell.index('"build", $solution')
    ps_tests = powershell.index('"Tools/run-test-contract.py"')
    if max(ps_static, ps_mutants) > ps_restore:
        raise BuildTopologyError("build.ps1 static topology gates must run before restore")
    if not ps_build < ps_runtime < ps_tests:
        raise BuildTopologyError("build.ps1 runtime topology regression must run after solution build and before tests")

    bash_pack = bash.index('if [[ "$skip_pack" == false ]]')
    ps_pack = powershell.index("if (-not $SkipPack)")
    release_invocations = (
        (
            "build.sh package metadata checker",
            bash,
            r"^\s*python3\s+Tools/package_metadata\.py\b",
            bash_pack,
        ),
        (
            "build.sh package metadata mutants",
            bash,
            r"^\s*python3\s+Tools/test-package-metadata-mutants\.py\b",
            bash_pack,
        ),
        (
            "build.ps1 package metadata checker",
            powershell,
            r'Invoke-CheckedNative\s+"python"\s+@\(\s*"Tools/package_metadata\.py"',
            ps_pack,
        ),
        (
            "build.ps1 package metadata mutants",
            powershell,
            r'Invoke-CheckedNative\s+"python"\s+@\("Tools/test-package-metadata-mutants\.py"',
            ps_pack,
        ),
    )
    for description, text, pattern, pack_start in release_invocations:
        if require_one(text, pattern, description) < pack_start:
            raise BuildTopologyError(f"{description} must remain inside the packaging gate")


def require_windows_powershell_gate(root: Path) -> None:
    workflow = root / ".github" / "workflows" / "dotnet-ci.yml"
    if not workflow.is_file():
        raise BuildTopologyError(f".NET CI workflow does not exist: {workflow}")
    text = workflow.read_text(encoding="utf-8-sig")
    pattern = (
        r"(?ms)^  windows-build-test:\s*$"
        r".*?^    runs-on:\s*windows-latest\s*$"
        r".*?^        shell:\s*pwsh\s*$"
        r".*?^        run:\s*\./build\.ps1 -SkipDocs -SkipPack\s*$"
    )
    require_one(text, pattern, "Windows canonical PowerShell CI job")


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate IDE-safe project references and canonical build topology gates."
    )
    parser.add_argument("--root", type=Path, default=Path.cwd())
    args = parser.parse_args()

    root = args.root.resolve()
    try:
        require_build_order_references_not_copy_local(root)
        require_language_pack_provider_contract(root)
        require_canonical_gate_order(root)
        require_windows_powershell_gate(root)
    except (BuildTopologyError, OSError, ValueError) as exc:
        print(f"BUILD_TOPOLOGY=FAIL: {exc}", file=sys.stderr)
        return 1

    print("BUILD_TOPOLOGY=PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
