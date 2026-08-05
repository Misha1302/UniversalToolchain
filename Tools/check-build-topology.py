#!/usr/bin/env python3
from __future__ import annotations

import argparse
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


def require_build_order_references_not_copy_local(root: Path) -> None:
    failures: list[str] = []
    for project in project_xml_files(root):
        try:
            tree = ET.parse(project)
        except ET.ParseError as exc:
            raise BuildTopologyError(f"invalid project XML {project}: {exc}") from exc
        for element in tree.getroot().iter():
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
            "ReferenceOutputAssembly=false references must also use Private=false; "
            "otherwise IDE/BuildProjectReferences=false builds try to copy missing "
            "apphost/deps/runtimeconfig files: " + "; ".join(failures)
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
    tree = ET.parse(project)
    targets = {
        element.attrib.get("Name", ""): element
        for element in tree.getroot().iter()
        if local_name(element.tag) == "Target"
    }
    target = targets.get("BuildFeatureManifestEmitterForIsolatedBuild")
    if target is None:
        raise BuildTopologyError(
            "language pack lacks BuildFeatureManifestEmitterForIsolatedBuild"
        )
    if "EmitToolchainFeatureManifest" not in target.attrib.get("BeforeTargets", ""):
        raise BuildTopologyError(
            "isolated emitter build must run before EmitToolchainFeatureManifest"
        )
    condition = target.attrib.get("Condition", "")
    if "BuildProjectReferences" not in condition or "false" not in condition.lower():
        raise BuildTopologyError(
            "isolated emitter build must be gated by BuildProjectReferences=false"
        )
    if not any(local_name(element.tag) == "MSBuild" for element in target.iter()):
        raise BuildTopologyError("isolated emitter target does not build the emitter project")


def require_release_gate_parity(root: Path) -> None:
    required = (
        "Tools/package_metadata.py",
        "Tools/test-package-metadata-mutants.py",
    )
    for relative in ("build.sh", "build.ps1"):
        script = root / relative
        if not script.is_file():
            raise BuildTopologyError(f"canonical build entry point does not exist: {script}")
        text = script.read_text(encoding="utf-8-sig")
        missing = [token for token in required if token not in text]
        if missing:
            raise BuildTopologyError(
                f"{relative} omits mandatory package metadata gates: {missing}"
            )


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate IDE-safe project references and canonical build-gate parity."
    )
    parser.add_argument("--root", type=Path, default=Path.cwd())
    args = parser.parse_args()

    root = args.root.resolve()
    try:
        require_build_order_references_not_copy_local(root)
        require_language_pack_isolated_emitter_build(root)
        require_release_gate_parity(root)
    except BuildTopologyError as exc:
        print(f"BUILD_TOPOLOGY=FAIL: {exc}", file=sys.stderr)
        return 1

    print("BUILD_TOPOLOGY=PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
