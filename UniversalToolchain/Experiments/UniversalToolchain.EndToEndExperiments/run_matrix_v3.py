#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import platform
import sys
from collections import Counter
from pathlib import Path
from typing import Any

import run_matrix as process_support

SCHEMA_VERSION = 3
POLICIES = (
    "P0_STRUCTURAL",
    "P1_INVALIDATION",
    "P1D_DEMAND_RECOMPUTATION",
    "P2_SELECTIVE",
    "P3_ALWAYS",
)
HISTORICAL_CASE_IDS = tuple(
    [f"C{i:02d}" for i in range(1, 11)]
    + [f"P{i:02d}" for i in range(1, 11)]
    + [f"B{i:02d}" for i in range(1, 11)]
)
DEMAND_CASE_IDS = ("D01", "D02")
CASE_IDS = HISTORICAL_CASE_IDS + DEMAND_CASE_IDS
FAULT_EXPECTATIONS = {
    "C01": "wrong-result",
    "C02": "wrong-result",
    "P01": "wrong-result",
    "P02": "late-failure",
    "B01": "late-failure",
    "D01": "wrong-result",
    "D02": "wrong-result",
}
EXPLICIT_DEMAND_CASES = {"D01"}
REPETITIONS = 2
EXPECTED_BOUNDARY = "optimized AIR contract verification"
EXPECTED_CAPABILITY_CODE = "UT-AIR-CAPABILITY-001"


def group(records: list[dict[str, Any]], case_id: str, policy: str) -> list[dict[str, Any]]:
    return [record for record in records if record["caseId"] == case_id and record["policy"] == policy]


def stable_signature(record: dict[str, Any]) -> str:
    return json.dumps(
        {
            "classification": record["classification"],
            "firstDetectionBoundary": record["firstDetectionBoundary"],
            "diagnosticCodes": record["diagnosticCodes"],
            "actualResult": record["actualResult"],
            "infrastructureErrorType": None if record["infrastructureError"] is None else record["diagnosticCodes"],
        },
        sort_keys=True,
        separators=(",", ":"),
    )


def policy_should_detect(case_id: str, policy: str) -> bool:
    if case_id not in FAULT_EXPECTATIONS:
        return False
    if policy in ("P2_SELECTIVE", "P3_ALWAYS"):
        return True
    return policy == "P1D_DEMAND_RECOMPUTATION" and case_id in EXPLICIT_DEMAND_CASES


def validate(records: list[dict[str, Any]]) -> None:
    expected_count = len(CASE_IDS) * len(POLICIES) * REPETITIONS
    if len(records) != expected_count:
        raise ValueError(f"expected {expected_count} records, found {len(records)}")

    keys = [(record["caseId"], record["policy"], record["repetition"]) for record in records]
    if len(keys) != len(set(keys)):
        raise ValueError("duplicate case/policy/repetition record")

    for case_id in CASE_IDS:
        for policy in POLICIES:
            case_records = group(records, case_id, policy)
            if len(case_records) != REPETITIONS:
                raise ValueError(f"missing repetitions for {case_id}/{policy}")
            if len({stable_signature(record) for record in case_records}) != 1:
                raise ValueError(f"fresh-process classification is unstable for {case_id}/{policy}")

            record = case_records[0]
            if record["schemaVersion"] != SCHEMA_VERSION:
                raise ValueError(f"schema mismatch for {case_id}/{policy}: {record['schemaVersion']}")
            if bool(record["demandQuery"]) != (case_id in EXPLICIT_DEMAND_CASES):
                raise ValueError(f"demand-query accounting mismatch for {case_id}/{policy}")

            expectation = FAULT_EXPECTATIONS.get(case_id)
            if expectation is None:
                if record["classification"] != "accepted" or record["actualResult"] is None:
                    raise ValueError(f"valid case rejected or failed: {case_id}/{policy}: {stable_signature(record)}")
                if abs(float(record["actualResult"]) - float(record["expectedResult"])) > 1e-9:
                    raise ValueError(f"valid result mismatch: {case_id}/{policy}")
                continue

            if policy_should_detect(case_id, policy):
                if record["classification"] != "rejected":
                    raise ValueError(f"protocol failed to reject: {case_id}/{policy}: {stable_signature(record)}")
                if record["firstDetectionBoundary"] != EXPECTED_BOUNDARY:
                    raise ValueError(f"protocol rejection boundary mismatch: {case_id}/{policy}")
                if EXPECTED_CAPABILITY_CODE not in record["diagnosticCodes"]:
                    raise ValueError(f"capability diagnostic missing: {case_id}/{policy}")
                if record["infrastructureError"] is not None:
                    raise ValueError(f"protocol rejection misclassified as infrastructure failure: {case_id}/{policy}")
                continue

            if record["classification"] != expectation:
                raise ValueError(
                    f"no-protocol symptom mismatch: {case_id}/{policy}; "
                    f"expected={expectation}; actual={stable_signature(record)}"
                )
            if expectation == "wrong-result":
                if record["actualResult"] is None or abs(float(record["actualResult"]) - 1.0) > 1e-9:
                    raise ValueError(f"wrong-result mutation was not observed: {case_id}/{policy}")
            else:
                if record["firstDetectionBoundary"] != "runtime-or-backend":
                    raise ValueError(f"late failure occurred at unexpected boundary: {case_id}/{policy}")

        selective = group(records, case_id, "P2_SELECTIVE")[0]
        always = group(records, case_id, "P3_ALWAYS")[0]
        if (
            selective["classification"],
            selective["firstDetectionBoundary"],
            selective["diagnosticCodes"],
            selective["actualResult"],
        ) != (
            always["classification"],
            always["firstDetectionBoundary"],
            always["diagnosticCodes"],
            always["actualResult"],
        ):
            raise ValueError(f"P2/P3 parity mismatch for {case_id}")


