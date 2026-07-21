#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import math
import statistics
from pathlib import Path
from typing import Any

MODES = ("B0", "B1", "B2")
REPETITIONS = 3
EXPECTED_PRIMARY_INSTANCES = 40
EXPECTED_PRIMARY_OPERATORS = 32
EXPECTED_PRIMARY_FAMILIES = 5
EXPECTED_CHALLENGE_OPERATORS = 10
EXPECTED_CONTROL_RUNS_PER_MODE = 100
EXPECTED_CONTROL_FAMILIES = 5


def load_jsonl(path: Path) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        if not line.strip():
            continue
        row = json.loads(line)
        required = {
            "Commit", "MutationId", "OperatorId", "StudySet", "Family", "Mode",
            "Repetition", "Detected", "DiagnosticCode", "Boundary", "ElapsedTicks",
        }
        missing = required.difference(row)
        if missing:
            raise ValueError(f"{path}:{number}: missing fields {sorted(missing)}")
        if row["Mode"] not in MODES:
            raise ValueError(f"{path}:{number}: unknown mode {row['Mode']!r}")
        if row["StudySet"] not in {"primary", "challenge", "control"}:
            raise ValueError(f"{path}:{number}: unknown study set {row['StudySet']!r}")
        rows.append(row)
    return rows


def wilson(successes: int, total: int, z: float = 1.959963984540054) -> tuple[float, float]:
    if total == 0:
        return (0.0, 0.0)
    p = successes / total
    denominator = 1.0 + z * z / total
    center = (p + z * z / (2.0 * total)) / denominator
    radius = z * math.sqrt(p * (1.0 - p) / total + z * z / (4.0 * total * total)) / denominator
    return (max(0.0, center - radius), min(1.0, center + radius))


def exact_mcnemar(left: dict[str, bool], right: dict[str, bool]) -> dict[str, Any]:
    ids = sorted(set(left) & set(right))
    left_only = sum(left[i] and not right[i] for i in ids)
    right_only = sum(not left[i] and right[i] for i in ids)
    n = left_only + right_only
    if n == 0:
        p_value = 1.0
    else:
        tail = min(left_only, right_only)
        one_sided = sum(math.comb(n, k) for k in range(tail + 1)) / (2 ** n)
        p_value = min(1.0, 2.0 * one_sided)
    return {
        "left_only": left_only,
        "right_only": right_only,
        "discordant": n,
        "two_sided_exact_p": p_value,
    }


def validate_and_collapse(rows: list[dict[str, Any]]) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    mutation_rows = [r for r in rows if r["StudySet"] in {"primary", "challenge"}]
    control_rows = [r for r in rows if r["StudySet"] == "control"]
    commits = {r["Commit"] for r in rows}
    if len(commits) != 1:
        raise ValueError(f"mixed commits: {sorted(commits)}")

    collapsed_instances: list[dict[str, Any]] = []
    for key in sorted({(r["StudySet"], r["MutationId"], r["Mode"]) for r in mutation_rows}):
        study_set, mutation_id, mode = key
        group = [
            r for r in mutation_rows
            if r["StudySet"] == study_set and r["MutationId"] == mutation_id and r["Mode"] == mode
        ]
        if len(group) != REPETITIONS:
            raise ValueError(f"{study_set}/{mutation_id}/{mode}: expected {REPETITIONS} repetitions, found {len(group)}")
        classifications = {
            (r["Detected"], r["DiagnosticCode"], r["Boundary"], r["OperatorId"], r["Family"])
            for r in group
        }
        if len(classifications) != 1:
            raise ValueError(f"{study_set}/{mutation_id}/{mode}: flaky or inconsistent classification")
        collapsed_instances.append(group[0])

    for key in sorted({(r["StudySet"], r["OperatorId"], r["Mode"]) for r in collapsed_instances}):
        study_set, operator_id, mode = key
        group = [
            r for r in collapsed_instances
            if r["StudySet"] == study_set and r["OperatorId"] == operator_id and r["Mode"] == mode
        ]
        if len({bool(r["Detected"]) for r in group}) != 1:
            raise ValueError(f"{study_set}/{operator_id}/{mode}: instances disagree on detection")

    primary = [r for r in collapsed_instances if r["StudySet"] == "primary"]
    challenge = [r for r in collapsed_instances if r["StudySet"] == "challenge"]
    if len({r["MutationId"] for r in primary}) != EXPECTED_PRIMARY_INSTANCES:
        raise ValueError("unexpected primary instance count")
    if len({r["OperatorId"] for r in primary}) != EXPECTED_PRIMARY_OPERATORS:
        raise ValueError("unexpected primary operator count")
    if len({r["Family"] for r in primary}) != EXPECTED_PRIMARY_FAMILIES:
        raise ValueError("unexpected primary family count")
    if len({r["OperatorId"] for r in challenge}) != EXPECTED_CHALLENGE_OPERATORS:
        raise ValueError("unexpected challenge operator count")

    for mode in MODES:
        controls = [r for r in control_rows if r["Mode"] == mode]
        if len(controls) != EXPECTED_CONTROL_RUNS_PER_MODE:
            raise ValueError(f"{mode}: expected {EXPECTED_CONTROL_RUNS_PER_MODE} controls, found {len(controls)}")
        if len({r["Family"] for r in controls}) != EXPECTED_CONTROL_FAMILIES:
            raise ValueError(f"{mode}: expected {EXPECTED_CONTROL_FAMILIES} control families")
        if any(r["Repetition"] != 1 for r in controls):
            raise ValueError(f"{mode}: controls must have repetition=1")

    return collapsed_instances, control_rows


