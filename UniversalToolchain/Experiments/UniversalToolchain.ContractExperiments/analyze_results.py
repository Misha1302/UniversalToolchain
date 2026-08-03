#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import math
import statistics
from pathlib import Path
from typing import Any

POLICIES = ("P0_STRUCTURAL", "P1_INVALIDATION", "P1D_DEMAND_RECOMPUTATION", "P2_SELECTIVE", "P3_ALWAYS")
REPETITIONS = 3
EXPECTED_PRIMARY_INSTANCES = 40
EXPECTED_PRIMARY_OPERATORS = 32
EXPECTED_PRIMARY_FAMILIES = 5
EXPECTED_CHALLENGE_OPERATORS = 10
EXPECTED_CONTROL_RUNS_PER_POLICY = 120
EXPECTED_CONTROL_FAMILIES = 6
EXPECTED_DEMAND_OPERATORS = 2
REQUIRED_FIELDS = {
    "schema_version", "run_id", "commit_sha", "policy", "corpus_id", "case_id",
    "case_kind", "language_id", "pipeline_id", "workload_stratum", "expected_outcome",
    "actual_outcome", "expected_diagnostic_family", "actual_diagnostic_family",
    "expected_boundary", "first_detection_boundary", "verifier_invocations_total",
    "verifier_invocations_by_rule", "verification_elapsed_ns", "pipeline_elapsed_ns",
    "whole_compilation_elapsed_ns", "allocated_bytes", "peak_working_set_bytes",
    "obligations_created", "obligations_discharged", "obligations_failed",
    "facts_invalidated", "facts_reverified", "process_exit_code", "repetition", "seed",
    "measurement_scope", "operator_id", "demand_query", "detected",
}


def load_jsonl(path: Path) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        if not line.strip():
            continue
        try:
            row = json.loads(line)
        except json.JSONDecodeError as error:
            raise ValueError(f"{path}:{number}: invalid JSON: {error}") from error
        if not isinstance(row, dict):
            raise ValueError(f"{path}:{number}: record must be a JSON object")
        missing = REQUIRED_FIELDS.difference(row)
        if missing:
            raise ValueError(f"{path}:{number}: missing fields {sorted(missing)}")
        if row["schema_version"] != 4:
            raise ValueError(f"{path}:{number}: unsupported schema_version {row['schema_version']!r}")
        if row["policy"] not in POLICIES:
            raise ValueError(f"{path}:{number}: unknown policy {row['policy']!r}")
        if row["corpus_id"] not in {"primary", "challenge", "demand-v4", "control"}:
            raise ValueError(f"{path}:{number}: unknown corpus {row['corpus_id']!r}")
        if row["case_kind"] not in {"fault", "valid-control"}:
            raise ValueError(f"{path}:{number}: invalid case_kind {row['case_kind']!r}")
        if row["actual_outcome"] not in {"accepted", "rejected", "infrastructure-error"}:
            raise ValueError(f"{path}:{number}: invalid actual_outcome {row['actual_outcome']!r}")
        calls = row["verifier_invocations_by_rule"]
        if not isinstance(calls, dict) or any(not isinstance(value, int) or value < 0 for value in calls.values()):
            raise ValueError(f"{path}:{number}: invalid verifier_invocations_by_rule")
        if row["verifier_invocations_total"] != sum(calls.values()):
            raise ValueError(f"{path}:{number}: verifier invocation accounting mismatch")
        for field in (
            "verification_elapsed_ns", "pipeline_elapsed_ns", "allocated_bytes",
            "peak_working_set_bytes", "obligations_created", "obligations_discharged",
            "obligations_failed", "facts_invalidated", "facts_reverified",
        ):
            if not isinstance(row[field], int) or row[field] < 0:
                raise ValueError(f"{path}:{number}: {field} must be a non-negative integer")
        if row["verification_elapsed_ns"] > row["pipeline_elapsed_ns"]:
            raise ValueError(f"{path}:{number}: verification time exceeds pipeline time")
        if row["whole_compilation_elapsed_ns"] is not None:
            raise ValueError(f"{path}:{number}: boundary study must not claim whole-compilation time")
        rows.append(row)
    if not rows:
        raise ValueError(f"{path}: no records")
    return rows


