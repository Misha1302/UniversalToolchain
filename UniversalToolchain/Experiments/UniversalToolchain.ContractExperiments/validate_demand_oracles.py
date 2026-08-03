#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
from collections import defaultdict
from pathlib import Path
from typing import Any

POLICIES = {
    "P0_STRUCTURAL",
    "P1_INVALIDATION",
    "P1D_DEMAND_RECOMPUTATION",
    "P2_SELECTIVE",
    "P3_ALWAYS",
}


def load_json(path: Path) -> dict[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"{path}: expected object")
    return value


def load_jsonl(path: Path) -> list[dict[str, Any]]:
    rows = [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]
    if not rows or not all(isinstance(row, dict) for row in rows):
        raise ValueError(f"{path}: expected non-empty object JSONL")
    return rows


def validate(rows: list[dict[str, Any]], oracle: dict[str, Any], catalog: Path) -> dict[str, Any]:
    if oracle.get("schema_version") != 4:
        raise ValueError("unsupported demand oracle schema")
    digest = hashlib.sha256(catalog.read_bytes()).hexdigest()
    if digest != oracle.get("catalog_sha256"):
        raise ValueError(f"demand catalog hash mismatch: {digest} != {oracle.get('catalog_sha256')}")

    cases = oracle.get("cases")
    if not isinstance(cases, list) or len(cases) != 2:
        raise ValueError("demand oracle must contain exactly two matched cases")
    by_id = {case["case_id"]: case for case in cases}
    if len(by_id) != len(cases):
        raise ValueError("duplicate demand oracle case")

    demand_rows = [row for row in rows if row.get("corpus_id") == oracle.get("corpus_id")]
    observed_ids = {row.get("case_id") for row in demand_rows}
    if observed_ids != set(by_id):
        raise ValueError(f"demand oracle/corpus mismatch: {sorted(observed_ids)} != {sorted(by_id)}")

    grouped: dict[tuple[str, str], list[dict[str, Any]]] = defaultdict(list)
    for row in demand_rows:
        case = by_id[row["case_id"]]
        policy = row.get("policy")
        if policy not in POLICIES:
            raise ValueError(f"unknown demand policy {policy!r}")
        for field in (
            "operator_id",
            "workload_stratum",
            "expected_outcome",
            "expected_diagnostic_family",
            "expected_boundary",
            "demand_query",
        ):
            if row.get(field) != case.get(field):
                raise ValueError(f"{row['case_id']}/{policy}: {field} mismatch")
        grouped[(row["case_id"], policy)].append(row)

    repetitions = oracle.get("repetitions")
    for case_id, case in by_id.items():
        expectations = case.get("policy_expectations")
        if not isinstance(expectations, dict) or set(expectations) != POLICIES:
            raise ValueError(f"{case_id}: incomplete policy expectations")
        for policy in sorted(POLICIES):
            policy_rows = grouped[(case_id, policy)]
            if len(policy_rows) != repetitions:
                raise ValueError(f"{case_id}/{policy}: expected {repetitions} rows, found {len(policy_rows)}")
            observed = {
                (
                    row.get("actual_outcome"),
                    row.get("actual_diagnostic_family"),
                    row.get("first_detection_boundary"),
                )
                for row in policy_rows
            }
            if len(observed) != 1:
                raise ValueError(f"{case_id}/{policy}: unstable outcome")
            if list(next(iter(observed))) != expectations[policy]:
                raise ValueError(
                    f"{case_id}/{policy}: observed={list(next(iter(observed)))} expected={expectations[policy]}"
                )

    controls = oracle.get("valid_controls")
    control_rows = [
        row for row in rows
        if row.get("case_kind") == "valid-control"
        and row.get("workload_stratum") == controls.get("workload_stratum")
    ]
    for policy in sorted(POLICIES):
        policy_rows = [row for row in control_rows if row.get("policy") == policy]
        if len(policy_rows) != controls.get("runs_per_policy"):
            raise ValueError(f"{policy}: clean-demand denominator mismatch")
        if any(
            row.get("actual_outcome") != controls.get("expected_outcome")
            or row.get("actual_diagnostic_family") != controls.get("expected_diagnostic_family")
            for row in policy_rows
        ):
            raise ValueError(f"{policy}: clean-demand false positive")

    return {
        "schema_version": 1,
        "cases": len(by_id),
        "catalog_sha256": digest,
        "matched_queried_unqueried_pair": True,
        "p1d_queried_detected": True,
        "p1d_unqueried_deferred": True,
        "p2_p3_pair_parity": True,
        "clean_demand_false_positives": 0,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("results", type=Path)
    parser.add_argument("--oracle", required=True, type=Path)
    parser.add_argument("--catalog", required=True, type=Path)
    parser.add_argument("--receipt", type=Path)
    args = parser.parse_args()
    receipt = validate(load_jsonl(args.results), load_json(args.oracle), args.catalog)
    text = json.dumps(receipt, indent=2, sort_keys=True) + "\n"
    if args.receipt:
        args.receipt.parent.mkdir(parents=True, exist_ok=True)
        args.receipt.write_text(text, encoding="utf-8")
    print(text, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
