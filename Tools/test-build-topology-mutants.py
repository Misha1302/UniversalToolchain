#!/usr/bin/env python3
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path


INPUTS = (
    Path("build.sh"),
    Path("build.ps1"),
    Path("UniversalToolchain/Tests/Tests.csproj"),
    Path(
        "UniversalToolchain/UniversalToolchain.Dialects.Tests/"
        "UniversalToolchain.Dialects.Tests.csproj"
    ),
    Path(
        "UniversalToolchain/UniversalToolchain.Wist.LanguagePack/"
        "UniversalToolchain.Wist.LanguagePack.csproj"
    ),
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
    for relative in INPUTS:
        target = destination / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source / relative, target)


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8-sig")
    if old not in text:
        raise AssertionError(f"mutation anchor not found in {path}: {old!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8")


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

    try:
        temporary, path = fixture(root, checker)
        with temporary:
            replace_once(path / INPUTS[3], '\n                          Private="false"', "")
            run_checker(checker, path, "ReferenceOutputAssembly=false references")

        temporary, path = fixture(root, checker)
        with temporary:
            replace_once(
                path / INPUTS[4],
                'Name="BuildFeatureManifestEmitterForIsolatedBuild"',
                'Name="DisabledFeatureManifestEmitterForIsolatedBuild"',
            )
            run_checker(checker, path, "lacks BuildFeatureManifestEmitterForIsolatedBuild")

        temporary, path = fixture(root, checker)
        with temporary:
            replace_once(
                path / "build.ps1",
                "Tools/package_metadata.py",
                "Tools/package_metadata.disabled.py",
            )
            run_checker(checker, path, "build.ps1 omits mandatory package metadata gates")

        temporary, path = fixture(root, checker)
        with temporary:
            replace_once(
                path / "build.sh",
                "Tools/test-package-metadata-mutants.py",
                "Tools/test-package-metadata-mutants.disabled.py",
            )
            run_checker(checker, path, "build.sh omits mandatory package metadata gates")
    except (AssertionError, OSError) as exc:
        print(f"BUILD_TOPOLOGY_MUTANTS=FAIL: {exc}", file=sys.stderr)
        return 1

    print("BUILD_TOPOLOGY_MUTANTS=PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
