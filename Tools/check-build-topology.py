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


def child_text(root: ET.Element, name: str) -> str | None:
    for element in root.iter():
        if local_name(element.tag) != name:
            continue
        text = (element.text or "").strip()
        if text:
            return text
    return None


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


def require_language_pack_isolated_emitter_build(root: Path) -> None:
    project = (
        root
        / "UniversalToolchain"
        / "UniversalToolchain.Wist.LanguagePack"
        / "UniversalToolchain.Wist.LanguagePack.csproj"
    )
    if not project.is_file():
        raise BuildTopologyError(f"language-pack project does not exist: {project}")
    project_root = parse_project(project)

    emitter_property = child_text(project_root, "FeatureManifestEmitterProjectPath")
    if emitter_property != "$(MSBuildThisFileDirectory)..\\UniversalToolchain.FeatureManifestEmitter\\UniversalToolchain.FeatureManifestEmitter.csproj":
        raise BuildTopologyError("language pack has an unexpected FeatureManifestEmitterProjectPath")

    emitter_references = [
        element
        for element in project_root.iter()
        if local_name(element.tag) == "ProjectReference"
        and element.attrib.get("Include") == "$(FeatureManifestEmitterProjectPath)"
    ]
    if len(emitter_references) != 1:
        raise BuildTopologyError("language pack must declare exactly one emitter ProjectReference")
    emitter_reference = emitter_references[0]
    if emitter_reference.attrib.get("ReferenceOutputAssembly", "").lower() != "false" or \
       emitter_reference.attrib.get("Private", "").lower() != "false":
        raise BuildTopologyError("emitter ProjectReference must be build-only and not Copy Local")

    targets = {
        element.attrib.get("Name", ""): element
        for element in project_root.iter()
        if local_name(element.tag) == "Target"
    }
    target = targets.get("BuildFeatureManifestEmitterForIsolatedBuild")
    if target is None:
        raise BuildTopologyError("language pack lacks BuildFeatureManifestEmitterForIsolatedBuild")
    if "EmitToolchainFeatureManifest" not in target.attrib.get("BeforeTargets", "").split(";"):
        raise BuildTopologyError("isolated emitter build must run before EmitToolchainFeatureManifest")
    condition = target.attrib.get("Condition", "")
    if "$(BuildProjectReferences)" not in condition or "false" not in condition.lower():
        raise BuildTopologyError("isolated emitter build must be gated by BuildProjectReferences=false")

    msbuild_tasks = [element for element in target if local_name(element.tag) == "MSBuild"]
    if len(msbuild_tasks) != 1:
        raise BuildTopologyError("isolated emitter target must contain exactly one MSBuild task")
    msbuild = msbuild_tasks[0]
    if msbuild.attrib.get("Projects") != "$(FeatureManifestEmitterProjectPath)":
        raise BuildTopologyError("isolated emitter target builds the wrong project")
    if "Build" not in msbuild.attrib.get("Targets", "").split(";"):
        raise BuildTopologyError("isolated emitter target must invoke Build")
    if "Configuration=$(Configuration)" not in msbuild.attrib.get("Properties", ""):
        raise BuildTopologyError("isolated emitter build must preserve Configuration")

    emit_target = targets.get("EmitToolchainFeatureManifest")
    if emit_target is None:
        raise BuildTopologyError("language pack lacks EmitToolchainFeatureManifest")
    errors = [element for element in emit_target if local_name(element.tag) == "Error"]
    execs = [element for element in emit_target if local_name(element.tag) == "Exec"]
    if not errors or "FeatureManifestEmitterResolvedDll" not in errors[0].attrib.get("Condition", ""):
        raise BuildTopologyError("feature manifest emission must fail clearly when the emitter is absent")
    if len(execs) != 1 or "$(FeatureManifestEmitterResolvedDll)" not in execs[0].attrib.get("Command", ""):
        raise BuildTopologyError("feature manifest emission must execute the resolved emitter path")


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
    bash_static = require_one(bash, r"^\s*python3\s+Tools/check-build-topology\.py\b", "build.sh topology checker invocation")
    bash_mutants = require_one(bash, r"^\s*python3\s+Tools/test-build-topology-mutants\.py\b", "build.sh topology mutant invocation")
    bash_runtime = require_one(bash, r"^\s*python3\s+Tools/test-build-topology-runtime\.py\b", "build.sh runtime topology invocation")
    bash_restore = bash.index('"$dotnet_command" restore')
    bash_build = bash.index('"$dotnet_command" build "$solution"')
    bash_tests = bash.index("python3 Tools/run-test-contract.py")
    if max(bash_static, bash_mutants) > bash_restore:
        raise BuildTopologyError("build.sh static topology gates must run before restore")
    if not bash_build < bash_runtime < bash_tests:
        raise BuildTopologyError("build.sh runtime topology regression must run after solution build and before tests")

    powershell = uncommented_text(root / "build.ps1")
    ps_static = require_one(powershell, r'Invoke-CheckedNative\s+"python"\s+@\("Tools/check-build-topology\.py"', "build.ps1 topology checker invocation")
    ps_mutants = require_one(powershell, r'Invoke-CheckedNative\s+"python"\s+@\("Tools/test-build-topology-mutants\.py"', "build.ps1 topology mutant invocation")
    ps_runtime = require_one(powershell, r'Invoke-CheckedNative\s+"python"\s+@\(\s*"Tools/test-build-topology-runtime\.py"', "build.ps1 runtime topology invocation")
    ps_restore = powershell.index("Invoke-CheckedNative $dotnet (New-RestoreArguments $solution)")
    ps_build = powershell.index('"build", $solution')
    ps_tests = powershell.index('"Tools/run-test-contract.py"')
    if max(ps_static, ps_mutants) > ps_restore:
        raise BuildTopologyError("build.ps1 static topology gates must run before restore")
    if not ps_build < ps_runtime < ps_tests:
        raise BuildTopologyError("build.ps1 runtime topology regression must run after solution build and before tests")

    required_release_gates = (
        "Tools/package_metadata.py",
        "Tools/test-package-metadata-mutants.py",
    )
    for relative, text in (("build.sh", bash), ("build.ps1", powershell)):
        missing = [gate for gate in required_release_gates if gate not in text]
        if missing:
            raise BuildTopologyError(f"{relative} omits mandatory package metadata gates: {missing}")


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate IDE-safe project references and canonical build topology gates."
    )
    parser.add_argument("--root", type=Path, default=Path.cwd())
    args = parser.parse_args()

    root = args.root.resolve()
    try:
        require_build_order_references_not_copy_local(root)
        require_language_pack_isolated_emitter_build(root)
        require_canonical_gate_order(root)
    except (BuildTopologyError, OSError, ValueError) as exc:
        print(f"BUILD_TOPOLOGY=FAIL: {exc}", file=sys.stderr)
        return 1

    print("BUILD_TOPOLOGY=PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
