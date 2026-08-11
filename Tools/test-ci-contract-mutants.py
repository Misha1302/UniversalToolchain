#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path


def run_checker(checker: Path, root: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(checker), "--root", str(root)],
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
    )


def copy_fixture(source: Path, destination: Path) -> None:
    (destination / "eng").mkdir(parents=True)
    (destination / "Tools").mkdir(parents=True)
    (destination / ".github/workflows").mkdir(parents=True)
    shutil.copy2(source / "eng/ci-required-workflows.json", destination / "eng/ci-required-workflows.json")
    shutil.copy2(source / ".github/workflows/ci-aggregate.yml", destination / ".github/workflows/ci-aggregate.yml")
    data = json.loads((source / "eng/ci-required-workflows.json").read_text(encoding="utf-8"))
    for item in [*data["requiredForCodeAcceptance"], *data.get("nonBlockingWorkflows", [])]:
        relative = Path(item["workflow"])
        target = destination / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source / relative, target)


def expect_failure(checker: Path, root: Path, label: str) -> None:
    result = run_checker(checker, root)
    if result.returncode == 0:
        raise AssertionError(f"{label}: checker accepted mutant\n{result.stdout}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    source = args.root.resolve()
    checker = source / "Tools/check-ci-contract.py"

    with tempfile.TemporaryDirectory(prefix="wist-ci-contract-") as temporary:
        baseline = Path(temporary) / "baseline"
        copy_fixture(source, baseline)
        baseline_result = run_checker(checker, baseline)
        if baseline_result.returncode != 0:
            raise AssertionError(f"baseline CI contract failed\n{baseline_result.stdout}")

        missing = Path(temporary) / "missing"
        shutil.copytree(baseline, missing)
        owner = json.loads((missing / "eng/ci-required-workflows.json").read_text(encoding="utf-8"))
        removed = missing / owner["requiredForCodeAcceptance"][0]["workflow"]
        removed.unlink()
        expect_failure(checker, missing, "missing required workflow")

        fail_open = Path(temporary) / "fail-open"
        shutil.copytree(baseline, fail_open)
        owner_path = fail_open / "eng/ci-required-workflows.json"
        owner = json.loads(owner_path.read_text(encoding="utf-8"))
        owner["allowedConclusions"] = ["success", "skipped"]
        owner_path.write_text(json.dumps(owner, indent=2) + "\n", encoding="utf-8")
        expect_failure(checker, fail_open, "fail-open skipped conclusion")

        drift = Path(temporary) / "drift"
        shutil.copytree(baseline, drift)
        aggregate = drift / ".github/workflows/ci-aggregate.yml"
        aggregate.write_text(
            aggregate.read_text(encoding="utf-8").replace("eng/ci-required-workflows.json", "eng/other.json"),
            encoding="utf-8",
        )
        expect_failure(checker, drift, "aggregate owner drift")

    print("CI contract mutants rejected")


if __name__ == "__main__":
    main()
