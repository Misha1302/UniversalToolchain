#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
from collections import defaultdict
from pathlib import Path
from typing import Any

HISTORICAL_POLICIES = {"P0_STRUCTURAL", "P1_INVALIDATION", "P2_SELECTIVE", "P3_ALWAYS"}
FAULT_CORPORA = {"primary", "challenge"}
EXPECTED_FAULT_CASES = 50
EXPECTED_REPETITIONS = 3


def load_json(path: Path) -> dict[str, Any]:
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as error:
        raise ValueError(f"{path}: invalid JSON: {error}") from error
    if not isinstance(payload, dict):
        raise ValueError(f"{path}: expected a JSON object")
    return payload


def load_jsonl(path: Path) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for line_number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        if not line.strip():
            continue
        try:
            row = json.loads(line)
        except json.JSONDecodeError as error:
            raise ValueError(f"{path}:{line_number}: invalid JSON: {error}") from error
        if not isinstance(row, dict):
            raise ValueError(f"{path}:{line_number}: expected an object")
        rows.append(row)
    if not rows:
        raise ValueError(f"{path}: no records")
    return rows


def validate(rows: list[dict[str, Any]], oracle: dict[str, Any], mutations: Path) -> dict[str, Any]:
    if oracle.get("schema_version") != 3:
        raise ValueError("unsupported oracle schema")
    cases = oracle.get("fault_cases")
    controls = oracle.get("valid_controls")
    if not isinstance(cases, list) or not isinstance(controls, dict):
        raise ValueError("oracle is missing fault_cases or valid_controls")
    if len(cases) != EXPECTED_FAULT_CASES:
        raise ValueError(f"expected {EXPECTED_FAULT_CASES} fault oracles, found {len(cases)}")

    expected_by_id: dict[str, dict[str, Any]] = {}
    for case in cases:
        if not isinstance(case, dict) or not isinstance(case.get("case_id"), str):
            raise ValueError("invalid oracle case")
        if case["case_id"] in expected_by_id:
            raise ValueError(f"duplicate oracle case {case['case_id']}")
        expected_by_id[case["case_id"]] = case

    catalog_sha256 = hashlib.sha256(mutations.read_bytes()).hexdigest()
    if catalog_sha256 != oracle.get("historical_catalog_sha256"):
        raise ValueError(
            f"mutation catalog hash mismatch: {catalog_sha256} != "
            f"{oracle.get('historical_catalog_sha256')}"
        )

    historical_rows = [
        row for row in rows
        if row.get("policy") in HISTORICAL_POLICIES
        and (
            row.get("corpus_id") in FAULT_CORPORA
            or (
                row.get("case_kind") == "valid-control"
                and row.get("workload_stratum") in controls.get("families", [])
            )
        )
    ]
    observed_fault_ids = {
        row.get("case_id") for row in historical_rows
        if row.get("case_kind") == "fault"
    }
    if observed_fault_ids != set(expected_by_id):
        missing = sorted(set(expected_by_id) - observed_fault_ids)
        extra = sorted(observed_fault_ids - set(expected_by_id))
        raise ValueError(f"oracle/corpus mismatch: missing={missing}, extra={extra}")

    grouped: dict[tuple[str, str], list[dict[str, Any]]] = defaultdict(list)
    for row in historical_rows:
        policy = row.get("policy")
        if policy not in HISTORICAL_POLICIES:
            raise ValueError(f"unknown historical policy {policy!r}")
        case_id = row.get("case_id")
        if not isinstance(case_id, str):
            raise ValueError("record without case_id")
        if row.get("case_kind") == "fault":
            expected = expected_by_id[case_id]
            for field in (
                "corpus_id",
                "operator_id",
                "workload_stratum",
                "expected_outcome",
                "expected_diagnostic_family",
                "expected_boundary",
            ):
                if row.get(field) != expected.get(field):
                    raise ValueError(
                        f"{case_id}/{policy}: {field}={row.get(field)!r}, "
                        f"oracle={expected.get(field)!r}"
                    )
            if row.get("corpus_id") not in FAULT_CORPORA:
                raise ValueError(f"{case_id}: invalid fault corpus")
            grouped[(case_id, policy)].append(row)
        elif row.get("case_kind") == "valid-control":
            if row.get("workload_stratum") not in controls.get("families", []):
                raise ValueError(f"{case_id}: control family absent from oracle")
            if row.get("expected_outcome") != controls.get("expected_outcome"):
                raise ValueError(f"{case_id}: control outcome oracle mismatch")
            if row.get("expected_diagnostic_family") != controls.get("expected_diagnostic_family"):
                raise ValueError(f"{case_id}: control diagnostic oracle mismatch")
        else:
            raise ValueError(f"{case_id}: unknown case_kind {row.get('case_kind')!r}")

    for case_id in sorted(expected_by_id):
        collapsed: dict[str, tuple[Any, Any, Any]] = {}
        for policy in HISTORICAL_POLICIES:
            policy_rows = grouped[(case_id, policy)]
            if len(policy_rows) != EXPECTED_REPETITIONS:
                raise ValueError(
                    f"{case_id}/{policy}: expected {EXPECTED_REPETITIONS} rows, "
                    f"found {len(policy_rows)}"
                )
            outcomes = {
                (
                    row.get("actual_outcome"),
                    row.get("actual_diagnostic_family"),
                    row.get("first_detection_boundary"),
                )
                for row in policy_rows
            }
            if len(outcomes) != 1:
                raise ValueError(f"{case_id}/{policy}: flaky outcome-diagnostic-boundary")
            collapsed[policy] = next(iter(outcomes))
        if collapsed["P2_SELECTIVE"] != collapsed["P3_ALWAYS"]:
            raise ValueError(
                f"{case_id}: P2/P3 parity mismatch: "
                f"{collapsed['P2_SELECTIVE']!r} != {collapsed['P3_ALWAYS']!r}"
            )

    oracle_sha256 = hashlib.sha256(
        json.dumps(oracle, sort_keys=True, separators=(",", ":")).encode("utf-8")
    ).hexdigest()
    return {
        "schema_version": 1,
        "fault_cases": len(expected_by_id),
        "catalog_sha256": catalog_sha256,
        "oracle_canonical_sha256": oracle_sha256,
        "p2_p3_outcome_diagnostic_boundary_parity": True,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("results", type=Path)
    parser.add_argument("--oracle", required=True, type=Path)
    parser.add_argument("--mutations", required=True, type=Path)
    parser.add_argument("--receipt", type=Path)
    args = parser.parse_args()

    receipt = validate(load_jsonl(args.results), load_json(args.oracle), args.mutations)
    serialized = json.dumps(receipt, indent=2, sort_keys=True) + "\n"
    if args.receipt:
        args.receipt.parent.mkdir(parents=True, exist_ok=True)
        args.receipt.write_text(serialized, encoding="utf-8")
    print(serialized, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