def collapse_operators(instance_rows: list[dict[str, Any]], study_set: str) -> list[dict[str, Any]]:
    rows = [r for r in instance_rows if r["StudySet"] == study_set]
    collapsed: list[dict[str, Any]] = []
    for key in sorted({(r["OperatorId"], r["Mode"]) for r in rows}):
        operator_id, mode = key
        group = [r for r in rows if r["OperatorId"] == operator_id and r["Mode"] == mode]
        collapsed.append(group[0])
    return collapsed


def summarize_set(operator_rows: list[dict[str, Any]]) -> dict[str, Any]:
    by_mode: dict[str, Any] = {}
    detected_by_mode: dict[str, dict[str, bool]] = {}
    for mode in MODES:
        rows = [r for r in operator_rows if r["Mode"] == mode]
        detected = sum(bool(r["Detected"]) for r in rows)
        low, high = wilson(detected, len(rows))
        by_mode[mode] = {
            "operators": len(rows),
            "detected": detected,
            "detection_rate": detected / len(rows) if rows else 0.0,
            "wilson_95": [low, high],
            "localized": sum(bool(r["Detected"] and r["DiagnosticCode"]) for r in rows),
        }
        detected_by_mode[mode] = {r["OperatorId"]: bool(r["Detected"]) for r in rows}

    by_family: list[dict[str, Any]] = []
    for family in sorted({r["Family"] for r in operator_rows}):
        for mode in MODES:
            group = [r for r in operator_rows if r["Family"] == family and r["Mode"] == mode]
            by_family.append({
                "family": family,
                "mode": mode,
                "operators": len(group),
                "detected": sum(bool(r["Detected"]) for r in group),
            })

    return {
        "operator_count": len({r["OperatorId"] for r in operator_rows}),
        "family_count": len({r["Family"] for r in operator_rows}),
        "by_mode": by_mode,
        "by_family": by_family,
        "detected_by_mode": detected_by_mode,
    }


def analyze(rows: list[dict[str, Any]], replicate_summaries: list[Path]) -> dict[str, Any]:
    collapsed_instances, controls = validate_and_collapse(rows)
    primary = summarize_set(collapse_operators(collapsed_instances, "primary"))
    challenge = summarize_set(collapse_operators(collapsed_instances, "challenge"))

    clean_by_mode = {
        mode: {
            "runs": sum(r["Mode"] == mode for r in controls),
            "false_positives": sum(r["Mode"] == mode and bool(r["Detected"]) for r in controls),
        }
        for mode in MODES
    }
    clean_by_family = []
    for family in sorted({r["Family"] for r in controls}):
        for mode in MODES:
            group = [r for r in controls if r["Family"] == family and r["Mode"] == mode]
            clean_by_family.append({
                "family": family,
                "mode": mode,
                "runs": len(group),
                "false_positives": sum(bool(r["Detected"]) for r in group),
            })

    performance_runs: list[dict[str, Any]] = []
    for path in replicate_summaries:
        payload = json.loads(path.read_text(encoding="utf-8"))
        perf = payload["Performance"]
        performance_runs.append({
            "path": str(path),
            "B1": float(perf["MedianOverheadPercent"]["B1"]),
            "B2": float(perf["MedianOverheadPercent"]["B2"]),
        })
    performance = None
    if performance_runs:
        performance = {
            "process_replicates": len(performance_runs),
            "B1_median_overhead_percent": statistics.median(r["B1"] for r in performance_runs),
            "B1_range_percent": [min(r["B1"] for r in performance_runs), max(r["B1"] for r in performance_runs)],
            "B2_median_overhead_percent": statistics.median(r["B2"] for r in performance_runs),
            "B2_range_percent": [min(r["B2"] for r in performance_runs), max(r["B2"] for r in performance_runs)],
            "runs": performance_runs,
        }

    primary_detected = primary.pop("detected_by_mode")
    challenge.pop("detected_by_mode")
    return {
        "commit": rows[0]["Commit"],
        "repetitions": REPETITIONS,
        "primary": primary,
        "challenge": challenge,
        "clean": clean_by_mode,
        "clean_by_family": clean_by_family,
        "mcnemar_primary_B0_vs_B2": exact_mcnemar(primary_detected["B0"], primary_detected["B2"]),
        "mcnemar_primary_B1_vs_B2": exact_mcnemar(primary_detected["B1"], primary_detected["B2"]),
        "performance": performance,
    }


