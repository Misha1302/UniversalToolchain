#!/usr/bin/env python3
"""Prove the workflow-command gate fails when a `run` block is silently split.

Two defects of this exact shape reached `master` from one change, so the guard is tested against the
real incidents rather than hypothetical ones. A guard that only runs green asserts nothing.
"""
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

WORKFLOW = Path(".github/workflows/wist-rollout-sample-smoke.yml")
LITERAL_BLOCK_WORKFLOW = Path(".github/workflows/ownership-package-verification.yml")


def run(checker: Path, root: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(checker), "--root", str(root)],
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
    )


def fixture(source: Path, destination: Path) -> None:
    for relative in (WORKFLOW, LITERAL_BLOCK_WORKFLOW):
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
    checker = source / "Tools/check-workflow-commands.py"

    with tempfile.TemporaryDirectory(prefix="workflow-command-mutants-") as temporary:
        base = Path(temporary) / "baseline"
        fixture(source, base)
        result = run(checker, base)
        if result.returncode != 0:
            raise AssertionError(f"baseline must pass before mutating:\n{result.stdout}")

        # 1. Exactly what broke the rollout-sample job: deleting an argument left a whitespace-only
        #    line, which YAML treats as the end of the folded block.
        split_folded = Path(temporary) / "split-folded"
        shutil.copytree(base, split_folded)
        path = split_folded / WORKFLOW
        text = path.read_text(encoding="utf-8")
        anchor = "          -p:UseSharedCompilation=false\n          --verbosity quiet"
        if anchor not in text:
            raise AssertionError("anchor for the folded-block split was not found")
        path.write_text(
            text.replace(anchor, "          -p:UseSharedCompilation=false\n         \n          --verbosity quiet", 1),
            encoding="utf-8",
        )
        expect_rejected(checker, split_folded, "whitespace-only line splits a folded run block")

        # 2. A `run` block that begins with a detached option group - the resulting shell error.
        detached = Path(temporary) / "detached-options"
        shutil.copytree(base, detached)
        path = detached / WORKFLOW
        text = path.read_text(encoding="utf-8")
        anchor = "      - name: Prepare optional local NuGet feed\n        run: mkdir -p UniversalToolchain/packages"
        if anchor not in text:
            raise AssertionError("anchor for the detached-option mutant was not found")
        path.write_text(
            text.replace(
                anchor,
                "      - name: Prepare optional local NuGet feed\n        run: |\n          --verbosity quiet",
                1,
            ),
            encoding="utf-8",
        )
        expect_rejected(checker, detached, "run block starting with a bare option")

        # 3. A `run:` that lost its body entirely, which is what a careless edit to a step looks
        #    like and which no YAML parser would flag.
        empty = Path(temporary) / "empty-run"
        shutil.copytree(base, empty)
        path = empty / WORKFLOW
        text = path.read_text(encoding="utf-8")
        anchor = "      - name: Prepare optional local NuGet feed\n        run: mkdir -p UniversalToolchain/packages"
        if anchor not in text:
            raise AssertionError("anchor for the empty-run mutant was not found")
        path.write_text(
            text.replace(anchor, "      - name: Prepare optional local NuGet feed\n        run:", 1),
            encoding="utf-8",
        )
        expect_rejected(checker, empty, "run block without a body")

    print("workflow-command mutants rejected")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
