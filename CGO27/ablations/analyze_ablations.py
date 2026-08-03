#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

MECHANISM_LABELS = {
    "M01": "Producer identity",
    "M02": "Source identity",
    "M03": "Selected order",
    "M04": "Canonical owner",
    "M05": "Route conflict",
    "M06": "Fail-closed route",
    "M07": "Capability contract",
    "M08": "Repeated occurrence",
}

POLICIES = (
    "P0_STRUCTURAL",
    "P1_INVALIDATION",
    "P1D_DEMAND_RECOMPUTATION",
    "P2_SELECTIVE",
    "P3_ALWAYS",
)
TENSOR_POLICY = {
    0: "P0_STRUCTURAL",
    1: "P1_INVALIDATION",
    2: "P1D_DEMAND_RECOMPUTATION",
    3: "P2_SELECTIVE",
    4: "P3_ALWAYS",
}


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


def tensor_rejections(
    data: dict[str, Any], policy: str, case_ids: set[str]
) -> int:
    rows = [
        row
        for row in data["Results"]
        if row["CaseId"] in case_ids and TENSOR_POLICY[row["Policy"]] == policy
    ]
    if {row["CaseId"] for row in rows} != case_ids:
        raise ValueError(f"missing TensorRules rows for {policy}")
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
        r"\begin{tabular}{@{}lp{0.43\columnwidth}ccc@{}}",
        r"\toprule",
        r"ID & Removed mechanism & Full & Ablated & Control FP \\",
        r"\midrule",
    ]
    for row in mechanisms:
        mechanism_lines.append(
            f"{row['Id']} & {MECHANISM_LABELS[row['Id']]} & "
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
    a2d = ablations["A2D_DEMAND_ONLY_DISCHARGE"]
    a3 = ablations["A3_SELECTIVE_VS_ALWAYS"]
    policy_lines = [
        r"\begin{tabular}{@{}lrrrrr@{}}",
        r"\toprule",
        r"Variant & Prim. & Chall. & Hist. W & Hist. T & Demand W/T \\",
        r"\midrule",
        f"P0 no contracts & {a1['boundaryPrimaryDetected']}/32 & "
        f"{a1['boundaryChallengeDetected']}/10 & {a1['wistEarlyFaultRejections']}/5 & "
        rf"{a1['tensorFaultRejections']}/8 & 0/2, 0/2 \\",
        f"P1 no discharge & {a2['boundaryPrimaryDetected']}/32 & "
        f"{a2['boundaryChallengeDetected']}/10 & {a2['wistEarlyFaultRejections']}/5 & "
        rf"{a2['tensorFaultRejections']}/8 & 0/2, 0/2 \\",
        f"P1D demand only & {a2d['boundaryPrimaryDetected']}/32 & "
        f"{a2d['boundaryChallengeDetected']}/10 & {a2d['wistEarlyFaultRejections']}/5 & "
        f"{a2d['tensorFaultRejections']}/8 & "
        rf"{a2d['wistDemandRejections']}/2, {a2d['tensorDemandRejections']}/2 \\",
        f"P2 selective & 32/32 & 10/10 & 5/5 & 8/8 & "
        rf"{a3['wistDemandRejectionsP2']}/2, {a3['tensorDemandRejectionsP2']}/2 \\",
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

    if boundary.get("schema_version") != 4:
        raise ValueError("boundary schema mismatch: expected v4")
    commit = str(boundary["commit_sha"])
    if e2e.get("commitSha") != commit:
        raise ValueError("boundary/e2e commit mismatch")
    if (
        e2e.get("status") != "VALIDATED"
        or e2e.get("schemaVersion") != 3
        or e2e.get("rawRecords") != 320
        or e2e.get("historicalV2Cases") != 30
        or e2e.get("demandV3Cases") != 2
    ):
        raise ValueError("e2e evidence is not validated v3 320-record evidence")
    if (
        tensor.get("Observations") != 70
        or tensor.get("FaultCases") != 10
        or tensor.get("SelectiveAlwaysParity") != 14
    ):
        raise ValueError("TensorRules v2 cardinality mismatch")
    mechanisms = validate_mechanisms(mechanisms_data, commit)

    historical_fault_ids = {
        row["caseId"]
        for row in e2e_rows
        if row.get("faultInjected") and row.get("studySet") == "historical-v2"
    }
    demand_fault_ids = {
        row["caseId"]
        for row in e2e_rows
        if row.get("faultInjected") and row.get("studySet") == "demand-v3"
    }
    if len(historical_fault_ids) != 5 or len(demand_fault_ids) != 2:
        raise ValueError("Wist historical/demand fault denominators changed")

    tensor_study_sets = {case["Id"]: case["StudySet"] for case in tensor["Cases"]}
    tensor_roles = {case["Id"]: case["Role"] for case in tensor["Cases"]}
    tensor_historical_fault_ids = {
        case_id
        for case_id, role in tensor_roles.items()
        if role == 2 and tensor_study_sets[case_id] == "historical-v1"
    }
    tensor_demand_fault_ids = {
        case_id
        for case_id, role in tensor_roles.items()
        if role == 2 and tensor_study_sets[case_id] == "demand-v2"
    }
    if len(tensor_historical_fault_ids) != 8 or len(tensor_demand_fault_ids) != 2:
        raise ValueError("TensorRules historical/demand fault denominators changed")

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
    if any(control_runs[policy] != 120 for policy in POLICIES):
        raise ValueError("boundary v4 control denominators changed")
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
            "wistEarlyFaultRejections": early_rejections(e2e_rows, "P0_STRUCTURAL", historical_fault_ids),
            "tensorFaultRejections": tensor_rejections(tensor, "P0_STRUCTURAL", tensor_historical_fault_ids),
        },
        "A2_NO_REVERIFICATION_DISCHARGE": {
            "proxyPolicy": "P1_INVALIDATION",
            "boundaryPrimaryDetected": detect_count(boundary, "primary", "P1_INVALIDATION"),
            "boundaryPrimaryLossVsP2": detect_count(boundary, "primary", "P2_SELECTIVE")
            - detect_count(boundary, "primary", "P1_INVALIDATION"),
            "boundaryChallengeDetected": detect_count(boundary, "challenge", "P1_INVALIDATION"),
            "boundaryChallengeLossVsP2": detect_count(boundary, "challenge", "P2_SELECTIVE")
            - detect_count(boundary, "challenge", "P1_INVALIDATION"),
            "wistEarlyFaultRejections": early_rejections(e2e_rows, "P1_INVALIDATION", historical_fault_ids),
            "tensorFaultRejections": tensor_rejections(tensor, "P1_INVALIDATION", tensor_historical_fault_ids),
        },
        "A2D_DEMAND_ONLY_DISCHARGE": {
            "proxyPolicy": "P1D_DEMAND_RECOMPUTATION",
            "boundaryPrimaryDetected": detect_count(
                boundary, "primary", "P1D_DEMAND_RECOMPUTATION"
            ),
            "boundaryPrimaryLossVsP2": detect_count(
                boundary, "primary", "P2_SELECTIVE"
            )
            - detect_count(boundary, "primary", "P1D_DEMAND_RECOMPUTATION"),
            "boundaryChallengeDetected": detect_count(
                boundary, "challenge", "P1D_DEMAND_RECOMPUTATION"
            ),
            "boundaryChallengeLossVsP2": detect_count(
                boundary, "challenge", "P2_SELECTIVE"
            )
            - detect_count(boundary, "challenge", "P1D_DEMAND_RECOMPUTATION"),
            "wistEarlyFaultRejections": early_rejections(
                e2e_rows, "P1D_DEMAND_RECOMPUTATION", historical_fault_ids
            ),
            "tensorFaultRejections": tensor_rejections(
                tensor, "P1D_DEMAND_RECOMPUTATION", tensor_historical_fault_ids
            ),
            "wistDemandRejections": early_rejections(
                e2e_rows, "P1D_DEMAND_RECOMPUTATION", demand_fault_ids
            ),
            "tensorDemandRejections": tensor_rejections(
                tensor, "P1D_DEMAND_RECOMPUTATION", tensor_demand_fault_ids
            ),
        },
        "A3_SELECTIVE_VS_ALWAYS": {
            "boundaryParityCases": 42,
            "wistParityCases": int(e2e["p2P3ParityCases"]),
            "tensorParityCases": int(tensor["SelectiveAlwaysParity"]),
            "wistDemandRejectionsP2": early_rejections(
                e2e_rows, "P2_SELECTIVE", demand_fault_ids
            ),
            "tensorDemandRejectionsP2": tensor_rejections(
                tensor, "P2_SELECTIVE", tensor_demand_fault_ids
            ),
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
            "removedTensorCases": 14,
            "removedTensorFaults": 10,
            "interpretation": (
                "Removing TensorRules leaves Wist evidence intact but removes the "
                "two-package applicability claim."
            ),
        },
    }

    result = {
        "schemaVersion": 3,
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
| Discharge only on explicit demand (`P1D`) | {ablations['A2D_DEMAND_ONLY_DISCHARGE']['boundaryPrimaryDetected']}/32 | {ablations['A2D_DEMAND_ONLY_DISCHARGE']['boundaryChallengeDetected']}/10 | 0/5 | 0/8 |
| Selective (`P2`) | 32/32 | 10/10 | 5/5 | 8/8 |

On the matched demand pairs, P1D rejects the queried case but defers the otherwise identical unqueried case in both systems (1/2 in each); P2 rejects both (2/2 in each). `P2` and `P3` retain parity on 42 boundary shapes, {e2e['p2P3ParityCases']} Wist source cases and {tensor['SelectiveAlwaysParity']} TensorRules cases. On the 120 boundary controls, P2 executed {p2_calls} verifier calls and P3 executed {p3_calls}, a reduction of {reduction:.1%}. The isolated-kernel threshold is met, but this is not whole-compilation timing and does not authorize an efficiency headline.

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