def exact_mcnemar(left: dict[str, bool], right: dict[str, bool]) -> dict[str, Any]:
    ids = sorted(set(left) & set(right))
    left_only = sum(left[item] and not right[item] for item in ids)
    right_only = sum(not left[item] and right[item] for item in ids)
    discordant = left_only + right_only
    if discordant == 0:
        p_value = 1.0
    else:
        tail = min(left_only, right_only)
        p_value = min(1.0, 2.0 * sum(math.comb(discordant, k) for k in range(tail + 1)) / (2 ** discordant))
    return {
        "left_only": left_only,
        "right_only": right_only,
        "discordant": discordant,
        "two_sided_exact_p": p_value,
    }


def collapse(rows: list[dict[str, Any]]) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    commits = {row["commit_sha"] for row in rows}
    run_ids = {row["run_id"] for row in rows}
    if len(commits) != 1:
        raise ValueError(f"mixed commits: {sorted(commits)}")
    if len(run_ids) != 1:
        raise ValueError(f"mixed run ids: {sorted(run_ids)}")

    fault_rows = [row for row in rows if row["corpus_id"] in {"primary", "challenge", "demand-v4"}]
    control_rows = [row for row in rows if row["corpus_id"] == "control"]
    collapsed: list[dict[str, Any]] = []
    keys = sorted({(row["corpus_id"], row["case_id"], row["policy"]) for row in fault_rows})
    for corpus, case_id, policy in keys:
        group = [
            row for row in fault_rows
            if row["corpus_id"] == corpus and row["case_id"] == case_id and row["policy"] == policy
        ]
        if len(group) != REPETITIONS:
            raise ValueError(f"{corpus}/{case_id}/{policy}: expected {REPETITIONS} repetitions, found {len(group)}")
        classifications = {
            (row["actual_outcome"], row["actual_diagnostic_family"], row["first_detection_boundary"])
            for row in group
        }
        if len(classifications) != 1:
            raise ValueError(f"{corpus}/{case_id}/{policy}: flaky classification")
        collapsed.append(group[0])

    for corpus, operator_id, policy in sorted({
        (row["corpus_id"], row["operator_id"], row["policy"]) for row in collapsed
    }):
        outcomes = {
            row["actual_outcome"] for row in collapsed
            if row["corpus_id"] == corpus and row["operator_id"] == operator_id and row["policy"] == policy
        }
        if len(outcomes) != 1:
            raise ValueError(f"{corpus}/{operator_id}/{policy}: instances disagree")

    primary = [row for row in collapsed if row["corpus_id"] == "primary"]
    challenge = [row for row in collapsed if row["corpus_id"] == "challenge"]
    if len({row["case_id"] for row in primary}) != EXPECTED_PRIMARY_INSTANCES:
        raise ValueError("unexpected primary instance count")
    if len({row["operator_id"] for row in primary}) != EXPECTED_PRIMARY_OPERATORS:
        raise ValueError("unexpected primary operator count")
    if len({row["workload_stratum"] for row in primary}) != EXPECTED_PRIMARY_FAMILIES:
        raise ValueError("unexpected primary family count")
    if len({row["operator_id"] for row in challenge}) != EXPECTED_CHALLENGE_OPERATORS:
        raise ValueError("unexpected challenge operator count")
    demand = [row for row in collapsed if row["corpus_id"] == "demand-v4"]
    if len({row["operator_id"] for row in demand}) != EXPECTED_DEMAND_OPERATORS:
        raise ValueError("unexpected demand-baseline operator count")

    for policy in POLICIES:
        policy_controls = [row for row in control_rows if row["policy"] == policy]
        if len(policy_controls) != EXPECTED_CONTROL_RUNS_PER_POLICY:
            raise ValueError(f"{policy}: expected {EXPECTED_CONTROL_RUNS_PER_POLICY} controls")
        if len({row["workload_stratum"] for row in policy_controls}) != EXPECTED_CONTROL_FAMILIES:
            raise ValueError(f"{policy}: unexpected control family count")
        if any(row["actual_outcome"] != "accepted" for row in policy_controls):
            raise ValueError(f"{policy}: valid control rejected")
    return collapsed, control_rows


def operator_rows(rows: list[dict[str, Any]], corpus: str) -> list[dict[str, Any]]:
    selected = [row for row in rows if row["corpus_id"] == corpus]
    return [
        next(row for row in selected if row["operator_id"] == operator_id and row["policy"] == policy)
        for operator_id, policy in sorted({(row["operator_id"], row["policy"]) for row in selected})
    ]


