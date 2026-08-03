#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
import xml.etree.ElementTree as ET
import zipfile
from dataclasses import dataclass
from pathlib import Path

MATRIX_BEGIN = "<!-- package-matrix:begin -->"
MATRIX_END = "<!-- package-matrix:end -->"
DEFAULT_DOCS = (
    "VERIFICATION.md",
    "docs/evidence/current-verification.md",
    "docs/evidence/maintainer-guide.md",
    "RELEASE_NOTES_RU.md",
)


class MetadataError(ValueError):
    pass


@dataclass(frozen=True)
class PackageIdentity:
    package_id: str
    version: str

    @property
    def file_name(self) -> str:
        return f"{self.package_id}.{self.version}.nupkg"


def manifest_lines(path: Path) -> list[str]:
    if not path.is_file():
        raise MetadataError(f"package project manifest does not exist: {path}")
    return [
        line.strip()
        for line in path.read_text(encoding="utf-8").splitlines()
        if line.strip() and not line.lstrip().startswith("#")
    ]


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def child_text(root: ET.Element, name: str) -> str | None:
    for element in root.iter():
        if local_name(element.tag) == name:
            text = (element.text or "").strip()
            if text:
                return text
    return None


def project_identity(path: Path) -> PackageIdentity:
    if not path.is_file():
        raise MetadataError(f"package project does not exist: {path}")
    try:
        root = ET.parse(path).getroot()
    except ET.ParseError as exc:
        raise MetadataError(f"invalid project XML {path}: {exc}") from exc
    package_id = child_text(root, "PackageId") or path.stem
    version = child_text(root, "PackageVersion") or child_text(root, "Version")
    if not version:
        raise MetadataError(f"project has no package version: {path}")
    return PackageIdentity(package_id, version)


def nuspec_identity(path: Path) -> PackageIdentity:
    try:
        with zipfile.ZipFile(path) as archive:
            nuspecs = [name for name in archive.namelist() if name.lower().endswith(".nuspec")]
            if len(nuspecs) != 1:
                raise MetadataError(f"{path.name}: expected exactly one nuspec, found {len(nuspecs)}")
            root = ET.fromstring(archive.read(nuspecs[0]))
    except zipfile.BadZipFile as exc:
        raise MetadataError(f"invalid nupkg ZIP {path}: {exc}") from exc
    except ET.ParseError as exc:
        raise MetadataError(f"invalid nuspec XML in {path}: {exc}") from exc
    package_id = child_text(root, "id")
    version = child_text(root, "version")
    if not package_id or not version:
        raise MetadataError(f"{path.name}: nuspec identity is incomplete")
    return PackageIdentity(package_id, version)


def parse_document_matrix(path: Path) -> dict[str, str]:
    if not path.is_file():
        raise MetadataError(f"active package documentation does not exist: {path}")
    text = path.read_text(encoding="utf-8")
    begin = text.count(MATRIX_BEGIN)
    end = text.count(MATRIX_END)
    if begin != 1 or end != 1:
        raise MetadataError(
            f"{path}: expected exactly one canonical package matrix block, found begin={begin}, end={end}"
        )
    body = text.split(MATRIX_BEGIN, 1)[1].split(MATRIX_END, 1)[0]
    result: dict[str, str] = {}
    for raw_line in body.splitlines():
        line = raw_line.strip()
        if not line.startswith("|"):
            continue
        cells = [cell.strip().strip("`") for cell in line.strip("|").split("|")]
        if len(cells) != 2 or cells[0] in {"Package ID", "---", ":---"} or set(cells[0]) <= {"-", ":"}:
            continue
        package_id, version = cells
        if not package_id or not version:
            continue
        if package_id in result:
            raise MetadataError(f"{path}: duplicate package matrix row for {package_id}")
        result[package_id] = version
    if not result:
        raise MetadataError(f"{path}: canonical package matrix block contains no package rows")
    return result


def collect_expected(root: Path, manifest: Path) -> tuple[dict[str, PackageIdentity], dict[str, Path]]:
    expected: dict[str, PackageIdentity] = {}
    projects: dict[str, Path] = {}
    for relative in manifest_lines(manifest):
        project = (root / relative).resolve()
        identity = project_identity(project)
        if identity.package_id in expected:
            raise MetadataError(
                f"duplicate PackageId {identity.package_id} in {projects[identity.package_id]} and {project}"
            )
        expected[identity.package_id] = identity
        projects[identity.package_id] = project
    if not expected:
        raise MetadataError("canonical package project list is empty")
    return expected, projects


def collect_actual(package_dir: Path) -> tuple[dict[str, PackageIdentity], dict[str, Path]]:
    if not package_dir.is_dir():
        raise MetadataError(f"package directory does not exist: {package_dir}")
    identities: dict[str, PackageIdentity] = {}
    files: dict[str, Path] = {}
    for path in sorted(package_dir.glob("*.nupkg")):
        if path.name.endswith((".snupkg", ".symbols.nupkg")):
            continue
        identity = nuspec_identity(path)
        if identity.package_id in identities:
            raise MetadataError(
                f"duplicate package identity {identity.package_id}: {files[identity.package_id].name}, {path.name}"
            )
        identities[identity.package_id] = identity
        files[identity.package_id] = path
    if not identities:
        raise MetadataError(f"package directory contains no primary nupkg files: {package_dir}")
    return identities, files


