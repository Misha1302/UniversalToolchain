#!/usr/bin/env python3
from __future__ import annotations

import argparse
import importlib.util
import json
import shutil
import tempfile
import zipfile
from pathlib import Path


def load_checker(root: Path):
    path = root / "Tools" / "check-wist-api-compatibility.py"
    spec = importlib.util.spec_from_file_location("api_checker", path)
    assert spec and spec.loader
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def make_parent_archive(path: Path, snapshot: bytes) -> None:
    with zipfile.ZipFile(path, "w") as archive:
        archive.writestr(
            "parent/UniversalToolchain/UniversalToolchain.Wist/PublicAPI.Shipped.txt",
            snapshot,
        )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default=str(Path(__file__).resolve().parents[1]))
    parser.add_argument("--baseline-source-archive", required=True)
    args = parser.parse_args()
    root = Path(args.root).resolve()
    parent = Path(args.baseline_source_archive).resolve()
    checker = load_checker(root)
    baseline = root / "eng/wist-public-api-baseline.txt"
    provenance = root / "eng/wist-api-baseline.json"
    checker.validate_baseline_provenance(baseline, provenance, parent)
    checker.validate_decision_ledger(root / "eng/wist-api-compatibility.csv")
    checker.validate_exact_diff(
        baseline,
        root / "UniversalToolchain/UniversalToolchain.Wist/PublicAPI.Shipped.txt",
        root / "eng/wist-api-deltas.csv",
    )
    with tempfile.TemporaryDirectory(prefix="wist-api-mutants-") as tmp_name:
        tmp = Path(tmp_name)
        current = tmp / "PublicAPI.Shipped.txt"
        shutil.copy2(root / "UniversalToolchain/UniversalToolchain.Wist/PublicAPI.Shipped.txt", current)
        current.write_text(current.read_text() + "\nmethod System.Void UniversalToolchain.Wist.SeededMutant.Forbidden()\n", encoding="utf-8")
        try:
            checker.validate_exact_diff(baseline, current, root / "eng/wist-api-deltas.csv")
        except ValueError:
            print("SURVIVOR=0 mutant=unclassified-public-api-addition")
        else:
            raise RuntimeError("unclassified public API addition survived")

        mutated_baseline = tmp / "wist-public-api-baseline.txt"
        mutated_current = tmp / "paired-current.txt"
        addition = b"\nmethod System.Void UniversalToolchain.Wist.SeededMutant.PairedForbidden()\n"
        mutated_baseline.write_bytes(baseline.read_bytes() + addition)
        mutated_current.write_bytes((root / "UniversalToolchain/UniversalToolchain.Wist/PublicAPI.Shipped.txt").read_bytes() + addition)
        mutated_provenance = tmp / "provenance.json"
        data = json.loads(provenance.read_text(encoding="utf-8"))
        data["publicApiSnapshotSha256"] = checker.sha256_bytes(mutated_baseline.read_bytes())
        mutated_provenance.write_text(json.dumps(data), encoding="utf-8")
        try:
            checker.validate_baseline_provenance(mutated_baseline, mutated_provenance, parent)
            checker.validate_exact_diff(mutated_baseline, mutated_current, root / "eng/wist-api-deltas.csv")
        except ValueError:
            print("SURVIVOR=0 mutant=paired-current-and-baseline-rewrite")
        else:
            raise RuntimeError("paired current+baseline public API rewrite survived")

        fake_parent = tmp / parent.name
        make_parent_archive(fake_parent, mutated_baseline.read_bytes())
        data["baselineSourceArchiveSha256"] = checker.sha256_file(fake_parent)
        mutated_provenance.write_text(json.dumps(data), encoding="utf-8")
        try:
            checker.validate_baseline_provenance(mutated_baseline, mutated_provenance, parent)
        except ValueError:
            print("SURVIVOR=0 mutant=fake-parent-provenance-with-genuine-parent")
        else:
            raise RuntimeError("fake parent provenance survived while the genuine parent artifact was supplied")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
