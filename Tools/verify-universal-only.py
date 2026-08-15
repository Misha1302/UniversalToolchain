#!/usr/bin/env python3
from __future__ import annotations

import argparse
import importlib.util
import subprocess
import sys
import tempfile
from pathlib import Path


def load_ownership_module(root: Path):
    path = root / "Tools" / "check-project-ownership.py"
    spec = importlib.util.spec_from_file_location("project_ownership", path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Cannot load ownership validator from {path}")
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def run(*args: str, cwd: Path) -> None:
    print("+", " ".join(args), flush=True)
    subprocess.run(args, cwd=cwd, check=True)


def is_test_project(path: Path) -> bool:
    text = path.read_text(encoding="utf-8-sig")
    return "Microsoft.NET.Test.Sdk" in text or "<IsTestProject>true</IsTestProject>" in text


def main() -> int:
    parser = argparse.ArgumentParser(description="Build and test only projects classified as UNIVERSAL.")
    parser.add_argument("--root", type=Path, default=Path.cwd())
    args = parser.parse_args()
    root = args.root.resolve()
    ownership = load_ownership_module(root)
    manifest = ownership.load_manifest(root)
    owners = ownership.require_graph_direction(root, manifest)
    ownership.require_universal_sources_language_neutral(root, manifest, owners)

    universal = sorted(path for path, owner in owners.items() if owner == "UNIVERSAL")
    if not universal:
        raise RuntimeError("No UNIVERSAL projects were classified.")

    tests = [project for project in universal if is_test_project(project)]
    with tempfile.TemporaryDirectory(prefix="ut-only-") as tmp:
        workspace = Path(tmp)
        run("dotnet", "new", "sln", "-n", "UniversalOnly", cwd=workspace)
        solution = workspace / "UniversalOnly.sln"
        for project in universal:
            run("dotnet", "sln", str(solution), "add", str(project), cwd=root)
        run("dotnet", "restore", str(solution), "--nologo", cwd=root)
        run("dotnet", "build", str(solution), "-c", "Release", "--no-restore", "--nologo", cwd=root)
        for project in tests:
            run("dotnet", "test", str(project), "-c", "Release", "--no-build", "--nologo", cwd=root)

    print(f"UNIVERSAL_ONLY=PASS projects={len(universal)} tests={len(tests)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
