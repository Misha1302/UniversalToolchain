#!/usr/bin/env python3
import argparse
import json
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[3]
study = root / "experiments" / "dsl-evolution-study" / "rq3-control"
cases = json.loads((study / "spec" / "cases.json").read_text())
parser = argparse.ArgumentParser()
parser.add_argument("--expect", choices=["retained", "canonicalized"], required=True)
args = parser.parse_args()

for treatment in ("shared-pipeline", "universal-toolchain"):
    proc = subprocess.run(
        ["dotnet", "run", "--project", str(study / "Rq3Control.csproj"), "-c", "Release", "--no-build", "--", "--treatment", treatment],
        cwd=root,
        check=True,
        text=True,
        capture_output=True,
    )
    rows = [json.loads(line) for line in proc.stdout.splitlines() if line.strip().startswith("{")]
    by_id = {row["Id"]: row for row in rows}
    failures = []
    for case in cases:
        row = by_id.get(case["id"])
        if row is None or row["Value"] != case["expected"]:
            failures.append(f"{case['id']}: value")
            continue
        expected_remaining = 0 if args.expect == "canonicalized" else len(case["features"])
        if row["RemainingOperations"] != expected_remaining:
            failures.append(
                f"{case['id']}: remaining={row['RemainingOperations']} expected={expected_remaining}"
            )
    print(json.dumps({
        "treatment": treatment,
        "expect": args.expect,
        "status": "PASS" if not failures else "FAIL",
        "failures": failures,
    }))
    if failures:
        raise SystemExit(1)
