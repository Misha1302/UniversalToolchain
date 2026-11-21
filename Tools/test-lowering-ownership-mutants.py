#!/usr/bin/env python3
"""Prove the lowering-ownership gate fails when the declarative source is bypassed.

The migration removed a thirteen-term `||` chain. Re-adding one is a small change that compiles and
passes the tests, so the guard is tested against each shape it forbids. A guard that only runs green
asserts nothing.
"""
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

FILES = (
    Path("UniversalToolchain/UniversalToolchain.Wist.LanguagePack/WistSemanticBytecodeLowerer.cs"),
    Path("UniversalToolchain/UniversalToolchain.Wist.LanguagePack/WistSemanticBytecodeLowerer.Calls.cs"),
    Path("UniversalToolchain/UniversalToolchain.Wist.LanguagePack/WistLoweringOwners.cs"),
    Path("UniversalToolchain/UniversalToolchain.Wist.LanguagePack/WistModuleDescriptors.cs"),
)

CHAIN_ANCHOR = """    public static bool SupportsModuleContribution(LanguageContributionId contributionId) =>
        WistLoweringOwners.OwnsLowering(contributionId);"""


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
    checker = source / "Tools/check-lowering-ownership.py"

    with tempfile.TemporaryDirectory(prefix="lowering-ownership-mutants-") as temporary:
        base = Path(temporary) / "baseline"
        fixture(source, base)
        result = run(checker, base)
        if result.returncode != 0:
            raise AssertionError(f"baseline must pass before mutating:\n{result.stdout}")

        # 1. The original defect: the predicate becomes a comparison chain again.
        chained = Path(temporary) / "predicate-chain"
        shutil.copytree(base, chained)
        replace_once(
            chained / FILES[0],
            CHAIN_ANCHOR,
            """    public static bool SupportsModuleContribution(LanguageContributionId contributionId) =>
        contributionId == WistContributionIds.ScopesModule
        || contributionId == WistContributionIds.NumbersModule
        || contributionId == WistContributionIds.ArithmeticModule;""",
        )
        expect_rejected(checker, chained, "predicate becomes a comparison chain")

        # 2. A competing chain elsewhere expresses ownership for a different subset.
        elsewhere = Path(temporary) / "competing-chain"
        shutil.copytree(base, elsewhere)
        replace_once(
            elsewhere / FILES[1],
            "    private void RequireModule(LanguageContributionId syntaxContributionId)\n    {",
            """    private static bool LowersSomething(LanguageContributionId id) =>
        id == WistContributionIds.NumbersModule
        || id == WistContributionIds.VariablesModule
        || id == WistContributionIds.LoopsModule;

    private void RequireModule(LanguageContributionId syntaxContributionId)
    {""",
        )
        expect_rejected(checker, elsewhere, "competing ownership chain in another file")

        # 3. Module descriptors stop deriving the lowering phase from the declarative list.
        derived = Path(temporary) / "descriptors-bypass"
        shutil.copytree(base, derived)
        replace_once(
            derived / FILES[3],
            "if (WistLoweringOwners.OwnsLowering(component.ContributionId))",
            "if (WistSemanticBytecodeLowerer.SupportsModuleContribution(component.ContributionId))",
        )
        expect_rejected(checker, derived, "descriptors derive phases from the lowering predicate")

    print("lowering-ownership mutants rejected")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
