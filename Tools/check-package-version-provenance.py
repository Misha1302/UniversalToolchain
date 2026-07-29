#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import re
import tempfile
import zipfile
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path

SEMVER = re.compile(r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-([0-9A-Za-z.-]+))?(?:\+[0-9A-Za-z.-]+)?$")


@dataclass(frozen=True)
class Version:
    major: int
    minor: int
    patch: int
    prerelease: tuple[tuple[int, int | str], ...] | None

    @staticmethod
    def parse(value: str) -> "Version":
        match = SEMVER.fullmatch(value)
        if not match:
            raise ValueError(f"unsupported SemVer value: {value!r}")
        pre = match.group(4)
        parsed = None if pre is None else tuple(
            (0, int(part)) if part.isdigit() else (1, part)
            for part in pre.split(".")
        )
        return Version(int(match.group(1)), int(match.group(2)), int(match.group(3)), parsed)

    def key(self):
        release_rank = 1 if self.prerelease is None else 0
        return self.major, self.minor, self.patch, release_rank, self.prerelease or ()


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def child_text(root: ET.Element, name: str) -> str | None:
    for element in root.iter():
        if element.tag.rsplit("}", 1)[-1] == name:
            return (element.text or "").strip() or None
    return None


def package_identity(path: Path) -> tuple[str, str]:
    with zipfile.ZipFile(path) as archive:
        nuspecs = [name for name in archive.namelist() if name.endswith(".nuspec")]
        if len(nuspecs) != 1:
            raise ValueError(f"{path}: expected exactly one nuspec, found {len(nuspecs)}")
        root = ET.fromstring(archive.read(nuspecs[0]))
    package_id = child_text(root, "id")
    version = child_text(root, "version")
    if not package_id or not version:
        raise ValueError(f"{path}: nuspec identity is incomplete")
    return package_id, version


def collect_packages_from_bundle(bundle: Path, temporary: Path) -> dict[str, tuple[str, Path]]:
    result: dict[str, tuple[str, Path]] = {}
    with zipfile.ZipFile(bundle) as archive:
        for name in archive.namelist():
            if not name.endswith(".nupkg") or name.endswith((".snupkg", ".symbols.nupkg")):
                continue
            data = archive.read(name)
            output = temporary / Path(name).name
            output.write_bytes(data)
            package_id, version = package_identity(output)
            if package_id in result:
                raise ValueError(f"previous bundle contains duplicate package ID {package_id}")
            result[package_id] = (version, output)
    if not result:
        raise ValueError("previous package bundle contains no .nupkg files")
    return result


def validate(previous_bundle: Path, current_dir: Path, baseline_contract: Path | None = None) -> int:
    if baseline_contract is not None:
        contract = json.loads(baseline_contract.read_text(encoding="utf-8"))
        if contract.get("schemaVersion") != 1:
            raise ValueError("unsupported package release baseline schema")
        expected_name = contract.get("previousBundleFileName")
        expected_sha = contract.get("previousBundleSha256")
        if expected_name and previous_bundle.name != expected_name:
            raise ValueError(f"unexpected previous package bundle name: {previous_bundle.name!r}")
        if expected_sha and sha256(previous_bundle) != expected_sha:
            raise ValueError("previous package bundle SHA-256 does not match the reviewed baseline")
    current_files = sorted(
        path for path in current_dir.glob("*.nupkg")
        if not path.name.endswith((".snupkg", ".symbols.nupkg"))
    )
    if not current_files:
        raise ValueError("current package directory contains no .nupkg files")
    with tempfile.TemporaryDirectory(prefix="ut-previous-packages-") as tmp:
        previous = collect_packages_from_bundle(previous_bundle, Path(tmp))
        checked = 0
        for current_path in current_files:
            package_id, current_version = package_identity(current_path)
            if package_id not in previous:
                continue
            previous_version, previous_path = previous[package_id]
            if sha256(current_path) == sha256(previous_path):
                if current_version != previous_version:
                    raise ValueError(f"{package_id}: identical payload changed version {previous_version} -> {current_version}")
            elif Version.parse(current_version).key() <= Version.parse(previous_version).key():
                raise ValueError(
                    f"{package_id}: payload changed without a monotonic version bump "
                    f"({previous_version} -> {current_version})"
                )
            checked += 1
    return checked


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--previous-bundle", required=True, type=Path)
    parser.add_argument("--current-packages", required=True, type=Path)
    parser.add_argument("--baseline-contract", type=Path)
    args = parser.parse_args()
    try:
        count = validate(args.previous_bundle.resolve(), args.current_packages.resolve(), args.baseline_contract.resolve() if args.baseline_contract else None)
    except (OSError, ValueError, zipfile.BadZipFile, ET.ParseError, json.JSONDecodeError) as exc:
        print(f"ERROR: {exc}")
        return 1
    print(f"package version provenance OK: {count} existing package identities checked")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
