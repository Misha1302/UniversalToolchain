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

import run_matrix as base

BASELINE_FAILURES: dict[str, dict[str, str]] = {}


def _group(records: list[dict[str, Any]], case_id: str, policy: str) -> list[dict[str, Any]]:
    return [record for record in records if record["caseId"] == case_id and record["policy"] == policy]


def _validate_baseline_failure(record: dict[str, Any], case_id: str, policy: str) -> None:
    expected = BASELINE_FAILURES[case_id]
    if record["faultInjected"] or record["mutationId"] != "none":
        raise ValueError(f"baseline failure was contaminated by fault injection: {case_id}/{policy}")
    if record["classification"] != expected["classification"]:
        raise ValueError(f"baseline failure classification changed: {case_id}/{policy}: {base.stable_signature(record)}")
    if record["firstDetectionBoundary"] != expected["firstDetectionBoundary"]:
        raise ValueError(f"baseline failure boundary changed: {case_id}/{policy}")
    if record["diagnosticCodes"] != [expected["diagnosticCode"]]:
        raise ValueError(f"baseline failure diagnostic changed: {case_id}/{policy}: {record['diagnosticCodes']}")
    message = record["infrastructureError"] or ""
    if expected["messageFragment"] not in message:
        raise ValueError(f"baseline failure message changed: {case_id}/{policy}")
    if record["actualResult"] is not None:
        raise ValueError(f"baseline failure unexpectedly produced a result: {case_id}/{policy}")


