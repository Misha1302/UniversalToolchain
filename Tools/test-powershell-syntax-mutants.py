#!/usr/bin/env python3
"""Prove the PowerShell syntax gate fails when parse-breaking shapes are reintroduced.

The gate exists because a Linux-only verification cannot see `build.ps1` at all: a trailing comma
after the last array element passed every local check and broke the Windows job on `master`. A guard
that only ever runs green is indistinguishable from a guard that asserts nothing, so each mutant
below reintroduces one real parse error and the gate must reject it.
"""
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

SCRIPTS = (
    Path("build.ps1"),
    Path("Tools/smoke-published-wist-package.ps1"),
)


def run(checker: Path, root: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(checker), "--root", str(root)],
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
    )


def fixture(source: Path, destination: Path) -> None:
    for relative in SCRIPTS:
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
    checker = source / "Tools/check-powershell-syntax.py"

    with tempfile.TemporaryDirectory(prefix="powershell-syntax-mutants-") as temporary:
        base = Path(temporary) / "baseline"
        fixture(source, base)
        result = run(checker, base)
        if result.returncode != 0:
            raise AssertionError(f"baseline must pass before mutating:\n{result.stdout}")

        # 1. The exact regression that reached master: a separator left behind after deleting the last
        #    element of an argument array.
        dangling = Path(temporary) / "dangling-separator"
        shutil.copytree(base, dangling)
        path = dangling / "build.ps1"
        text = path.read_text(encoding="utf-8-sig")
        anchor = '        "-p:UseSharedCompilation=$sharedCompilation"\n    ))'
        if anchor not in text:
            raise AssertionError("mutation anchor for the trailing separator was not found")
        path.write_text(
            text.replace(anchor, '        "-p:UseSharedCompilation=$sharedCompilation",\n    ))', 1),
            encoding="utf-8-sig",
        )
        expect_rejected(checker, dangling, "trailing comma before a closing bracket")

        # 2. An unclosed bracket, which a parser rejects and a textual review routinely misses.
        unclosed = Path(temporary) / "unclosed-bracket"
        shutil.copytree(base, unclosed)
        path = unclosed / "Tools/smoke-published-wist-package.ps1"
        text = path.read_text(encoding="utf-8-sig")
        path.write_text(text + "\n$unclosed = @(\n    'only-element'\n", encoding="utf-8-sig")
        expect_rejected(checker, unclosed, "unclosed bracket")

        # 3. An unterminated string, which silently swallows the rest of the file for a human reader.
        unterminated = Path(temporary) / "unterminated-string"
        shutil.copytree(base, unterminated)
        path = unterminated / "build.ps1"
        text = path.read_text(encoding="utf-8-sig")
        path.write_text(text + "\n$broken = 'never closed\n", encoding="utf-8-sig")
        expect_rejected(checker, unterminated, "unterminated string")

    print("powershell-syntax mutants rejected")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
