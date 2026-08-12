#!/usr/bin/env python3
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

FILES = (
    Path("UniversalToolchain/UniversalToolchain.Runtime/LanguageRuntime.cs"),
    Path("UniversalToolchain/UniversalToolchain.Runtime/LanguageBuildRuntime.cs"),
    Path("UniversalToolchain/UniversalToolchain.Runtime/RuntimeConstructionFailure.cs"),
    Path("UniversalToolchain/UniversalToolchain.Wist/WistEngine.cs"),
    Path("UniversalToolchain/UniversalToolchain.Wist/WistFailureClassifier.cs"),
    Path("UniversalToolchain/UniversalToolchain.Wist/WistUserInputException.cs"),
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
    path.write_text(text.replace(old, new, 1), encoding="utf-8")


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
    checker = source / "Tools/check-hardening-contract.py"

    with tempfile.TemporaryDirectory(prefix="wist-hardening-mutants-") as temporary:
        baseline = Path(temporary) / "baseline"
        fixture(source, baseline)
        result = run(checker, baseline)
        if result.returncode != 0:
            raise AssertionError(f"baseline hardening contract failed\n{result.stdout}")

        masked = Path(temporary) / "masked-primary"
        shutil.copytree(baseline, masked)
        replace_once(
            masked / "UniversalToolchain/UniversalToolchain.Runtime/LanguageRuntime.cs",
            "RuntimeConstructionFailure.Rethrow(",
            "RuntimeConstructionFailure.DisabledRethrow(",
        )
        expect_rejected(checker, masked, "primary exception masking")

        hidden_capability = Path(temporary) / "hidden-capability"
        shutil.copytree(baseline, hidden_capability)
        replace_once(
            hidden_capability / "UniversalToolchain/UniversalToolchain.Runtime/LanguageRuntime.cs",
            "public static LanguageBuildRuntime Create(",
            "public static LanguageRuntime Create(",
        )
        expect_rejected(checker, hidden_capability, "runtime capability hole")

        internal_as_user = Path(temporary) / "internal-as-user"
        shutil.copytree(baseline, internal_as_user)
        replace_once(
            internal_as_user / "UniversalToolchain/UniversalToolchain.Wist/WistFailureClassifier.cs",
            "_ => WistFailureKind.Internal",
            "_ => WistFailureKind.UserInput",
        )
        expect_rejected(checker, internal_as_user, "internal fault converted to user input")

        broad_argument = Path(temporary) / "broad-argument-as-user"
        shutil.copytree(baseline, broad_argument)
        replace_once(
            broad_argument / "UniversalToolchain/UniversalToolchain.Wist/WistFailureClassifier.cs",
            "WistUserInputException => WistFailureKind.UserInput",
            "ArgumentException => WistFailureKind.UserInput",
        )
        expect_rejected(checker, broad_argument, "arbitrary argument fault converted to user input")

        swallowed_guard = Path(temporary) / "swallowed-internal"
        shutil.copytree(baseline, swallowed_guard)
        replace_once(
            swallowed_guard / "UniversalToolchain/UniversalToolchain.Wist/WistEngine.cs",
            "if (!WistFailureClassifier.IsStructuredResultFailure(kind))\n                throw;",
            "if (!WistFailureClassifier.IsStructuredResultFailure(kind))\n                kind = WistFailureKind.UserInput;",
        )
        expect_rejected(checker, swallowed_guard, "internal fault swallowed by Validate/TryCompile")

    print("HARDENING_MUTANTS=PASS")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (AssertionError, OSError) as error:
        print(f"HARDENING_MUTANTS=FAIL: {error}")
        raise SystemExit(1)
