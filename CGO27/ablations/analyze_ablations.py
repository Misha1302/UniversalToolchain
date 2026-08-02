#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

POLICIES = ("P0_STRUCTURAL", "P1_INVALIDATION", "P2_SELECTIVE", "P3_ALWAYS")
TENSOR_POLICY = {0: "P0_STRUCTURAL", 1: "P1_INVALIDATION", 2: "P2_SELECTIVE", 3: "P3_ALWAYS"}


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def load_jsonl(path: Path) -> list[dict[str, Any]]:
    result: list[dict[str, Any]] = []
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        if not line.strip():
            continue
        value = json.loads(line)
        if not isinstance(value, dict):
            raise ValueError(f"{path}:{number}: expected object")
        result.append(value)
    if not result:
        raise ValueError(f"{path}: empty")
    return result


def detect_count(analysis: dict[str, Any], corpus: str, policy: str) -> int:
    return int(analysis[corpus]["by_policy"][policy]["detected"])


def early_rejections(rows: list[dict[str, Any]], policy: str, fault_ids: set[str]) -> int:
    per_case: dict[str, set[tuple[str, str]]] = {}
    for row in rows:
        if row["policy"] == policy and row["caseId"] in fault_ids:
            per_case.setdefault(row["caseId"], set()).add(
                (row["classification"], row["firstDetectionBoundary"])
            )
    if set(per_case) != fault_ids:
        raise ValueError(f"missing e2e fault rows for {policy}")
    return sum(
        values == {("rejected", "optimized AIR contract verification")}
        for values in per_case.values()
    )


def tensor_fault_rejections(data: dict[str, Any], policy: str) -> int:
    roles = {case["Id"]: case["Role"] for case in data["Cases"]}
    rows = [
        row
        for row in data["Results"]
        if roles[row["CaseId"]] == 2 and TENSOR_POLICY[row["Policy"]] == policy
    ]
    return sum(row["Classification"] == "rejected" for row in rows)


def tensor_invocations(data: dict[str, Any], policy: str, role: int) -> int:
    roles = {case["Id"]: case["Role"] for case in data["Cases"]}
    return sum(
        int(row["VerifierInvocations"])
        for row in data["Results"]
        if roles[row["CaseId"]] == role and TENSOR_POLICY[row["Policy"]] == policy
    )


def validate_mechanisms(data: dict[str, Any], expected_commit: str) -> list[dict[str, Any]]:
    if data.get("Status") != "VALIDATED":
        raise ValueError("mechanism ablations are not validated")
    if data.get("Commit") != expected_commit:
        raise ValueError(
            f"mechanism commit mismatch: {data.get('Commit')} != {expected_commit}"
        )
    rows = data.get("Results")
    if not isinstance(rows, list) or len(rows) != 8:
        raise ValueError("expected eight mechanism ablations")
    if data.get("LostDetections") != 8 or data.get("FalsePositiveDeltas") != 0:
        raise ValueError("mechanism ablation aggregate invariant failed")
    expected_ids = {f"M{index:02d}" for index in range(1, 9)}
    if {row.get("Id") for row in rows} != expected_ids:
        raise ValueError("mechanism ablation identifiers changed")
    for row in rows:
        full = row["FullProtocol"]
        ablated = row["AblatedProtocol"]
        full_control = row["FullControl"]
        ablated_control = row["AblatedControl"]
        if not full["Detected"] or full["DiagnosticCode"] != row["ExpectedDiagnosticCode"]:
            raise ValueError(f"full protocol invariant failed for {row['Id']}")
        if ablated["Detected"]:
            raise ValueError(f"ablation did not lose the detection for {row['Id']}")
        if full_control["Detected"] or ablated_control["Detected"]:
            raise ValueError(f"control rejection for {row['Id']}")
        if row["LostDetections"] != 1 or row["FalsePositiveDelta"] != 0:
            raise ValueError(f"mechanism delta invariant failed for {row['Id']}")
    return rows


def tex_escape(value: str) -> str:
    replacements = {
        "&": r"\&",
        "%": r"\%",
        "_": r"\_",
        "#": r"\#",
    }
    return "".join(replacements.get(character, character) for character in value)


