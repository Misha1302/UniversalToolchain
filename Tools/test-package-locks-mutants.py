#!/usr/bin/env python3
"""Mutation tests for Tools/check-package-locks.py.

Each case breaks the repository model in one specific way and asserts the gate rejects it.
A gate that cannot fail for the reason it exists is not a gate, so every failure mode the
checker claims to cover is exercised here against a real temporary tree.
"""
from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path


PROJECT = """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  {items}
</Project>
"""
PACKAGE_ITEMS = '<ItemGroup><PackageReference Include="NUnit" Version="4.2.2" /></ItemGroup>'
PROJECT_ITEMS = '<ItemGroup><ProjectReference Include="../Lib/Lib.csproj" /></ItemGroup>'

PACKAGE_LOCK = {
    "version": 1,
    "dependencies": {
        "net10.0": {
            "NUnit": {
                "type": "Direct",
                "requested": "[4.2.2, )",
                "resolved": "4.2.2",
                "contentHash": "deadbeef",
            }
        }
    },
}
EMPTY_LOCK = {"version": 1, "dependencies": {"net10.0": {}}}
PROJECT_ONLY_LOCK = {
    "version": 1,
    "dependencies": {"net10.0": {"lib": {"type": "Project"}}},
}


def write_tree(root: Path, *, packages: bool = True, lock: dict | None = PACKAGE_LOCK,
               template_lock: bool = False) -> None:
    # Each mutant starts from a clean tree; a leftover lock from a previous case would make the
    # next assertion pass or fail for the wrong reason.
    if root.exists():
        shutil.rmtree(root)
    (root / "UniversalToolchain/Lib").mkdir(parents=True, exist_ok=True)
    (root / "UniversalToolchain/Lib/Lib.csproj").write_text(
        PROJECT.format(items=PACKAGE_ITEMS if packages else PROJECT_ITEMS), encoding="utf-8")
    if lock is not None:
        (root / "UniversalToolchain/Lib/packages.lock.json").write_text(
            json.dumps(lock), encoding="utf-8")

    if template_lock:
        template = root / "UniversalToolchain/UniversalToolchain.Templates/content/ut-language"
        template.mkdir(parents=True, exist_ok=True)
        (template / "TemplateLanguage.csproj").write_text(
            PROJECT.format(items=PACKAGE_ITEMS), encoding="utf-8")
        (template / "packages.lock.json").write_text(json.dumps(PACKAGE_LOCK), encoding="utf-8")


def run(checker: Path, root: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(checker), "--root", str(root)],
        text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, check=False)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path.cwd())
    args = parser.parse_args()
    checker = (args.root / "Tools/check-package-locks.py").resolve()

    with tempfile.TemporaryDirectory(prefix="ut-lock-mutants-") as temporary:
        root = Path(temporary) / "repo"

        # Positive control: correct tree must pass, otherwise the mutants below prove nothing.
        write_tree(root)
        result = run(checker, root)
        if result.returncode != 0:
            raise RuntimeError(f"valid lock coverage was rejected:\n{result.stdout}")
        print("positive-control=1 complete-lock-coverage")

        # Mutant 1: a project resolves packages but has no lock at all. This is exactly the state
        # RestoreLockedMode silently accepts, since restore just writes a new lock file.
        (root / "UniversalToolchain/Lib/packages.lock.json").unlink()
        result = run(checker, root)
        if result.returncode == 0:
            raise RuntimeError("project with packages and no lock file survived")
        if "no packages.lock.json" not in result.stdout:
            raise RuntimeError(f"wrong diagnostic for missing lock:\n{result.stdout}")
        print("SURVIVOR=0 mutant=missing-lock-for-package-project")

        # Mutant 2: a lock file that exists but pins nothing. It looks authoritative and passes
        # RestoreLockedMode while recording no package resolution at all.
        write_tree(root, lock=EMPTY_LOCK)
        result = run(checker, root)
        if result.returncode == 0:
            raise RuntimeError("empty lock file survived")
        print("SURVIVOR=0 mutant=empty-lock-file")

        # Mutant 3: project-reference-only lock. Not a package resolution; keeping it makes every
        # added ProjectReference fail with NU1004 until a bulk regeneration is run.
        write_tree(root, packages=False, lock=PROJECT_ONLY_LOCK)
        result = run(checker, root)
        if result.returncode == 0:
            raise RuntimeError("project-reference-only lock survived")
        print("SURVIVOR=0 mutant=project-only-lock")

        # Mutant 4: lock inside the dotnet-new template payload, which is packed verbatim into the
        # template package and would ship to consumers.
        write_tree(root, template_lock=True)
        result = run(checker, root)
        if result.returncode == 0:
            raise RuntimeError("lock committed inside template content survived")
        if "template" not in result.stdout:
            raise RuntimeError(f"wrong diagnostic for template lock:\n{result.stdout}")
        print("SURVIVOR=0 mutant=lock-inside-template-content")

        # Mutant 5: a project resolves only project references and correctly has no lock. This is
        # the negative case: coverage must not demand locks that carry no information.
        write_tree(root, packages=False, lock=None)
        result = run(checker, root)
        if result.returncode != 0:
            raise RuntimeError(f"project-reference-only project incorrectly required a lock:\n{result.stdout}")
        print("SURVIVOR=0 mutant=none-lock-for-project-reference-only")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