def validate(records: list[dict[str, Any]]) -> None:
    expected_count = len(base.CASE_IDS) * len(base.POLICIES) * base.REPETITIONS
    if len(records) != expected_count:
        raise ValueError(f"expected {expected_count} records, found {len(records)}")

    keys = [(record["caseId"], record["policy"], record["repetition"]) for record in records]
    if len(keys) != len(set(keys)):
        raise ValueError("duplicate case/policy/repetition record")

    for case_id in base.CASE_IDS:
        for policy in base.POLICIES:
            group = _group(records, case_id, policy)
            if len(group) != base.REPETITIONS:
                raise ValueError(f"missing repetitions for {case_id}/{policy}")
            if len({base.stable_signature(record) for record in group}) != 1:
                raise ValueError(f"fresh-process classification is unstable for {case_id}/{policy}")

            record = group[0]
            if case_id in BASELINE_FAILURES:
                _validate_baseline_failure(record, case_id, policy)
                continue

            expectation = base.FAULT_EXPECTATIONS.get(case_id)
            if expectation is None:
                if record["classification"] != "accepted" or record["actualResult"] is None:
                    raise ValueError(f"valid case rejected or failed: {case_id}/{policy}: {base.stable_signature(record)}")
                if abs(float(record["actualResult"]) - float(record["expectedResult"])) > 1e-9:
                    raise ValueError(f"valid result mismatch: {case_id}/{policy}")
                continue

            if policy in ("P0_STRUCTURAL", "P1_INVALIDATION"):
                if record["classification"] != expectation:
                    raise ValueError(
                        f"no-protocol symptom mismatch: {case_id}/{policy}; expected={expectation}; actual={base.stable_signature(record)}"
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
                    raise ValueError(f"protocol failed to reject: {case_id}/{policy}: {base.stable_signature(record)}")
                if record["firstDetectionBoundary"] != base.EXPECTED_BOUNDARY:
                    raise ValueError(f"protocol rejection boundary mismatch: {case_id}/{policy}")
                if base.EXPECTED_CAPABILITY_CODE not in record["diagnosticCodes"]:
                    raise ValueError(f"capability diagnostic missing: {case_id}/{policy}")
                if record["infrastructureError"] is not None:
                    raise ValueError(f"protocol rejection misclassified as infrastructure failure: {case_id}/{policy}")

        selective = _group(records, case_id, "P2_SELECTIVE")[0]
        always = _group(records, case_id, "P3_ALWAYS")[0]
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
    catalog: list[dict[str, Any]] = []
    for case_id in base.CASE_IDS:
        record = next(item for item in records if item["caseId"] == case_id)
        is_fault = case_id in base.FAULT_EXPECTATIONS
        is_baseline_failure = case_id in BASELINE_FAILURES
        catalog.append(
            {
                "caseId": case_id,
                "stratum": record["stratum"],
                "source": record["source"],
                "arguments": record["arguments"],
                "presetId": record["presetId"],
                "backend": record["backend"],
                "expectedResult": record["expectedResult"],
                "caseRole": "targeted-fault" if is_fault else ("baseline-runtime-failure" if is_baseline_failure else "valid-control"),
                "faultInjected": is_fault,
                "mutationId": record["mutationId"],
                "expectedNoProtocolBehavior": base.FAULT_EXPECTATIONS.get(
                    case_id,
                    BASELINE_FAILURES.get(case_id, {}).get("classification", "accepted"),
                ),
                "expectedDetectionBoundary": (
                    base.EXPECTED_BOUNDARY
                    if is_fault
                    else BASELINE_FAILURES.get(case_id, {}).get("firstDetectionBoundary", "result")
                ),
                "expectedDiagnosticFamily": (
                    base.EXPECTED_CAPABILITY_CODE
                    if is_fault
                    else BASELINE_FAILURES.get(case_id, {}).get("diagnosticCode")
                ),
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
    run_id = f"cgo27-e2e-{os.getpid()}"
    commit = resolve_commit()

    records: list[dict[str, Any]] = []
    raw_path = output / "raw-results.jsonl"
    with raw_path.open("w", encoding="utf-8") as raw:
        for case_id in base.CASE_IDS:
            for policy in base.POLICIES:
                for repetition in range(1, base.REPETITIONS + 1):
                    record = base.run_child(dll, case_id, policy, repetition, run_id)
                    records.append(record)
                    raw.write(json.dumps(record, sort_keys=True, separators=(",", ":")) + "\n")
                    raw.flush()

    catalog = case_catalog(records)
    (output / "cases.json").write_text(json.dumps(catalog, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    (output / "prevalidation-summary.json").write_text(
        json.dumps(
            {
                "schemaVersion": 2,
                "runId": run_id,
                "commitSha": commit,
                "recordsCollected": len(records),
                "faultExpectations": base.FAULT_EXPECTATIONS,
                "baselineRuntimeFailures": BASELINE_FAILURES,
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
        for policy in base.POLICIES
    }
    summary = {
        "schemaVersion": 2,
        "runId": run_id,
        "commitSha": commit,
        "status": "VALIDATED",
        "cases": len(base.CASE_IDS),
        "strata": dict(Counter(item["stratum"] for item in catalog)),
        "targetedFaultCases": len(base.FAULT_EXPECTATIONS),
        "validControlCases": len(base.CASE_IDS) - len(base.FAULT_EXPECTATIONS) - len(BASELINE_FAILURES),
        "baselineRuntimeFailureCases": sorted(BASELINE_FAILURES),
        "freshProcessRepetitions": base.REPETITIONS,
        "rawRecords": len(records),
        "policyOutcomes": by_policy,
        "p2P3ParityCases": len(base.CASE_IDS),
        "externallyAuthored": False,
        "corpusLabel": "model-authored-exploratory",
        "claimBoundary": "Source-to-result and fresh-process reproducible; all 25 non-fault programs are valid controls after the independently tracked numeric-promotion repair.",
    }
    (output / "summary.json").write_text(json.dumps(summary, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    (output / "environment.json").write_text(
        json.dumps(
            {
                "schemaVersion": 2,
                "commitSha": commit,
                "python": sys.version,
                "platform": platform.platform(),
                "processor": platform.processor(),
                "policies": base.POLICIES,
                "repetitions": base.REPETITIONS,
                "baselineRuntimeFailures": BASELINE_FAILURES,
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