def validate(
    root: Path,
    manifest: Path,
    package_dir: Path,
    docs: list[Path],
) -> dict[str, object]:
    expected, projects = collect_expected(root, manifest)
    actual, package_files = collect_actual(package_dir)

    expected_ids = set(expected)
    actual_ids = set(actual)
    missing = sorted(expected_ids - actual_ids)
    extra = sorted(actual_ids - expected_ids)
    if missing or extra:
        raise MetadataError(f"package set mismatch: missing={missing}, extra={extra}")

    for package_id in sorted(expected):
        project_identity_value = expected[package_id]
        package_identity_value = actual[package_id]
        package_path = package_files[package_id]
        if package_identity_value != project_identity_value:
            raise MetadataError(
                f"{package_path.name}: embedded identity {package_identity_value.package_id}/{package_identity_value.version} "
                f"does not match project {project_identity_value.package_id}/{project_identity_value.version}"
            )
        if package_path.name != project_identity_value.file_name:
            raise MetadataError(
                f"{package_path.name}: file name does not match embedded/project identity; "
                f"expected {project_identity_value.file_name}"
            )

    expected_versions = {package_id: identity.version for package_id, identity in expected.items()}
    doc_results: dict[str, dict[str, str]] = {}
    for doc in docs:
        matrix = parse_document_matrix(doc)
        if matrix != expected_versions:
            missing_rows = sorted(expected_ids - set(matrix))
            extra_rows = sorted(set(matrix) - expected_ids)
            mismatched = {
                package_id: {"expected": expected_versions[package_id], "actual": matrix.get(package_id)}
                for package_id in sorted(expected_ids & set(matrix))
                if matrix[package_id] != expected_versions[package_id]
            }
            raise MetadataError(
                f"{doc}: package matrix mismatch: missing={missing_rows}, extra={extra_rows}, versions={mismatched}"
            )
        doc_results[str(doc.relative_to(root) if doc.is_relative_to(root) else doc)] = matrix

    return {
        "schemaVersion": 1,
        "packageCount": len(expected),
        "packages": [
            {
                "id": package_id,
                "version": expected[package_id].version,
                "project": str(projects[package_id].relative_to(root)),
                "nupkg": package_files[package_id].name,
            }
            for package_id in sorted(expected)
        ],
        "documents": sorted(doc_results),
        "status": "PASS",
    }


def run_provenance_gate(
    root: Path,
    previous_bundle: Path,
    package_dir: Path,
    baseline_contract: Path | None,
) -> None:
    command = [
        sys.executable,
        str(root / "Tools" / "check-package-version-provenance.py"),
        "--previous-bundle",
        str(previous_bundle),
        "--current-packages",
        str(package_dir),
    ]
    if baseline_contract is not None:
        command.extend(["--baseline-contract", str(baseline_contract)])
    completed = subprocess.run(command, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, check=False)
    if completed.returncode != 0:
        raise MetadataError("package version/content provenance failed:\n" + completed.stdout.rstrip())


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate project, nupkg, documentation, and optional provenance package metadata.")
    parser.add_argument("--root", type=Path, default=Path.cwd())
    parser.add_argument("--manifest", type=Path, default=Path("eng/package-projects.txt"))
    parser.add_argument("--packages", type=Path, default=Path("artifacts/packages"))
    parser.add_argument("--document", action="append", type=Path, dest="documents")
    parser.add_argument("--previous-bundle", type=Path)
    parser.add_argument("--baseline-contract", type=Path)
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()

    root = args.root.resolve()
    manifest = args.manifest.resolve() if args.manifest.is_absolute() else (root / args.manifest).resolve()
    packages = args.packages.resolve() if args.packages.is_absolute() else (root / args.packages).resolve()
    documents = args.documents or [Path(value) for value in DEFAULT_DOCS]
    docs = [path.resolve() if path.is_absolute() else (root / path).resolve() for path in documents]
    baseline_contract = None
    if args.baseline_contract:
        baseline_contract = args.baseline_contract.resolve() if args.baseline_contract.is_absolute() else (root / args.baseline_contract).resolve()

    try:
        report = validate(root, manifest, packages, docs)
        if args.previous_bundle:
            previous = args.previous_bundle.resolve() if args.previous_bundle.is_absolute() else (root / args.previous_bundle).resolve()
            run_provenance_gate(root, previous, packages, baseline_contract)
            report["provenance"] = "PASS"
    except (OSError, MetadataError, json.JSONDecodeError) as exc:
        print(f"package-metadata: ERROR: {exc}", file=sys.stderr)
        return 1

    report_path = args.report
    if report_path:
        report_path = report_path.resolve() if report_path.is_absolute() else (root / report_path).resolve()
        report_path.parent.mkdir(parents=True, exist_ok=True)
        report_path.write_text(json.dumps(report, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(f"package-metadata: verified {report['packageCount']} packages across {len(report['documents'])} active documents")
    if report.get("provenance") == "PASS":
        print("package-metadata: version/content provenance PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
