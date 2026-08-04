#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import platform
import subprocess
import sys
from collections import Counter
from pathlib import Path
from typing import Any

POLICIES = ("P0_STRUCTURAL", "P1_INVALIDATION", "P2_SELECTIVE", "P3_ALWAYS")
CASE_IDS = tuple([f"C{i:02d}" for i in range(1, 11)] + [f"P{i:02d}" for i in range(1, 11)] + [f"B{i:02d}" for i in range(1, 11)])
FAULT_EXPECTATIONS = {
    "C01": "wrong-result",
    "C02": "wrong-result",
    "P01": "wrong-result",
    "P02": "late-failure",
    "B01": "late-failure",
}
REPETITIONS = 2
EXPECTED_BOUNDARY = "optimized AIR contract verification"
EXPECTED_CAPABILITY_CODE = "UT-AIR-CAPABILITY-001"


def resolve_commit() -> str:
    return (
        os.environ.get("CGO27_SOURCE_SHA")
        or os.environ.get("CGO27_EXPERIMENT_COMMIT")
        or os.environ.get("GITHUB_SHA")
        or "local-uncommitted"
    )


def run_child(dll: Path, case_id: str, policy: str, repetition: int, run_id: str) -> dict[str, Any]:
    seed = stable_seed(case_id, policy, repetition)
    completed = subprocess.run(
        ["dotnet", str(dll), "--child", case_id, policy, str(repetition), str(seed), run_id],
        check=False,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        timeout=120,
        env={**os.environ, "CGO27_EXPERIMENT_COMMIT": resolve_commit()},
    )
    if completed.returncode != 0:
        raise RuntimeError(f"child failed {case_id}/{policy}/r{repetition}: exit={completed.returncode}; stderr={completed.stderr}")
    lines = [line.strip() for line in completed.stdout.splitlines() if line.strip()]
    if not lines:
        raise RuntimeError(f"child emitted no record: {case_id}/{policy}/r{repetition}")
    record = json.loads(lines[-1])
    record["processExitCode"] = completed.returncode
    return record


def stable_seed(case_id: str, policy: str, repetition: int) -> int:
    value = 17
    for character in f"{case_id}|{policy}":
        value = ((value * 31) + ord(character)) & 0xFFFFFFFF
    value = ((value * 31) + repetition) & 0xFFFFFFFF
    return value - 0x100000000 if value >= 0x80000000 else value


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


def validate(records: list[dict[str, Any]]) -> None:
    expected_count = len(CASE_IDS) * len(POLICIES) * REPETITIONS
    if len(records) != expected_count:
        raise ValueError(f"expected {expected_count} records, found {len(records)}")

    keys = [(record["caseId"], record["policy"], record["repetition"]) for record in records]
    if len(keys) != len(set(keys)):
        raise ValueError("duplicate case/policy/repetition record")

    for case_id in CASE_IDS:
        for policy in POLICIES:
            group = [record for record in records if record["caseId"] == case_id and record["policy"] == policy]
            if len(group) != REPETITIONS:
                raise ValueError(f"missing repetitions for {case_id}/{policy}")
            if len({stable_signature(record) for record in group}) != 1:
                raise ValueError(f"fresh-process classification is unstable for {case_id}/{policy}")

            record = group[0]
            expectation = FAULT_EXPECTATIONS.get(case_id)
            if expectation is None:
                if record["classification"] != "accepted" or record["actualResult"] is None:
                    raise ValueError(f"valid case rejected or failed: {case_id}/{policy}: {stable_signature(record)}")
                if abs(float(record["actualResult"]) - float(record["expectedResult"])) > 1e-9:
                    raise ValueError(f"valid result mismatch: {case_id}/{policy}")
                continue

            if policy in ("P0_STRUCTURAL", "P1_INVALIDATION"):
                if record["classification"] != expectation:
                    raise ValueError(
                        f"no-protocol symptom mismatch: {case_id}/{policy}; expected={expectation}; actual={stable_signature(record)}"
                    )
                if expectation == "wrong-result":
                    if record["actualResult"] is None or abs(float(record["actualResult"]) - 1.0) > 1e-9:
                        raise ValueError(f"wrong-result mutation was not observed: {case_id}/{policy}")
                else:
                    if record["firstDetectionBoundary"] != "runtime-or-backend":
                        raise ValueError(f"late failure occurred at unexpected boundary: {case_id}/{policy}")
                    if not any(code.endswith("RuntimeExecutionException") for code in record["diagnosticCodes"]):
                        raise ValueError(f"late failure diagnostic family mismatch: {case_id}/{policy}")
            else:
                if record["classification"] != "rejected":
                    raise ValueError(f"protocol failed to reject: {case_id}/{policy}: {stable_signature(record)}")
                if record["firstDetectionBoundary"] != EXPECTED_BOUNDARY:
                    raise ValueError(f"protocol rejection boundary mismatch: {case_id}/{policy}")
                if EXPECTED_CAPABILITY_CODE not in record["diagnosticCodes"]:
                    raise ValueError(f"capability diagnostic missing: {case_id}/{policy}")
                if record["infrastructureError"] is not None:
                    raise ValueError(f"protocol rejection misclassified as infrastructure failure: {case_id}/{policy}")

        selective = next(record for record in records if record["caseId"] == case_id and record["policy"] == "P2_SELECTIVE")
        always = next(record for record in records if record["caseId"] == case_id and record["policy"] == "P3_ALWAYS")
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