def summarize_operators(rows: list[dict[str, Any]]) -> tuple[dict[str, Any], dict[str, dict[str, bool]]]:
    by_policy: dict[str, Any] = {}
    detections: dict[str, dict[str, bool]] = {}
    for policy in POLICIES:
        policy_rows = [row for row in rows if row["policy"] == policy]
        rejected = sum(row["actual_outcome"] == "rejected" for row in policy_rows)
        by_policy[policy] = {
            "operators": len(policy_rows),
            "detected": rejected,
            "localized": sum(
                row["actual_outcome"] == "rejected" and row["actual_diagnostic_family"] is not None
                for row in policy_rows
            ),
        }
        detections[policy] = {
            row["operator_id"]: row["actual_outcome"] == "rejected" for row in policy_rows
        }
    return by_policy, detections


def median(values: list[int | float]) -> float:
    return float(statistics.median(values)) if values else 0.0


def analyze(rows: list[dict[str, Any]], replicate_summaries: list[Path]) -> dict[str, Any]:
    collapsed, controls = collapse(rows)
    primary_by_policy, primary_detections = summarize_operators(operator_rows(collapsed, "primary"))
    challenge_by_policy, _ = summarize_operators(operator_rows(collapsed, "challenge"))
    demand_by_policy, demand_detections = summarize_operators(operator_rows(collapsed, "demand-v4"))

    telemetry: dict[str, Any] = {}
    for policy in POLICIES:
        policy_rows = [row for row in rows if row["policy"] == policy]
        telemetry[policy] = {
            "records": len(policy_rows),
            "median_verifier_invocations": median([row["verifier_invocations_total"] for row in policy_rows]),
            "median_verification_elapsed_ns": median([row["verification_elapsed_ns"] for row in policy_rows]),
            "median_pipeline_elapsed_ns": median([row["pipeline_elapsed_ns"] for row in policy_rows]),
            "median_allocated_bytes": median([row["allocated_bytes"] for row in policy_rows]),
            "obligations_created": sum(row["obligations_created"] for row in policy_rows),
            "obligations_discharged": sum(row["obligations_discharged"] for row in policy_rows),
            "obligations_failed": sum(row["obligations_failed"] for row in policy_rows),
            "facts_invalidated": sum(row["facts_invalidated"] for row in policy_rows),
            "facts_reverified": sum(row["facts_reverified"] for row in policy_rows),
        }

    clean = {
        policy: {
            "runs": sum(row["policy"] == policy for row in controls),
            "false_positives": sum(
                row["policy"] == policy and row["actual_outcome"] == "rejected" for row in controls
            ),
        }
        for policy in POLICIES
    }

    performance_runs: list[dict[str, Any]] = []
    for path in replicate_summaries:
        payload = json.loads(path.read_text(encoding="utf-8"))
        overhead = payload["Performance"]["MedianOverheadPercent"]
        performance_runs.append({"path": str(path), **{policy: float(overhead[policy]) for policy in POLICIES}})
    performance = None
    if performance_runs:
        performance = {
            "scope": "isolated boundary-kernel; not whole compilation",
            "process_replicates": len(performance_runs),
            "by_policy": {
                policy: {
                    "median_overhead_percent": median([run[policy] for run in performance_runs]),
                    "range_percent": [min(run[policy] for run in performance_runs), max(run[policy] for run in performance_runs)],
                }
                for policy in POLICIES
            },
            "runs": performance_runs,
        }

    return {
        "schema_version": 4,
        "run_id": rows[0]["run_id"],
        "commit_sha": rows[0]["commit_sha"],
        "policies": list(POLICIES),
        "primary": {"operators": EXPECTED_PRIMARY_OPERATORS, "by_policy": primary_by_policy},
        "challenge": {"operators": EXPECTED_CHALLENGE_OPERATORS, "by_policy": challenge_by_policy},
        "demand_baseline": {"operators": EXPECTED_DEMAND_OPERATORS, "by_policy": demand_by_policy},
        "clean": clean,
        "telemetry": telemetry,
        "paired_tests": {
            "P0_vs_P2": exact_mcnemar(primary_detections["P0_STRUCTURAL"], primary_detections["P2_SELECTIVE"]),
            "P1_vs_P2": exact_mcnemar(primary_detections["P1_INVALIDATION"], primary_detections["P2_SELECTIVE"]),
            "P1D_vs_P2": exact_mcnemar(primary_detections["P1D_DEMAND_RECOMPUTATION"], primary_detections["P2_SELECTIVE"]),
            "P2_vs_P3": exact_mcnemar(primary_detections["P2_SELECTIVE"], primary_detections["P3_ALWAYS"]),
            "demand_P1D_vs_P2": exact_mcnemar(demand_detections["P1D_DEMAND_RECOMPUTATION"], demand_detections["P2_SELECTIVE"]),
        },
        "performance": performance,
    }


