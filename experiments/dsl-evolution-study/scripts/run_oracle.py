#!/usr/bin/env python3
import argparse, json, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
STUDY = ROOT / "experiments" / "dsl-evolution-study"
PROJECT = STUDY / "DslEvolutionStudy.csproj"
CASES = {c["id"]: c for c in json.loads((STUDY / "spec" / "cases.json").read_text())}

p = argparse.ArgumentParser()
p.add_argument("--treatment", choices=["control", "clone-own", "universal-toolchain"], required=True)
p.add_argument("--output", type=Path)
a = p.parse_args()
cmd = ["dotnet", "run", "--project", str(PROJECT), "-c", "Release", "--no-build", "--", "--treatment", a.treatment]
r = subprocess.run(cmd, cwd=ROOT, text=True, capture_output=True)
if r.returncode:
    sys.stderr.write(r.stderr); raise SystemExit(r.returncode)
rows = [json.loads(line) for line in r.stdout.splitlines() if line.strip().startswith("{")]
failures = []
for row in rows:
    case = CASES[row["Id"]]
    expected_value, expected_error = case.get("expected"), case.get("error")
    if row.get("Value") != expected_value or row.get("Error") != expected_error:
        failures.append({"id": row["Id"], "expected": {"value": expected_value, "error": expected_error}, "actual": row})
result = {"treatment": a.treatment, "status": "PASS" if not failures else "FAIL", "cases": rows, "failures": failures}
if a.output:
    a.output.parent.mkdir(parents=True, exist_ok=True); a.output.write_text(json.dumps(result, indent=2) + "\n")
print(json.dumps(result, indent=2))
raise SystemExit(1 if failures else 0)
