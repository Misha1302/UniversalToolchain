#!/usr/bin/env python3
"""Seeded negative tests for the exact test-count and timeout contract."""
from __future__ import annotations

import argparse
import copy
import importlib.util
import json
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"cannot load {name} from {path}")
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


def expect_rejected(label: str, action, accepted_exceptions: tuple[type[BaseException], ...]) -> None:
    try:
        action()
    except accepted_exceptions:
        print(f"SURVIVOR=0 mutant={label}")
    else:
        raise RuntimeError(f"seeded test-contract mutant survived: {label}")


def make_sleeping_dotnet(root: Path) -> str:
    # run_entry invokes `<dotnet> vstest ...`; using the current Python interpreter
    # with a temporary `vstest` script gives a portable sleeping executable on
    # Linux and Windows without relying on shell command-file semantics.
    (root / "vstest").write_text("import time\ntime.sleep(5)\n", encoding="utf-8")
    return sys.executable


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default=str(Path(__file__).resolve().parents[1]))
    parser.add_argument("--manifest", default="eng/test-counts.json")
    parser.add_argument("--results-directory", default="artifacts/test-contract")
    args = parser.parse_args()

    root = Path(args.root).resolve()
    runner = load_module("test_contract_runner", root / "Tools" / "run-test-contract.py")
    verifier = load_module("trx_verifier", root / "Tools" / "verify_trx_count.py")
    document = json.loads((root / args.manifest).read_text(encoding="utf-8"))
    entries = runner.validate_manifest(document)
    results = (root / args.results_directory).resolve()
    core = results / "Core.trx"
    if not core.is_file():
        raise RuntimeError(f"canonical Core TRX is missing: {core}")

    wrong_total = copy.deepcopy(document)
    wrong_total["totalPassed"] += 1
    expect_rejected(
        "test-contract-total-drift",
        lambda: runner.validate_manifest(wrong_total),
        (runner.ContractError,),
    )

    wrong_count = int(document["main"][0]["expectedPassed"]) + 1
    expect_rejected(
        "trx-deleted-test-count",
        lambda: verifier.verify_trx(core, wrong_count, "Core-mutant"),
        (ValueError,),
    )

    invalid_build_type = copy.deepcopy(document)
    invalid_build_type["main"][0]["buildBeforeTest"] = "true"
    expect_rejected(
        "test-contract-build-before-test-type",
        lambda: runner.validate_manifest(invalid_build_type),
        (runner.ContractError,),
    )

    invalid_build_runner = copy.deepcopy(document)
    invalid_build_runner["main"][0]["buildBeforeTest"] = True
    expect_rejected(
        "test-contract-build-before-test-runner",
        lambda: runner.validate_manifest(invalid_build_runner),
        (runner.ContractError,),
    )

    with tempfile.TemporaryDirectory(prefix="test-contract-mutants-") as temp:
        temp_path = Path(temp)
        skipped = temp_path / "skipped.trx"
        tree = ET.parse(core)
        root_xml = tree.getroot()
        result = next(element for element in root_xml.iter() if element.tag.endswith("UnitTestResult"))
        result.set("outcome", "Skipped")
        tree.write(skipped, encoding="utf-8", xml_declaration=True)
        expect_rejected(
            "trx-skipped-outcome",
            lambda: verifier.verify_trx(skipped, int(document["main"][0]["expectedPassed"]), "Core-skipped-mutant"),
            (ValueError,),
        )

        fake_dotnet = make_sleeping_dotnet(temp_path)
        timeout_entry = {
            "label": "SeededTimeout",
            "documentationName": "SeededTimeout",
            "runner": "assembly",
            "path": "seeded-timeout.dll",
            "timeoutSeconds": 1,
            "expectedPassed": 1,
        }
        expect_rejected(
            "per-entry-timeout",
            lambda: runner.run_entry(temp_path, str(fake_dotnet), "Release", temp_path / "timeout-results", timeout_entry),
            (runner.ContractError,),
        )

    print(f"test-contract mutants rejected: 6; canonical entries={len(entries)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