def write_report(analysis: dict[str, Any], output: Path) -> None:
    primary = analysis["primary"]["by_mode"]
    challenge = analysis["challenge"]["by_mode"]
    lines = [
        "# Contract experiment analysis v2",
        "",
        f"- Commit identity: `{analysis['commit']}`",
        f"- Primary catalog: {analysis['primary']['operator_count']} operator shapes",
        f"- Post-freeze challenge set: {analysis['challenge']['operator_count']} operators",
        f"- Repetitions: {analysis['repetitions']} per mutation instance and mode",
        f"- Primary B0/B1/B2: {primary['B0']['detected']}/{primary['B0']['operators']}, "
        f"{primary['B1']['detected']}/{primary['B1']['operators']}, "
        f"{primary['B2']['detected']}/{primary['B2']['operators']}",
        f"- Primary exact McNemar B0 vs B2: p={analysis['mcnemar_primary_B0_vs_B2']['two_sided_exact_p']:.8g}",
        f"- Primary exact McNemar B1 vs B2: p={analysis['mcnemar_primary_B1_vs_B2']['two_sided_exact_p']:.8g}",
        f"- Challenge B0/B1/B2: {challenge['B0']['detected']}/{challenge['B0']['operators']}, "
        f"{challenge['B1']['detected']}/{challenge['B1']['operators']}, "
        f"{challenge['B2']['detected']}/{challenge['B2']['operators']}",
        f"- Stratified valid controls: B2 false positives {analysis['clean']['B2']['false_positives']}/{analysis['clean']['B2']['runs']}",
    ]
    if analysis["performance"]:
        perf = analysis["performance"]
        lines.extend([
            f"- Synthetic B2 boundary-kernel overhead: {perf['B2_median_overhead_percent']:.1f}% median across {perf['process_replicates']} processes",
            f"- Synthetic B2 range: {perf['B2_range_percent'][0]:.1f}% to {perf['B2_range_percent'][1]:.1f}%",
        ])

    lines.extend([
        "",
        "## Primary family-level detections",
        "",
        "| Family | B0 | B1 | B2 |",
        "|---|---:|---:|---:|",
    ])
    families = sorted({r["family"] for r in analysis["primary"]["by_family"]})
    for family in families:
        values = {r["mode"]: r for r in analysis["primary"]["by_family"] if r["family"] == family}
        lines.append(
            f"| {family} | {values['B0']['detected']}/{values['B0']['operators']} | "
            f"{values['B1']['detected']}/{values['B1']['operators']} | "
            f"{values['B2']['detected']}/{values['B2']['operators']} |"
        )

    lines.extend([
        "",
        "## Interpretation boundary",
        "",
        "The primary catalog and post-freeze challenge set are author-designed and execute production verifier components at compiler boundaries. The challenge set uses diagnostic operators absent from the primary catalog, but it is neither blind nor externally authored. The stratified controls vary five valid boundary families; they are not a population-level false-positive estimate. No result establishes general compiler correctness, end-to-end source-to-execution detection, or external validity across unrelated runtimes.",
    ])
    output.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("results", type=Path)
    parser.add_argument("--replicate-summary", action="append", type=Path, default=[])
    parser.add_argument("--out-dir", type=Path, required=True)
    args = parser.parse_args()
    args.out_dir.mkdir(parents=True, exist_ok=True)
    analysis = analyze(load_jsonl(args.results), args.replicate_summary)
    (args.out_dir / "analysis.json").write_text(json.dumps(analysis, indent=2) + "\n", encoding="utf-8")
    write_report(analysis, args.out_dir / "analysis.md")
    print(json.dumps(analysis, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
