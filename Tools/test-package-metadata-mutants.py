#!/usr/bin/env python3
from __future__ import annotations

import argparse
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

MATRIX_BEGIN = "<!-- package-matrix:begin -->"
MATRIX_END = "<!-- package-matrix:end -->"
DOCS = (
    "VERIFICATION.md",
    "docs/evidence/current-verification.md",
    "docs/evidence/maintainer-guide.md",
    "RELEASE_NOTES_RU.md",
)


def write_project(path: Path, package_id: str, version: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><PackageId>{package_id}</PackageId><Version>{version}</Version></PropertyGroup></Project>\n',
        encoding="utf-8",
    )


def write_package(path: Path, package_id: str, version: str, marker: str = "payload") -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    nuspec = (
        '<?xml version="1.0"?><package><metadata>'
        f'<id>{package_id}</id><version>{version}</version><authors>x</authors><description>x</description>'
        '</metadata></package>'
    )
    with zipfile.ZipFile(path, "w") as archive:
        archive.writestr(f"{package_id}.nuspec", nuspec)
        archive.writestr("lib/net10.0/payload.txt", marker)


def matrix_text(entries: list[tuple[str, str]]) -> str:
    rows = "\n".join(f"| `{package_id}` | `{version}` |" for package_id, version in entries)
    return (
        f"{MATRIX_BEGIN}\n"
        "| Package ID | Version |\n"
        "|---|---|\n"
        f"{rows}\n"
        f"{MATRIX_END}\n"
    )


def create_fixture(root: Path) -> None:
    entries = [("Example.Core", "1.0.0-alpha.1"), ("Example.Wist", "2.0.0-alpha.2")]
    projects = []
    for package_id, version in entries:
        project = root / "src" / package_id / f"{package_id}.csproj"
        write_project(project, package_id, version)
        projects.append(project.relative_to(root).as_posix())
        write_package(root / "artifacts" / "packages" / f"{package_id}.{version}.nupkg", package_id, version)
    (root / "eng").mkdir(parents=True, exist_ok=True)
    (root / "eng" / "package-projects.txt").write_text("\n".join(projects) + "\n", encoding="utf-8")
    for relative in DOCS:
        path = root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("# Fixture\n\n" + matrix_text(entries), encoding="utf-8")


def run(checker: Path, root: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [
            "python3",
            str(checker),
            "--root",
            str(root),
            "--manifest",
            "eng/package-projects.txt",
            "--packages",
            "artifacts/packages",
        ],
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
    )


def require_killed(name: str, checker: Path, pristine: Path, mutate) -> None:
    mutant = pristine.parent / name
    shutil.copytree(pristine, mutant)
    mutate(mutant)
    completed = run(checker, mutant)
    if completed.returncode == 0:
        raise RuntimeError(f"{name} mutant survived:\n{completed.stdout}")
    print(f"SURVIVOR=0 mutant={name}")


def set_project_version(path: Path, version: str) -> None:
    tree = ET.parse(path)
    root = tree.getroot()
    version_element = next(element for element in root.iter() if element.tag.rsplit("}", 1)[-1] == "Version")
    version_element.text = version
    tree.write(path, encoding="unicode")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    checker = args.root.resolve() / "Tools" / "package_metadata.py"

    with tempfile.TemporaryDirectory(prefix="package-metadata-mutants-") as tmp_name:
        tmp = Path(tmp_name)
        pristine = tmp / "pristine"
        create_fixture(pristine)
        positive = run(checker, pristine)
        if positive.returncode != 0:
            raise RuntimeError("positive package metadata fixture failed:\n" + positive.stdout)
        print("positive-control=1 canonical-package-metadata")

        require_killed(
            "project-version-without-docs",
            checker,
            pristine,
            lambda root: set_project_version(root / "src/Example.Core/Example.Core.csproj", "1.0.0-alpha.2"),
        )
        require_killed(
            "docs-version-without-project",
            checker,
            pristine,
            lambda root: (root / "VERIFICATION.md").write_text(
                (root / "VERIFICATION.md").read_text(encoding="utf-8").replace("1.0.0-alpha.1", "1.0.0-alpha.2"),
                encoding="utf-8",
            ),
        )
        require_killed(
            "stale-nupkg-version",
            checker,
            pristine,
            lambda root: (
                (root / "artifacts/packages/Example.Wist.2.0.0-alpha.2.nupkg").unlink(),
                write_package(root / "artifacts/packages/Example.Wist.2.0.0-alpha.1.nupkg", "Example.Wist", "2.0.0-alpha.1"),
            ),
        )
        require_killed(
            "missing-nupkg",
            checker,
            pristine,
            lambda root: (root / "artifacts/packages/Example.Core.1.0.0-alpha.1.nupkg").unlink(),
        )
        require_killed(
            "duplicate-package-identity",
            checker,
            pristine,
            lambda root: write_package(root / "artifacts/packages/duplicate.nupkg", "Example.Core", "1.0.0-alpha.1", "duplicate"),
        )
        require_killed(
            "correct-filename-wrong-nuspec",
            checker,
            pristine,
            lambda root: write_package(
                root / "artifacts/packages/Example.Wist.2.0.0-alpha.2.nupkg",
                "Example.Wist.Wrong",
                "2.0.0-alpha.2",
                "wrong nuspec",
            ),
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