def case_catalog(records: list[dict[str, Any]]) -> list[dict[str, Any]]:
    catalog = []
    for case_id in CASE_IDS:
        record = next(item for item in records if item["caseId"] == case_id)
        catalog.append(
            {
                "caseId": case_id,
                "stratum": record["stratum"],
                "source": record["source"],
                "arguments": record["arguments"],
                "presetId": record["presetId"],
                "backend": record["backend"],
                "expectedResult": record["expectedResult"],
                "faultInjected": case_id in FAULT_EXPECTATIONS,
                "mutationId": record["mutationId"],
                "expectedNoProtocolBehavior": FAULT_EXPECTATIONS.get(case_id, "accepted"),
                "expectedDetectionBoundary": EXPECTED_BOUNDARY if case_id in FAULT_EXPECTATIONS else "result",
                "expectedDiagnosticFamily": EXPECTED_CAPABILITY_CODE if case_id in FAULT_EXPECTATIONS else None,
                "replay": f"dotnet UniversalToolchain.EndToEndExperiments.dll --child {case_id} <POLICY> <REPETITION> <SEED> <RUN_ID>",
            }
        )
    return catalog


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("dll", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    dll = args.dll.resolve()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    run_id = f"cgo27-e2e-{os.getpid()}"

    records: list[dict[str, Any]] = []
    raw_path = output / "raw-results.jsonl"
    with raw_path.open("w", encoding="utf-8") as raw:
        for case_id in CASE_IDS:
            for policy in POLICIES:
                for repetition in range(1, REPETITIONS + 1):
                    record = run_child(dll, case_id, policy, repetition, run_id)
                    records.append(record)
                    raw.write(json.dumps(record, sort_keys=True, separators=(",", ":")) + "\n")
                    raw.flush()

    catalog = case_catalog(records)
    (output / "cases.json").write_text(json.dumps(catalog, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    (output / "prevalidation-summary.json").write_text(
        json.dumps(
            {
                "schemaVersion": 1,
                "runId": run_id,
                "recordsCollected": len(records),
                "faultExpectations": FAULT_EXPECTATIONS,
                "status": "COLLECTED_NOT_YET_VALIDATED",
            },
            indent=2,
            sort_keys=True,
        )
        + "\n",
        encoding="utf-8",
    )

    validate(records)
    by_policy = {
        policy: dict(Counter(record["classification"] for record in records if record["policy"] == policy))
        for policy in POLICIES
    }
    summary = {
        "schemaVersion": 1,
        "runId": run_id,
        "commitSha": resolve_commit(),
        "status": "VALIDATED",
        "cases": len(CASE_IDS),
        "strata": dict(Counter(item["stratum"] for item in catalog)),
        "faultCases": len(FAULT_EXPECTATIONS),
        "freshProcessRepetitions": REPETITIONS,
        "rawRecords": len(records),
        "policyOutcomes": by_policy,
        "p2P3ParityCases": len(CASE_IDS),
        "externallyAuthored": False,
        "corpusLabel": "model-authored-exploratory",
        "claimBoundary": "Source-to-result and fresh-process reproducible; not externally authored.",
    }
    (output / "summary.json").write_text(json.dumps(summary, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    (output / "environment.json").write_text(
        json.dumps(
            {
                "schemaVersion": 1,
                "python": sys.version,
                "platform": platform.platform(),
                "processor": platform.processor(),
                "policies": POLICIES,
                "repetitions": REPETITIONS,
            },
            indent=2,
            sort_keys=True,
        )
        + "\n",
        encoding="utf-8",
    )
    print("CGO27_END_TO_END_SUMMARY=" + json.dumps(summary, sort_keys=True, separators=(",", ":")))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
