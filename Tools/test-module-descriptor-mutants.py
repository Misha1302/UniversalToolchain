#!/usr/bin/env python3
"""Prove the ADR-008 boundary gate fails when the rule is broken.

A guard that only ever runs green is indistinguishable from one that asserts nothing.
"""
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

FILES = (
    Path("UniversalToolchain/UniversalToolchain.FeatureSdk/LanguageModuleRegistry.cs"),
    Path("UniversalToolchain/UniversalToolchain.FeatureSdk/LanguageModuleDescriptor.cs"),
    Path("UniversalToolchain/UniversalToolchain.LanguageSdk/LanguagePlan.cs"),
)


def run(checker: Path, root: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(checker), "--root", str(root)],
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
    )


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8-sig")
    if old not in text:
        raise AssertionError(f"mutation anchor not found in {path}: {old!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8-sig")


def fixture(source: Path, destination: Path) -> None:
    for relative in FILES:
        target = destination / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source / relative, target)


def expect_rejected(checker: Path, root: Path, label: str) -> None:
    result = run(checker, root)
    if result.returncode == 0:
        raise AssertionError(f"{label}: mutant survived\n{result.stdout}")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path.cwd())
    args = parser.parse_args()
    source = args.root.resolve()
    checker = source / "Tools/check-module-descriptor-boundary.py"

    with tempfile.TemporaryDirectory(prefix="module-descriptor-mutants-") as temporary:
        base = Path(temporary) / "baseline"
        fixture(source, base)
        result = run(checker, base)
        if result.returncode != 0:
            raise AssertionError(f"baseline must pass before mutating:\n{result.stdout}")

        # 1. The planner starts reading module descriptors - a second selection layer in embryo.
        planner = Path(temporary) / "planner-references-modules"
        shutil.copytree(base, planner)
        replace_once(
            planner / "UniversalToolchain/UniversalToolchain.LanguageSdk/LanguagePlan.cs",
            "    public string PlanHash { get; }",
            "    internal LanguageModuleDescriptor? ModuleProbe => null;\n    public string PlanHash { get; }",
        )
        expect_rejected(checker, planner, "planner references module descriptor")

        # 2. The registry grows a method that answers the planner's question.
        selecting = Path(temporary) / "registry-selects"
        shutil.copytree(base, selecting)
        replace_once(
            selecting / "UniversalToolchain/UniversalToolchain.FeatureSdk/LanguageModuleRegistry.cs",
            "    public IReadOnlyList<LanguageModuleDescriptor> Modules =>",
            "    public bool Select(LanguageModuleDescriptor descriptor) => true;\n\n"
            "    public IReadOnlyList<LanguageModuleDescriptor> Modules =>",
        )
        expect_rejected(checker, selecting, "registry exposes Select")

        # 3. The doc statement that keeps readers from assuming otherwise is dropped.
        undocumented = Path(temporary) / "descriptor-undocumented"
        shutil.copytree(base, undocumented)
        replace_once(
            undocumented / "UniversalToolchain/UniversalToolchain.FeatureSdk/LanguageModuleDescriptor.cs",
            "This is deliberately <em>not</em> a planner node.",
            "This is a grouping of contributions.",
        )
        expect_rejected(checker, undocumented, "descriptor loses non-planner statement")

    print("module-descriptor mutants rejected")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
