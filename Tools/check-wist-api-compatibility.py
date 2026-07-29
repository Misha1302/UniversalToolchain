#!/usr/bin/env python3
from __future__ import annotations

import argparse
import csv
import hashlib
import json
import sys
import zipfile
from pathlib import Path, PurePosixPath

ALLOWED = {"intentional_break", "intentional_addition", "compatible", "retained"}
REQUIRED = {
    "WistBackend", "WistPreset", "WistEngineOptions.Backend", "WistEngineOptions.Preset",
    "WistEngineOptions.EnableCompilationCache", "WistValidationResult.Message",
    "UniversalToolchain.Dialects.Wist.Facade.*",
    "WistDialectExecutionConfiguration/Host/Workflow and compatibility backend extensions",
    "UniversalToolchain.Dialects.Integration.ToolchainRuntimeHost",
}


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def read_lines(path: Path) -> set[str]:
    if not path.is_file():
        raise ValueError(f"API snapshot is missing: {path}")
    return {
        line.strip()
        for line in path.read_text(encoding="utf-8").splitlines()
        if line.strip() and not line.lstrip().startswith("#")
    }


def extract_parent_snapshot(archive: Path, suffix: str) -> bytes:
    normalized_suffix = PurePosixPath(suffix).as_posix()
    with zipfile.ZipFile(archive) as source:
        matches = [
            name for name in source.namelist()
            if PurePosixPath(name).as_posix().endswith("/" + normalized_suffix)
            or PurePosixPath(name).as_posix() == normalized_suffix
        ]
        if len(matches) != 1:
            raise ValueError(
                f"parent source archive must contain exactly one {normalized_suffix!r}; found {matches}"
            )
        return source.read(matches[0])


def validate_baseline_provenance(baseline: Path, provenance: Path, parent_archive: Path) -> None:
    data = json.loads(provenance.read_text(encoding="utf-8"))
    if data.get("schemaVersion") != 2:
        raise ValueError(f"unsupported API baseline provenance schema: {data.get('schemaVersion')!r}")
    expected_path = data.get("publicApiSnapshot")
    expected_digest = data.get("publicApiSnapshotSha256")
    if expected_path != "eng/wist-public-api-baseline.txt":
        raise ValueError(f"unexpected API baseline snapshot path: {expected_path!r}")
    if not isinstance(expected_digest, str) or len(expected_digest) != 64:
        raise ValueError("API baseline provenance has an invalid snapshot SHA-256")
    if not parent_archive.is_file():
        raise ValueError(f"previous source archive is missing: {parent_archive}")
    expected_archive_name = data.get("baselineSourceArchive")
    if expected_archive_name and parent_archive.name != expected_archive_name:
        raise ValueError(
            f"unexpected previous source archive name: expected {expected_archive_name!r}, "
            f"actual {parent_archive.name!r}"
        )
    expected_archive_digest = data.get("baselineSourceArchiveSha256")
    actual_archive_digest = sha256_file(parent_archive)
    if actual_archive_digest != expected_archive_digest:
        raise ValueError(
            "previous source archive SHA-256 mismatch; "
            f"expected={expected_archive_digest}, actual={actual_archive_digest}"
        )
    parent_api_path = data.get("baselineApiPathInArchive")
    if not isinstance(parent_api_path, str) or not parent_api_path:
        raise ValueError("API baseline provenance has no parent API path")
    parent_snapshot = extract_parent_snapshot(parent_archive, parent_api_path)
    checked_in = baseline.read_bytes()
    if parent_snapshot != checked_in:
        raise ValueError("checked-in API baseline differs from the actual previous source artifact")
    actual_digest = sha256_bytes(checked_in)
    if actual_digest != expected_digest:
        raise ValueError(
            "checked-in API baseline snapshot digest mismatch; "
            f"expected={expected_digest}, actual={actual_digest}"
        )


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
    if rows:
        if list(rows[0]) != expected_columns:
            raise ValueError(f"unexpected exact-delta columns in {delta_ledger}")
    else:
        with delta_ledger.open(encoding="utf-8") as stream:
            if next(csv.reader(stream)) != expected_columns:
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
    parser.add_argument("--baseline-source-archive", required=True)
    args = parser.parse_args(argv)
    try:
        baseline = Path(args.baseline)
        validate_baseline_provenance(baseline, Path(args.baseline_provenance), Path(args.baseline_source_archive))
        decisions = validate_decision_ledger(Path(args.path))
        removed, added = validate_exact_diff(baseline, Path(args.current), Path(args.deltas))
    except (OSError, ValueError, csv.Error, json.JSONDecodeError, zipfile.BadZipFile) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1
    print(f"Wist API/package compatibility OK: {decisions} decisions; exact delta removed={removed}, added={added}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
