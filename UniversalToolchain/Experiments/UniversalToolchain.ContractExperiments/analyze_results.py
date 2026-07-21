#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import math
import statistics
from collections import defaultdict
from pathlib import Path
from typing import Any

MODES = ("B0", "B1", "B2")
REPETITIONS = 3
EXPECTED_MUTATIONS = 40
EXPECTED_FAMILIES = 5


def load_jsonl(path: Path) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        if not line.strip():
            continue
        row = json.loads(line)
        required = {
            "Commit", "MutationId", "Family", "Mode", "Repetition", "Detected",
            "DiagnosticCode", "Boundary", "ElapsedTicks",
        }
        missing = required.difference(row)
        if missing:
            raise ValueError(f"{path}:{number}: missing fields {sorted(missing)}")
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
    mutations = [r for r in rows if not r["MutationId"].startswith("CLEAN-")]
    clean = [r for r in rows if r["MutationId"].startswith("CLEAN-")]
    mutation_ids = sorted({r["MutationId"] for r in mutations})
    families = sorted({r["Family"] for r in mutations})
    if len(mutation_ids) != EXPECTED_MUTATIONS:
        raise ValueError(f"expected {EXPECTED_MUTATIONS} mutation ids, found {len(mutation_ids)}")
    if len(families) != EXPECTED_FAMILIES:
        raise ValueError(f"expected {EXPECTED_FAMILIES} families, found {len(families)}")
    commits = {r["Commit"] for r in mutations}
    if len(commits) != 1:
        raise ValueError(f"mixed commits: {sorted(commits)}")

    collapsed: list[dict[str, Any]] = []
    for mutation_id in mutation_ids:
        for mode in MODES:
            group = [r for r in mutations if r["MutationId"] == mutation_id and r["Mode"] == mode]
            if len(group) != REPETITIONS:
                raise ValueError(f"{mutation_id}/{mode}: expected {REPETITIONS} repetitions, found {len(group)}")
            classifications = {(r["Detected"], r["DiagnosticCode"], r["Boundary"]) for r in group}
            if len(classifications) != 1:
                raise ValueError(f"{mutation_id}/{mode}: flaky classification {sorted(classifications)}")
            collapsed.append(group[0])
    return collapsed, clean


def analyze(rows: list[dict[str, Any]], replicate_summaries: list[Path]) -> dict[str, Any]:
    collapsed, clean = validate_and_collapse(rows)
    by_mode: dict[str, Any] = {}
    detected_by_mode: dict[str, dict[str, bool]] = {}
    for mode in MODES:
        mode_rows = [r for r in collapsed if r["Mode"] == mode]
        detected = sum(bool(r["Detected"]) for r in mode_rows)
        low, high = wilson(detected, len(mode_rows))
        by_mode[mode] = {
            "mutations": len(mode_rows),
            "detected": detected,
            "detection_rate": detected / len(mode_rows),
            "wilson_95": [low, high],
            "localized": sum(bool(r["Detected"] and r["DiagnosticCode"]) for r in mode_rows),
        }
        detected_by_mode[mode] = {r["MutationId"]: bool(r["Detected"]) for r in mode_rows}

    by_family: list[dict[str, Any]] = []
    for family in sorted({r["Family"] for r in collapsed}):
        for mode in MODES:
            group = [r for r in collapsed if r["Family"] == family and r["Mode"] == mode]
            by_family.append({
                "family": family,
                "mode": mode,
                "mutations": len(group),
                "detected": sum(bool(r["Detected"]) for r in group),
            })

    clean_by_mode = {
        mode: {
            "runs": sum(r["Mode"] == mode for r in clean),
            "false_positives": sum(r["Mode"] == mode and bool(r["Detected"]) for r in clean),
        }
        for mode in MODES
    }

    performance_runs: list[dict[str, Any]] = []
    for path in replicate_summaries:
        payload = json.loads(path.read_text(encoding="utf-8"))
        performance_runs.append({
            "path": str(path),
            "B1": float(payload["Performance"]["MedianOverheadPercent"]["B1"]),
            "B2": float(payload["Performance"]["MedianOverheadPercent"]["B2"]),
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

    return {
        "commit": collapsed[0]["Commit"],
        "mutation_count": len({r["MutationId"] for r in collapsed}),
        "family_count": len({r["Family"] for r in collapsed}),
        "repetitions": REPETITIONS,
        "by_mode": by_mode,
        "by_family": by_family,
        "clean": clean_by_mode,
        "mcnemar_B0_vs_B2": exact_mcnemar(detected_by_mode["B0"], detected_by_mode["B2"]),
        "mcnemar_B1_vs_B2": exact_mcnemar(detected_by_mode["B1"], detected_by_mode["B2"]),
        "performance": performance,
    }


def write_report(analysis: dict[str, Any], output: Path) -> None:
    b0 = analysis["by_mode"]["B0"]
    b1 = analysis["by_mode"]["B1"]
    b2 = analysis["by_mode"]["B2"]
    perf = analysis["performance"]
    lines = [
        "# Contract experiment analysis",
        "",
        f"- Commit identity: `{analysis['commit']}`",
        f"- Mutations: {analysis['mutation_count']} across {analysis['family_count']} families",
        f"- Repetitions: {analysis['repetitions']} per mutation/mode",
        f"- B0 detection: {b0['detected']}/{b0['mutations']} ({b0['detection_rate']:.1%})",
        f"- B1 detection: {b1['detected']}/{b1['mutations']} ({b1['detection_rate']:.1%})",
        f"- B2 detection: {b2['detected']}/{b2['mutations']} ({b2['detection_rate']:.1%})",
        f"- Exact McNemar B0 vs B2: p={analysis['mcnemar_B0_vs_B2']['two_sided_exact_p']:.8g}",
        f"- Exact McNemar B1 vs B2: p={analysis['mcnemar_B1_vs_B2']['two_sided_exact_p']:.8g}",
        f"- Clean B2 false positives: {analysis['clean']['B2']['false_positives']}/{analysis['clean']['B2']['runs']}",
    ]
    if perf:
        lines.extend([
            f"- B2 median overhead across {perf['process_replicates']} process replicates: {perf['B2_median_overhead_percent']:.1f}%",
            f"- B2 replicate range: {perf['B2_range_percent'][0]:.1f}% to {perf['B2_range_percent'][1]:.1f}%",
        ])
    lines.extend([
        "",
        "## Family-level detections",
        "",
        "| Family | B0 | B1 | B2 |",
        "|---|---:|---:|---:|",
    ])
    families = sorted({r["family"] for r in analysis["by_family"]})
    for family in families:
        values = {r["mode"]: r for r in analysis["by_family"] if r["family"] == family}
        lines.append(f"| {family} | {values['B0']['detected']}/{values['B0']['mutations']} | {values['B1']['detected']}/{values['B1']['mutations']} | {values['B2']['detected']}/{values['B2']['mutations']} |")
    lines.extend([
        "",
        "## Interpretation boundary",
        "",
        "This is a frozen, author-designed production-boundary mutation study. It measures the implemented verifiers and effect protocol on the selected mutation catalog; it does not establish general compiler correctness, independent external validity, or effectiveness on arbitrary future defects.",
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
