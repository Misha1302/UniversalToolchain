#!/usr/bin/env python3
from __future__ import annotations

import argparse
import csv
import hashlib
import json
import sys
from pathlib import Path

ALLOWED = {"intentional_break", "intentional_addition", "compatible", "retained"}
REQUIRED = {
    "WistBackend", "WistPreset", "WistEngineOptions.Backend", "WistEngineOptions.Preset",
    "WistEngineOptions.EnableCompilationCache", "WistValidationResult.Message",
    "UniversalToolchain.Dialects.Wist.Facade.*",
    "WistDialectExecutionConfiguration/Host/Workflow and compatibility backend extensions",
    "UniversalToolchain.Dialects.Integration.ToolchainRuntimeHost",
}


def read_lines(path: Path) -> set[str]:
    if not path.is_file():
        raise ValueError(f"API snapshot is missing: {path}")
    return {line.strip() for line in path.read_text(encoding="utf-8").splitlines() if line.strip() and not line.lstrip().startswith('#')}



def validate_baseline_provenance(baseline: Path, provenance: Path) -> None:
    data = json.loads(provenance.read_text(encoding="utf-8"))
    if data.get("schemaVersion") != 1:
        raise ValueError(f"unsupported API baseline provenance schema: {data.get('schemaVersion')!r}")
    expected_path = data.get("publicApiSnapshot")
    expected_digest = data.get("publicApiSnapshotSha256")
    if expected_path != "eng/wist-public-api-baseline.txt":
        raise ValueError(f"unexpected API baseline snapshot path: {expected_path!r}")
    if not isinstance(expected_digest, str) or len(expected_digest) != 64:
        raise ValueError("API baseline provenance has an invalid snapshot SHA-256")
    actual_digest = hashlib.sha256(baseline.read_bytes()).hexdigest()
    if actual_digest != expected_digest:
        raise ValueError(
            "immutable API baseline snapshot does not match its previous-release provenance; "
            f"expected={expected_digest}, actual={actual_digest}"
        )
    archive_digest = data.get("baselineSourceArchiveSha256")
    if not isinstance(archive_digest, str) or len(archive_digest) != 64:
        raise ValueError("API baseline provenance has an invalid source-archive SHA-256")

def validate_decision_ledger(path: Path) -> int:
    with path.open(newline="", encoding="utf-8") as stream:
        rows = list(csv.DictReader(stream))
    if not rows:
        raise ValueError("compatibility ledger is empty")
    required_columns = {"symbol_or_asset", "old", "new", "classification", "intent", "migration", "verification"}
    if set(rows[0]) != required_columns:
        raise ValueError(f"unexpected columns: {sorted(rows[0])}")
    names: set[str] = set()
    for number, row in enumerate(rows, 2):
        for key in required_columns:
            if not row[key].strip():
                raise ValueError(f"{path}:{number}: empty {key}")
        if row["classification"] not in ALLOWED:
            raise ValueError(f"{path}:{number}: unsupported classification {row['classification']!r}")
        name = row["symbol_or_asset"]
        if name in names:
            raise ValueError(f"{path}:{number}: duplicate symbol/asset {name!r}")
        names.add(name)
        lower = " ".join(row.values()).lower()
        if "not classified" in lower or row["intent"].strip().lower() == "unknown":
            raise ValueError(f"{path}:{number}: unclassified compatibility decision")
    missing = sorted(REQUIRED - names)
    if missing:
        raise ValueError(f"missing required compatibility decisions: {missing}")
    return len(rows)


def validate_exact_diff(baseline: Path, current: Path, delta_ledger: Path) -> tuple[int, int]:
    previous = read_lines(baseline)
    actual = read_lines(current)
    required = {*(f"removed|{line}" for line in previous - actual), *(f"added|{line}" for line in actual - previous)}
    with delta_ledger.open(newline="", encoding="utf-8") as stream:
        rows = list(csv.DictReader(stream))
    expected_columns = ["change", "symbol", "classification", "intent", "migration", "verification"]
    if (rows and list(rows[0]) != expected_columns) or (not rows and next(csv.reader(delta_ledger.open(encoding='utf-8'))) != expected_columns):
        raise ValueError(f"unexpected exact-delta columns in {delta_ledger}")
    declared: set[str] = set()
    for number, row in enumerate(rows, 2):
        if row["change"] not in {"added", "removed"}:
            raise ValueError(f"{delta_ledger}:{number}: unsupported change {row['change']!r}")
        if row["classification"] not in ALLOWED:
            raise ValueError(f"{delta_ledger}:{number}: unsupported classification {row['classification']!r}")
        for key in expected_columns:
            if not row[key].strip():
                raise ValueError(f"{delta_ledger}:{number}: empty {key}")
        key = f"{row['change']}|{row['symbol']}"
        if key in declared:
            raise ValueError(f"{delta_ledger}:{number}: duplicate exact delta")
        declared.add(key)
    if declared != required:
        raise ValueError(
            "exact public API diff is not fully classified; "
            f"missing={sorted(required - declared)}; unexpected={sorted(declared - required)}"
        )
    return len(previous - actual), len(actual - previous)


def main(argv: list[str] | None = None) -> int:
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser()
    parser.add_argument("path", nargs="?", default=str(root / "eng" / "wist-api-compatibility.csv"))
    parser.add_argument("--baseline", default=str(root / "eng" / "wist-public-api-baseline.txt"))
    parser.add_argument("--current", default=str(root / "UniversalToolchain" / "UniversalToolchain.Wist" / "PublicAPI.Shipped.txt"))
    parser.add_argument("--deltas", default=str(root / "eng" / "wist-api-deltas.csv"))
    parser.add_argument("--baseline-provenance", default=str(root / "eng" / "wist-api-baseline.json"))
    args = parser.parse_args(argv)
    try:
        baseline = Path(args.baseline)
        validate_baseline_provenance(baseline, Path(args.baseline_provenance))
        decisions = validate_decision_ledger(Path(args.path))
        removed, added = validate_exact_diff(baseline, Path(args.current), Path(args.deltas))
    except (OSError, ValueError, csv.Error, json.JSONDecodeError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1
    print(f"Wist API/package compatibility OK: {decisions} decisions; exact delta removed={removed}, added={added}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
