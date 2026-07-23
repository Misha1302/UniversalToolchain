#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path


def manifest_lines(path: Path) -> list[str]:
    return [
        line.strip()
        for line in path.read_text(encoding="utf-8").splitlines()
        if line.strip() and not line.lstrip().startswith("#")
    ]


def child_text(root: ET.Element, name: str) -> str | None:
    for element in root.iter():
        if element.tag.rsplit("}", 1)[-1] == name and element.text:
            return element.text.strip()
    return None


def project_identity(project: Path) -> tuple[str, str]:
    root = ET.parse(project).getroot()
    package_id = child_text(root, "PackageId") or project.stem
    version = child_text(root, "PackageVersion") or child_text(root, "Version")
    if not version:
        raise ValueError(f"Project has no package version: {project}")
    return package_id, version


def nuspec_identity(data: bytes) -> tuple[str, str, dict[str, str]]:
    root = ET.fromstring(data)
    package_id = child_text(root, "id")
    version = child_text(root, "version")
    if not package_id or not version:
        raise ValueError("Package nuspec has no id/version")
    dependencies: dict[str, str] = {}
    for element in root.iter():
        if element.tag.rsplit("}", 1)[-1] == "dependency":
            dependency_id = element.attrib.get("id")
            dependency_version = element.attrib.get("version")
            if dependency_id and dependency_version:
                dependencies[dependency_id] = dependency_version
    return package_id, version, dependencies


def fail(message: str) -> None:
    print(f"package-matrix: {message}", file=sys.stderr)
    raise SystemExit(1)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path.cwd())
    parser.add_argument("--manifest", type=Path, default=Path("eng/package-projects.txt"))
    parser.add_argument("--packages", type=Path, default=Path("artifacts/packages"))
    args = parser.parse_args()

    root = args.root.resolve()
    manifest = args.manifest if args.manifest.is_absolute() else root / args.manifest
    package_dir = args.packages if args.packages.is_absolute() else root / args.packages
    expected: dict[str, tuple[str, Path]] = {}
    for relative in manifest_lines(manifest):
        project = root / relative
        package_id, version = project_identity(project)
        if package_id in expected:
            fail(f"duplicate package ID {package_id}")
        expected[package_id] = (version, project)

    actual_files = {
        path.name: path
        for path in package_dir.glob("*.nupkg")
        if not path.name.endswith(".snupkg") and not path.name.endswith(".symbols.nupkg")
    }
    expected_names = {f"{package_id}.{version}.nupkg" for package_id, (version, _) in expected.items()}
    missing = sorted(expected_names - actual_files.keys())
    extra = sorted(actual_files.keys() - expected_names)
    if missing or extra:
        fail(f"package set mismatch; missing={missing}, extra={extra}")

    identities = {package_id: version for package_id, (version, _) in expected.items()}
    for package_id, (expected_version, _) in sorted(expected.items()):
        package_path = actual_files[f"{package_id}.{expected_version}.nupkg"]
        with zipfile.ZipFile(package_path) as archive:
            nuspec_names = [name for name in archive.namelist() if name.endswith(".nuspec")]
            if len(nuspec_names) != 1:
                fail(f"{package_path.name} contains {len(nuspec_names)} nuspec files")
            actual_id, actual_version, dependencies = nuspec_identity(archive.read(nuspec_names[0]))
            if (actual_id, actual_version) != (package_id, expected_version):
                fail(
                    f"{package_path.name} identity is {actual_id}/{actual_version}, "
                    f"expected {package_id}/{expected_version}"
                )
            for dependency_id, dependency_version in dependencies.items():
                if dependency_id in identities and identities[dependency_id] not in dependency_version:
                    fail(
                        f"{package_id} references {dependency_id} {dependency_version}, "
                        f"expected package-family version {identities[dependency_id]}"
                    )

            if package_id == "UniversalToolchain.Wist.LanguagePack":
                names = [name for name in archive.namelist() if name.endswith(".toolchain.feature.json")]
                if len(names) != 1:
                    fail(f"Wist LanguagePack contains {len(names)} feature manifests")
                feature_manifest = json.loads(archive.read(names[0]))
                if feature_manifest.get("schemaVersion") != 5:
                    fail("Wist LanguagePack feature manifest is not schema v5")
                if feature_manifest.get("canonicalization") != "universaltoolchain-json-v1":
                    fail("Wist LanguagePack feature manifest has wrong canonicalization")
                if feature_manifest.get("hashAlgorithm") != "sha256":
                    fail("Wist LanguagePack feature manifest has wrong hash algorithm")
                package = feature_manifest.get("package", {})
                if package.get("id") != package_id or package.get("version") != expected_version:
                    fail("Wist LanguagePack embedded descriptor identity is inconsistent")

            if package_id == "UniversalToolchain.Templates":
                if not any(name.endswith(".template.config/template.json") for name in archive.namelist()):
                    fail("Template package contains no dotnet template configuration")

    print(f"package-matrix: verified {len(expected)} packages")


if __name__ == "__main__":
    main()
