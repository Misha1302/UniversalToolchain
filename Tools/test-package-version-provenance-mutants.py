#!/usr/bin/env python3
from __future__ import annotations

import argparse
import subprocess
import tempfile
import zipfile
from pathlib import Path


def write_package(path: Path, package_id: str, version: str, marker: str) -> None:
    nuspec = f'''<?xml version="1.0"?><package><metadata><id>{package_id}</id><version>{version}</version><authors>x</authors><description>x</description></metadata></package>'''
    with zipfile.ZipFile(path, "w") as archive:
        archive.writestr(f"{package_id}.nuspec", nuspec)
        archive.writestr("lib/net10.0/payload.txt", marker)


def run(checker: Path, bundle: Path, current: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["python3", str(checker), "--previous-bundle", str(bundle), "--current-packages", str(current)],
        text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, check=False)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    checker = args.root / "Tools/check-package-version-provenance.py"
    with tempfile.TemporaryDirectory(prefix="package-version-mutants-") as tmp_name:
        tmp = Path(tmp_name)
        previous_package = tmp / "Example.1.0.0-alpha.1.nupkg"
        write_package(previous_package, "Example", "1.0.0-alpha.1", "old")
        bundle = tmp / "previous.zip"
        with zipfile.ZipFile(bundle, "w") as archive:
            archive.write(previous_package, "packages/" + previous_package.name)
        current = tmp / "current"
        current.mkdir()
        write_package(current / "Example.1.0.0-alpha.1.nupkg", "Example", "1.0.0-alpha.1", "changed")
        if run(checker, bundle, current).returncode == 0:
            raise RuntimeError("same-version changed-payload mutant survived")
        print("SURVIVOR=0 mutant=same-version-changed-payload")
        (current / "Example.1.0.0-alpha.1.nupkg").unlink()
        write_package(current / "Example.1.0.0-alpha.0.nupkg", "Example", "1.0.0-alpha.0", "changed")
        if run(checker, bundle, current).returncode == 0:
            raise RuntimeError("downgrade mutant survived")
        print("SURVIVOR=0 mutant=package-version-downgrade")
        (current / "Example.1.0.0-alpha.0.nupkg").unlink()
        write_package(current / "Example.1.0.0-alpha.2.nupkg", "Example", "1.0.0-alpha.2", "changed")
        if run(checker, bundle, current).returncode != 0:
            raise RuntimeError("valid monotonic version bump was rejected")
        print("positive-control=1 monotonic-package-version")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