def write_report(analysis: dict[str, Any], output: Path) -> None:
    primary = analysis["primary"]["by_policy"]
    challenge = analysis["challenge"]["by_policy"]
    demand = analysis["demand_baseline"]["by_policy"]
    clean = analysis["clean"]
    telemetry = analysis["telemetry"]
    lines = [
        "# CGO 2027 five-policy contract experiment",
        "",
        f"- Commit: `{analysis['commit_sha']}`",
        f"- Run: `{analysis['run_id']}`",
        f"- Primary operators: {analysis['primary']['operators']}",
        f"- Challenge operators: {analysis['challenge']['operators']}",
        f"- Matched demand-baseline operators: {analysis['demand_baseline']['operators']}",
        "- Whole-compilation time: not measured by this boundary experiment",
        "",
        "## Detection and controls",
        "",
        "| Policy | Primary detected | Challenge detected | Demand detected | Control false positives |",
        "|---|---:|---:|---:|---:|",
    ]
    for policy in POLICIES:
        lines.append(
            f"| {policy} | {primary[policy]['detected']}/{primary[policy]['operators']} | "
            f"{challenge[policy]['detected']}/{challenge[policy]['operators']} | "
            f"{demand[policy]['detected']}/{demand[policy]['operators']} | "
            f"{clean[policy]['false_positives']}/{clean[policy]['runs']} |"
        )
    lines.extend([
        "",
        "## Telemetry",
        "",
        "| Policy | Median verifier calls | Obligations created | Discharged | Failed | Facts reverified |",
        "|---|---:|---:|---:|---:|---:|",
    ])
    for policy in POLICIES:
        item = telemetry[policy]
        lines.append(
            f"| {policy} | {item['median_verifier_invocations']:.1f} | {item['obligations_created']} | "
            f"{item['obligations_discharged']} | {item['obligations_failed']} | {item['facts_reverified']} |"
        )
    paired = analysis["paired_tests"]
    lines.extend([
        "",
        "## Paired exact tests on frozen primary operators",
        "",
        f"- P0 vs P2: p={paired['P0_vs_P2']['two_sided_exact_p']:.8g}",
        f"- P1 vs P2: p={paired['P1_vs_P2']['two_sided_exact_p']:.8g}",
        f"- P1D vs P2: p={paired['P1D_vs_P2']['two_sided_exact_p']:.8g}",
        f"- P2 vs P3: p={paired['P2_vs_P3']['two_sided_exact_p']:.8g}",
        f"- Demand P1D vs P2: p={paired['demand_P1D_vs_P2']['two_sided_exact_p']:.8g}",
    ])
    if analysis["performance"]:
        lines.extend(["", "## Boundary-kernel timing", ""])
        for policy in POLICIES:
            item = analysis["performance"]["by_policy"][policy]
            lines.append(
                f"- {policy}: {item['median_overhead_percent']:.1f}% median overhead; "
                f"range {item['range_percent'][0]:.1f}% to {item['range_percent'][1]:.1f}%"
            )
    lines.extend([
        "",
        "These timing values are environment-sensitive verifier-kernel measurements. They are not whole-compilation or application overhead.",
        "",
    ])
    output.write_text("\n".join(lines), encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("results", type=Path)
    parser.add_argument("--replicate-summary", action="append", default=[], type=Path)
    parser.add_argument("--out-dir", required=True, type=Path)
    args = parser.parse_args()
    rows = load_jsonl(args.results)
    analysis = analyze(rows, args.replicate_summary)
    args.out_dir.mkdir(parents=True, exist_ok=True)
    (args.out_dir / "analysis.json").write_text(json.dumps(analysis, indent=2) + "\n", encoding="utf-8")
    write_report(analysis, args.out_dir / "REPORT.md")
    print(json.dumps(analysis, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