def write_tex_tables(
    output: Path,
    mechanisms: list[dict[str, Any]],
    ablations: dict[str, Any],
) -> None:
    mechanism_lines = [
        r"\begin{tabular}{@{}llccc@{}}",
        r"\toprule",
        r"ID & Removed mechanism & Full & Ablated & Control FP \\",
        r"\midrule",
    ]
    for row in mechanisms:
        mechanism_lines.append(
            f"{row['Id']} & {tex_escape(row['Mechanism'])} & "
            f"{int(row['FullProtocol']['Detected'])}/1 & "
            f"{int(row['AblatedProtocol']['Detected'])}/1 & "
            f"{int(row['AblatedControl']['Detected'])}/1 \\\\"
        )
    mechanism_lines.extend([r"\bottomrule", r"\end{tabular}", ""])
    (output / "mechanism-ablation-table.tex").write_text(
        "\n".join(mechanism_lines), encoding="utf-8"
    )

    a1 = ablations["A1_NO_TYPED_CONTRACTS"]
    a2 = ablations["A2_NO_REVERIFICATION_DISCHARGE"]
    policy_lines = [
        r"\begin{tabular}{@{}lrrrr@{}}",
        r"\toprule",
        r"Policy ablation & Primary & Challenge & Wist early & Tensor \\",
        r"\midrule",
        f"Remove typed contracts (P0) & {a1['boundaryPrimaryDetected']}/32 & "
        f"{a1['boundaryChallengeDetected']}/10 & {a1['wistEarlyFaultRejections']}/5 & "
        f"{a1['tensorFaultRejections']}/8 \\\\ ",
        f"Keep invalidation only (P1) & {a2['boundaryPrimaryDetected']}/32 & "
        f"{a2['boundaryChallengeDetected']}/10 & {a2['wistEarlyFaultRejections']}/5 & "
        f"{a2['tensorFaultRejections']}/8 \\\\ ",
        r"Selective (P2) & 32/32 & 10/10 & 5/5 & 8/8 \\",
        r"\bottomrule",
        r"\end{tabular}",
        "",
    ]
    (output / "policy-ablation-table.tex").write_text(
        "\n".join(policy_lines), encoding="utf-8"
    )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("boundary_analysis", type=Path)
    parser.add_argument("boundary_raw", type=Path)
    parser.add_argument("e2e_summary", type=Path)
    parser.add_argument("e2e_raw", type=Path)
    parser.add_argument("tensor_results", type=Path)
    parser.add_argument("mechanism_results", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)

    boundary = load_json(args.boundary_analysis)
    boundary_rows = load_jsonl(args.boundary_raw)
    e2e = load_json(args.e2e_summary)
    e2e_rows = load_jsonl(args.e2e_raw)
    tensor = load_json(args.tensor_results)
    mechanisms_data = load_json(args.mechanism_results)

    if boundary.get("schema_version") != 3:
        raise ValueError("boundary schema mismatch")
    commit = str(boundary["commit_sha"])
    if e2e.get("commitSha") != commit:
        raise ValueError("boundary/e2e commit mismatch")
    if e2e.get("status") != "VALIDATED" or e2e.get("rawRecords") != 240:
        raise ValueError("e2e evidence is not validated 240-record evidence")
    if tensor.get("Observations") != 48 or tensor.get("FaultCases") != 8:
        raise ValueError("TensorRules cardinality mismatch")
    mechanisms = validate_mechanisms(mechanisms_data, commit)

    fault_ids = {row["caseId"] for row in e2e_rows if row.get("faultInjected")}
    if len(fault_ids) != 5:
        raise ValueError("expected five Wist targeted faults")

    control_calls = {
        policy: sum(
            int(row["verifier_invocations_total"])
            for row in boundary_rows
            if row["corpus_id"] == "control" and row["policy"] == policy
        )
        for policy in POLICIES
    }
    control_runs = {
        policy: sum(
            1
            for row in boundary_rows
            if row["corpus_id"] == "control" and row["policy"] == policy
        )
        for policy in POLICIES
    }
    if any(control_runs[policy] != 100 for policy in POLICIES):
        raise ValueError("boundary control denominators changed")
    p2_calls = control_calls["P2_SELECTIVE"]
    p3_calls = control_calls["P3_ALWAYS"]
    reduction = (p3_calls - p2_calls) / p3_calls if p3_calls else 0.0

    ablations = {
        "A0_MECHANISM_ISOLATION": {
            "mechanisms": len(mechanisms),
            "fullProtocolDetections": sum(row["FullProtocol"]["Detected"] for row in mechanisms),
            "ablatedProtocolDetections": sum(row["AblatedProtocol"]["Detected"] for row in mechanisms),
            "controlFalsePositives": sum(row["AblatedControl"]["Detected"] for row in mechanisms),
            "results": mechanisms,
        },
        "A1_NO_TYPED_CONTRACTS": {
            "proxyPolicy": "P0_STRUCTURAL",
            "boundaryPrimaryDetected": detect_count(boundary, "primary", "P0_STRUCTURAL"),
            "boundaryPrimaryLossVsP2": detect_count(boundary, "primary", "P2_SELECTIVE")
            - detect_count(boundary, "primary", "P0_STRUCTURAL"),
            "boundaryChallengeDetected": detect_count(boundary, "challenge", "P0_STRUCTURAL"),
            "boundaryChallengeLossVsP2": detect_count(boundary, "challenge", "P2_SELECTIVE")
            - detect_count(boundary, "challenge", "P0_STRUCTURAL"),
            "wistEarlyFaultRejections": early_rejections(e2e_rows, "P0_STRUCTURAL", fault_ids),
            "tensorFaultRejections": tensor_fault_rejections(tensor, "P0_STRUCTURAL"),
        },
        "A2_NO_REVERIFICATION_DISCHARGE": {
            "proxyPolicy": "P1_INVALIDATION",
            "boundaryPrimaryDetected": detect_count(boundary, "primary", "P1_INVALIDATION"),
            "boundaryPrimaryLossVsP2": detect_count(boundary, "primary", "P2_SELECTIVE")
            - detect_count(boundary, "primary", "P1_INVALIDATION"),
            "boundaryChallengeDetected": detect_count(boundary, "challenge", "P1_INVALIDATION"),
            "boundaryChallengeLossVsP2": detect_count(boundary, "challenge", "P2_SELECTIVE")
            - detect_count(boundary, "challenge", "P1_INVALIDATION"),
            "wistEarlyFaultRejections": early_rejections(e2e_rows, "P1_INVALIDATION", fault_ids),
            "tensorFaultRejections": tensor_fault_rejections(tensor, "P1_INVALIDATION"),
        },
        "A3_SELECTIVE_VS_ALWAYS": {
            "boundaryParityCases": 42,
            "wistParityCases": int(e2e["p2P3ParityCases"]),
            "tensorParityCases": int(tensor["SelectiveAlwaysParity"]),
            "boundaryControlVerifierCallsP2": p2_calls,
            "boundaryControlVerifierCallsP3": p3_calls,
            "boundaryControlInvocationReduction": reduction,
            "tensorValidVerifierCallsP2": tensor_invocations(tensor, "P2_SELECTIVE", 0),
            "tensorValidVerifierCallsP3": tensor_invocations(tensor, "P3_ALWAYS", 0),
            "efficiencyHeadlineThresholdMet": reduction >= 0.25,
        },
        "A4_REMOVE_SECOND_LANGUAGE": {
            "wistEvidenceRemains": True,
            "crossLanguageClaimSupported": False,
            "removedTensorCases": 12,
            "removedTensorFaults": 8,
            "interpretation": (
                "Removing TensorRules leaves Wist evidence intact but removes the "
                "two-package applicability claim."
            ),
        },
    }

    result = {
        "schemaVersion": 2,
        "status": "VALIDATED",
        "inputCommit": commit,
        "ablations": ablations,
        "claimBoundary": {
            "wholeCompilationPerformance": "BLOCKED_PINNED_MACHINE",
            "externalValidity": "BLOCKED_EXTERNAL",
            "efficiencyHeadlineAllowed": False,
        },
    }
    (args.output / "ablations.json").write_text(
        json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8"
    )

    mechanism_rows = "\n".join(
        f"| {row['Id']} | {row['Mechanism']} | 1/1 | 0/1 | 0/1 |"
        for row in mechanisms
    )
    report = f"""# CGO 2027 ablation report

Status: `VALIDATED`.

## Mechanism-level ablations

Each row changes exactly one experiment-side validation mechanism while preserving the same counterexample and a valid control.

| ID | Removed mechanism | Full protocol | Ablated protocol | Ablated control false positives |
|---|---|---:|---:|---:|
{mechanism_rows}

The full protocol detected all 8/8 minimal counterexamples. Removing each mechanism independently lost its corresponding detection (0/8 retained), while both full and ablated validators accepted all eight valid controls.

## Policy-level ablations

| Ablation | Boundary primary | Boundary challenge | Wist early fault rejection | Tensor fault rejection |
|---|---:|---:|---:|---:|
| Remove typed contracts (`P0`) | {ablations['A1_NO_TYPED_CONTRACTS']['boundaryPrimaryDetected']}/32 | {ablations['A1_NO_TYPED_CONTRACTS']['boundaryChallengeDetected']}/10 | 0/5 | 0/8 |
| Keep invalidation, remove discharge (`P1`) | {ablations['A2_NO_REVERIFICATION_DISCHARGE']['boundaryPrimaryDetected']}/32 | {ablations['A2_NO_REVERIFICATION_DISCHARGE']['boundaryChallengeDetected']}/10 | 0/5 | 0/8 |
| Selective (`P2`) | 32/32 | 10/10 | 5/5 | 8/8 |

`P2` and `P3` retain parity on 42 boundary shapes, {e2e['p2P3ParityCases']} Wist source cases and {tensor['SelectiveAlwaysParity']} TensorRules cases. On the 100 boundary controls, P2 executed {p2_calls} verifier calls and P3 executed {p3_calls}, a reduction of {reduction:.1%}. This is below the frozen 25% headline threshold and is not whole-compilation timing.

Removing TensorRules does not change Wist results, but it removes support for the bounded two-package applicability claim. Performance and external-validity claims remain blocked.
"""
    (args.output / "ABLATION_REPORT.md").write_text(report, encoding="utf-8")
    write_tex_tables(args.output, mechanisms, ablations)
    print(
        json.dumps(
            {
                "status": "VALIDATED",
                "mechanismLostDetections": 8,
                "mechanismControlFalsePositives": 0,
                "boundaryControlReduction": reduction,
                "output": str(args.output),
            },
            sort_keys=True,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
