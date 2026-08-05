#!/usr/bin/env python3
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path


DIALECT_TEST_PROJECT = Path(
    "UniversalToolchain/UniversalToolchain.Dialects.Tests/"
    "UniversalToolchain.Dialects.Tests.csproj"
)
FRESH_PROCESS_HOST_PROJECT = Path(
    "UniversalToolchain/UniversalToolchain.Dialects.Tests/FreshProcess/"
    "RuntimeSharedAssemblyFreshProcessHost/RuntimeSharedAssemblyFreshProcessHost.csproj"
)
LANGUAGE_PACK_PROJECT = Path(
    "UniversalToolchain/UniversalToolchain.Wist.LanguagePack/"
    "UniversalToolchain.Wist.LanguagePack.csproj"
)
SUPPORT_FILES = (
    Path("build.sh"),
    Path("build.ps1"),
    Path("Tools/test-build-topology-runtime.py"),
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


def rename_target(path: Path, old_name: str, new_name: str) -> None:
    tree = ET.parse(path)
    matches = [
        element
        for element in tree.getroot().iter()
        if element.tag.rsplit("}", 1)[-1] == "Target"
        and element.attrib.get("Name") == old_name
    ]
    if len(matches) != 1:
        raise AssertionError(f"expected target {old_name!r} in {path}")
    matches[0].set("Name", new_name)
    tree.write(path, encoding="unicode")


def redirect_msbuild_project(path: Path, target_name: str, project: str) -> None:
    tree = ET.parse(path)
    targets = [
        element
        for element in tree.getroot().iter()
        if element.tag.rsplit("}", 1)[-1] == "Target"
        and element.attrib.get("Name") == target_name
    ]
    if len(targets) != 1:
        raise AssertionError(f"expected target {target_name!r} in {path}")
    tasks = [
        element
        for element in targets[0]
        if element.tag.rsplit("}", 1)[-1] == "MSBuild"
    ]
    if len(tasks) != 1:
        raise AssertionError(f"expected one MSBuild task in {target_name!r}")
    tasks[0].set("Projects", project)
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

    try:
        temporary, path = fixture(root, checker)
        with temporary:
            mutate_project_reference(
                path / DIALECT_TEST_PROJECT,
                "RuntimeSharedAssemblyFreshProcessHost.csproj",
            )
            run_checker(checker, path, "repository build-order ProjectReference")

        temporary, path = fixture(root, checker)
        with temporary:
            mutate_project_reference(
                path / FRESH_PROCESS_HOST_PROJECT,
                "CanonicalRuntimeFixture.csproj",
            )
            run_checker(checker, path, "repository build-order ProjectReference")

        temporary, path = fixture(root, checker)
        with temporary:
            rename_target(
                path / LANGUAGE_PACK_PROJECT,
                "BuildFeatureManifestEmitterForIsolatedBuild",
                "DisabledFeatureManifestEmitterForIsolatedBuild",
            )
            run_checker(checker, path, "lacks BuildFeatureManifestEmitterForIsolatedBuild")

        temporary, path = fixture(root, checker)
        with temporary:
            redirect_msbuild_project(
                path / LANGUAGE_PACK_PROJECT,
                "BuildFeatureManifestEmitterForIsolatedBuild",
                "wrong-emitter.csproj",
            )
            run_checker(checker, path, "builds the wrong project")

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

        temporary, path = fixture(root, checker)
        with temporary:
            replace_once(
                path / "build.sh",
                "Tools/test-build-topology-runtime.py",
                "Tools/test-build-topology-runtime.disabled.py",
            )
            run_checker(checker, path, "build.sh runtime topology invocation")
    except (AssertionError, OSError) as exc:
        print(f"BUILD_TOPOLOGY_MUTANTS=FAIL: {exc}", file=sys.stderr)
        return 1

    print("BUILD_TOPOLOGY_MUTANTS=PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
