#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

ROW_END = r"\\"
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


def load(path: Path) -> dict[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError("ablation input must be an object")
    return value


def mechanism_table(data: dict[str, Any]) -> str:
    rows = data["ablations"]["A0_MECHANISM_ISOLATION"]["results"]
    if {row["Id"] for row in rows} != set(MECHANISM_LABELS):
        raise ValueError("mechanism identifiers changed")
    lines = [
        r"\begin{tabular}{@{}lp{0.43\columnwidth}ccc@{}}",
        r"\toprule",
        f"ID & Removed mechanism & Full & Ablated & Control FP {ROW_END}",
        r"\midrule",
    ]
    for row in sorted(rows, key=lambda item: item["Id"]):
        lines.append(
            f"{row['Id']} & {MECHANISM_LABELS[row['Id']]} & "
            f"{int(row['FullProtocol']['Detected'])}/1 & "
            f"{int(row['AblatedProtocol']['Detected'])}/1 & "
            f"{int(row['AblatedControl']['Detected'])}/1 {ROW_END}"
        )
    lines.extend([r"\bottomrule", r"\end{tabular}", ""])
    return "\n".join(lines)


def policy_table(data: dict[str, Any]) -> str:
    ablations = data["ablations"]
    p0 = ablations["A1_NO_TYPED_CONTRACTS"]
    p1 = ablations["A2_NO_REVERIFICATION_DISCHARGE"]
    p1d = ablations["A2D_DEMAND_ONLY_DISCHARGE"]
    p2 = ablations["A3_SELECTIVE_VS_ALWAYS"]
    lines = [
        r"\begin{tabular}{@{}lrrrrr@{}}",
        r"\toprule",
        f"Variant & Prim. & Chall. & Hist. W & Hist. T & Demand W/T {ROW_END}",
        r"\midrule",
        f"P0 no contracts & {p0['boundaryPrimaryDetected']}/32 & "
        f"{p0['boundaryChallengeDetected']}/10 & "
        f"{p0['wistEarlyFaultRejections']}/5 & {p0['tensorFaultRejections']}/8 & "
        f"0/2, 0/2 {ROW_END}",
        f"P1 no discharge & {p1['boundaryPrimaryDetected']}/32 & "
        f"{p1['boundaryChallengeDetected']}/10 & "
        f"{p1['wistEarlyFaultRejections']}/5 & {p1['tensorFaultRejections']}/8 & "
        f"0/2, 0/2 {ROW_END}",
        f"P1D demand only & {p1d['boundaryPrimaryDetected']}/32 & "
        f"{p1d['boundaryChallengeDetected']}/10 & "
        f"{p1d['wistEarlyFaultRejections']}/5 & {p1d['tensorFaultRejections']}/8 & "
        f"{p1d['wistDemandRejections']}/2, {p1d['tensorDemandRejections']}/2 {ROW_END}",
        f"P2 selective & 32/32 & 10/10 & 5/5 & 8/8 & "
        f"{p2['wistDemandRejectionsP2']}/2, {p2['tensorDemandRejectionsP2']}/2 {ROW_END}",
        r"\bottomrule",
        r"\end{tabular}",
        "",
    ]
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("ablations", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    data = load(args.ablations)
    if data.get("status") != "VALIDATED" or data.get("schemaVersion") != 3:
        raise ValueError("expected validated schema-v3 ablation evidence")
    args.output.mkdir(parents=True, exist_ok=True)
    (args.output / "mechanism-ablation-table.tex").write_text(
        mechanism_table(data), encoding="utf-8"
    )
    (args.output / "policy-ablation-table.tex").write_text(
        policy_table(data), encoding="utf-8"
    )
    print(json.dumps({"status": "VALIDATED", "tables": 2}, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
