#!/usr/bin/env python3
import json
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[3]
change = "5bca2e701a8e213416f0bf1965a5b53b63e9c953"
before = "214204c5668007353033e6c00f2fecca73e39923"
files = {
    "shared-pipeline": "experiments/dsl-evolution-study/rq3-control/SharedPipelineTreatment.cs",
    "universal-toolchain": "experiments/dsl-evolution-study/rq3-control/UtDownstreamTreatment.cs",
}
feature_files = {
    "experiments/dsl-evolution-study/rq3-control/SharedFeatureSemantics.cs",
    "experiments/dsl-evolution-study/rq3-control/UtFeatureSemantics.cs",
}

def git(*args):
    return subprocess.check_output(["git", *args], cwd=root, text=True)

changed = [line for line in git("diff", "--name-only", f"{before}..{change}").splitlines() if line]
expected = sorted(files.values())
if sorted(changed) != expected:
    raise SystemExit(f"unexpected downstream mutation scope: {changed}")

metrics = {}
for treatment, path in files.items():
    numstat = git("diff", "--numstat", f"{before}..{change}", "--", path).strip().split("\t")
    added, deleted = int(numstat[0]), int(numstat[1])
    patch = git("diff", "--unified=0", f"{before}..{change}", "--", path)
    site_token = "program = Canonicalize" if treatment == "shared-pipeline" else 'AddPass("rq3.downstream.canonicalize"'
    propagation_sites = sum(1 for line in patch.splitlines() if line.startswith("+") and site_token in line)
    metrics[treatment] = {
        "changed_existing_loc": added + deleted,
        "added": added,
        "deleted": deleted,
        "touched_existing_files": 1,
        "propagation_sites": propagation_sites,
        "new_downstream_loc": added,
        "variant_semantic_files_touched": len(set(changed) & feature_files),
    }

summary = {
    "schema_version": 1,
    "before_commit": before,
    "downstream_change_commit": change,
    "metrics": metrics,
    "interpretation": "Shared downstream reuse is available to both the ordinary shared-pipeline baseline and UT; this control does not establish a source-language extensibility advantage.",
}
out = root / "experiments" / "dsl-evolution-study" / "rq3-control" / "results"
out.mkdir(exist_ok=True)
(out / "summary.json").write_text(json.dumps(summary, indent=2) + "\n")
(out / "raw.json").write_text(json.dumps({"changed_files": changed, "feature_files": sorted(feature_files), "metrics": metrics}, indent=2) + "\n")
print(json.dumps(summary, indent=2))