def build_catalog(records: list[dict[str, Any]]) -> list[dict[str, Any]]:
    catalog: list[dict[str, Any]] = []
    for case_id in CASE_IDS:
        record = next(item for item in records if item["caseId"] == case_id)
        is_fault = case_id in FAULT_EXPECTATIONS
        catalog.append(
            {
                "caseId": case_id,
                "studySet": record["studySet"],
                "stratum": record["stratum"],
                "source": record["source"],
                "arguments": record["arguments"],
                "presetId": record["presetId"],
                "backend": record["backend"],
                "expectedResult": record["expectedResult"],
                "caseRole": "demand-baseline-fault" if case_id in DEMAND_CASE_IDS else ("targeted-fault" if is_fault else "valid-control"),
                "faultInjected": is_fault,
                "mutationId": record["mutationId"],
                "explicitDemand": case_id in EXPLICIT_DEMAND_CASES,
                "expectedNoProtocolBehavior": FAULT_EXPECTATIONS.get(case_id, "accepted"),
                "expectedDetectionBoundary": EXPECTED_BOUNDARY if is_fault else "result",
                "expectedDiagnosticFamily": EXPECTED_CAPABILITY_CODE if is_fault else None,
                "replay": f"dotnet UniversalToolchain.EndToEndExperiments.dll --child {case_id} <POLICY> <REPETITION> <SEED> <RUN_ID>",
            }
        )
    return catalog


def resolve_commit() -> str:
    commit = (
        os.environ.get("CGO27_SOURCE_SHA")
        or os.environ.get("CGO27_EXPERIMENT_COMMIT")
        or os.environ.get("GITHUB_SHA")
        or ""
    )
    if len(commit) != 40 or any(character not in "0123456789abcdef" for character in commit):
        raise ValueError("exact 40-hex source commit is required")
    return commit


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("dll", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    dll = args.dll.resolve()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    run_id = f"cgo27-e2e-v3-{os.getpid()}"
    commit = resolve_commit()

    records: list[dict[str, Any]] = []
    raw_path = output / "raw-results.jsonl"
    with raw_path.open("w", encoding="utf-8") as raw:
        for case_id in CASE_IDS:
            for policy in POLICIES:
                for repetition in range(1, REPETITIONS + 1):
                    record = process_support.run_child(dll, case_id, policy, repetition, run_id)
                    records.append(record)
                    raw.write(json.dumps(record, sort_keys=True, separators=(",", ":")) + "\n")
                    raw.flush()

    catalog = build_catalog(records)
    (output / "cases.json").write_text(json.dumps(catalog, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    (output / "prevalidation-summary.json").write_text(
        json.dumps(
            {
                "schemaVersion": SCHEMA_VERSION,
                "runId": run_id,
                "commitSha": commit,
                "recordsCollected": len(records),
                "historicalV2Cases": len(HISTORICAL_CASE_IDS),
                "demandV3Cases": len(DEMAND_CASE_IDS),
                "faultExpectations": FAULT_EXPECTATIONS,
                "explicitDemandCases": sorted(EXPLICIT_DEMAND_CASES),
                "status": "COLLECTED_NOT_YET_VALIDATED",
            },
            indent=2,
            sort_keys=True,
        ) + "\n",
        encoding="utf-8",
    )

    validate(records)
    by_policy = {
        policy: dict(Counter(record["classification"] for record in records if record["policy"] == policy))
        for policy in POLICIES
    }
    summary = {
        "schemaVersion": SCHEMA_VERSION,
        "runId": run_id,
        "commitSha": commit,
        "status": "VALIDATED",
        "cases": len(CASE_IDS),
        "historicalV2Cases": len(HISTORICAL_CASE_IDS),
        "demandV3Cases": len(DEMAND_CASE_IDS),
        "strata": dict(Counter(item["stratum"] for item in catalog)),
        "targetedFaultCases": len(FAULT_EXPECTATIONS),
        "validControlCases": len(CASE_IDS) - len(FAULT_EXPECTATIONS),
        "freshProcessRepetitions": REPETITIONS,
        "rawRecords": len(records),
        "policyOutcomes": by_policy,
        "p2P3ParityCases": len(CASE_IDS),
        "demandBaseline": {
            "queriedCase": "D01",
            "unqueriedCase": "D02",
            "p1dQueriedClassification": group(records, "D01", "P1D_DEMAND_RECOMPUTATION")[0]["classification"],
            "p1dUnqueriedClassification": group(records, "D02", "P1D_DEMAND_RECOMPUTATION")[0]["classification"],
        },
        "p07Status": {
            policy: group(records, "P07", policy)[0]["classification"] for policy in POLICIES
        },
        "externallyAuthored": False,
        "corpusLabel": "model-authored-exploratory",
        "claimBoundary": (
            "Source-to-result and fresh-process reproducible. The historical 30-case denominator is retained, "
            "the two demand cases are reported separately, and the corpus is not externally authored."
        ),
    }
    (output / "summary.json").write_text(json.dumps(summary, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    (output / "environment.json").write_text(
        json.dumps(
            {
                "schemaVersion": SCHEMA_VERSION,
                "commitSha": commit,
                "python": sys.version,
                "platform": platform.platform(),
                "processor": platform.processor(),
                "policies": POLICIES,
                "repetitions": REPETITIONS,
            },
            indent=2,
            sort_keys=True,
        ) + "\n",
        encoding="utf-8",
    )
    print("CGO27_END_TO_END_SUMMARY=" + json.dumps(summary, sort_keys=True, separators=(",", ":")))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
