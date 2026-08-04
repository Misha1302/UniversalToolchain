#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project_root="$root/UniversalToolchain/Experiments/UniversalToolchain.EndToEndExperiments"
project="$project_root/UniversalToolchain.EndToEndExperiments.csproj"
output="${1:-$root/artifacts/cgo27-end-to-end}"
commit="$(git -C "$root" rev-parse HEAD 2>/dev/null || true)"
if [[ -z "$commit" ]]; then
  commit="${CGO27_SOURCE_SHA:-${CGO27_EXPERIMENT_COMMIT:-}}"
fi
if [[ ! "$commit" =~ ^[0-9a-f]{40}$ ]]; then
  echo "exact 40-hex source commit could not be resolved" >&2
  exit 2
fi
declared_commit="${CGO27_SOURCE_SHA:-${CGO27_EXPERIMENT_COMMIT:-}}"
if [[ -n "$declared_commit" && "$declared_commit" != "$commit" ]]; then
  echo "declared source commit does not match the checked-out revision" >&2
  exit 2
fi
export CGO27_EXPERIMENT_COMMIT="$commit"
rm -rf "$output"
mkdir -p "$output/source-snapshot" "$root/UniversalToolchain/packages"

unset PLATFORM || true
dotnet restore "$project" -p:NuGetAudit=false
dotnet build "$project" -c Release --no-restore -p:WarningsAsErrors=true

dll="$(find "$project_root/bin" -type f -name 'UniversalToolchain.EndToEndExperiments.dll' -print | sort | tail -n 1)"
if [[ -z "$dll" || ! -f "$dll" ]]; then
  echo "end-to-end experiment assembly was not found" >&2
  exit 1
fi

: > "$output/probe-results.jsonl"
for case_id in C01 C02 P01 P02 B01 P07 D01 D02; do
  for policy in P0_STRUCTURAL P1_INVALIDATION P1D_DEMAND_RECOMPUTATION P2_SELECTIVE P3_ALWAYS; do
    dotnet "$dll" --child "$case_id" "$policy" 1 1 cgo27-e2e-probe \
      | tee -a "$output/probe-results.jsonl"
  done
done

python3 "$project_root/run_matrix_v3.py" "$dll" "$output"

python3 - "$output/probe-results.jsonl" "$output/raw-results.jsonl" "$commit" <<'PY'
import json
import sys
from pathlib import Path

for path_text in sys.argv[1:3]:
    path = Path(path_text)
    rows = [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]
    if not rows:
        raise SystemExit(f"no records in {path}")
    mismatches = sorted({row.get("commitSha") for row in rows if row.get("commitSha") != sys.argv[3]})
    if mismatches:
        raise SystemExit(f"commit mismatch in {path}: {mismatches}")
PY

cp "$project" "$output/source-snapshot/"
cp "$project_root/Program.cs" "$output/source-snapshot/"
cp "$project_root/Cgo27FaultOptimizer.cs" "$output/source-snapshot/"
cp "$project_root/run_matrix.py" "$output/source-snapshot/"
cp "$project_root/run_matrix_v2.py" "$output/source-snapshot/"
cp "$project_root/run_matrix_v3.py" "$output/source-snapshot/"
cp "$project_root/README.md" "$output/source-snapshot/"
cp "$root/Tools/run-cgo27-end-to-end.sh" "$output/source-snapshot/"
if git -C "$root" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  git -C "$root" status --porcelain=v1 > "$output/git-status.txt"
else
  : > "$output/git-status.txt"
fi
printf '%s\n' "$commit" > "$output/COMMIT"
(
  cd "$output"
  find . -type f ! -name MANIFEST.sha256 -print0 \
    | sort -z \
    | xargs -0 sha256sum \
    > MANIFEST.sha256
  sha256sum -c MANIFEST.sha256
)
