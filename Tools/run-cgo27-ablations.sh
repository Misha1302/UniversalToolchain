#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
output="${1:-$root/artifacts/cgo27-ablations}"
inputs="$output/inputs"
rm -rf "$output"
mkdir -p "$inputs" "$output/source-snapshot" "$root/UniversalToolchain/packages"
CONTRACT_EXPERIMENT_REPLICATES=1 bash "$root/Tools/run-contract-experiment.sh" "$inputs/boundary"
bash "$root/Tools/run-cgo27-end-to-end.sh" "$inputs/wist-end-to-end"
bash "$root/UniversalToolchain/Experiments/UniversalToolchain.TensorRules/run.sh" "$inputs/tensorrules"
python3 "$root/CGO27/ablations/analyze_ablations.py" \
  "$inputs/boundary/analysis/analysis.json" \
  "$inputs/boundary/main/results.jsonl" \
  "$inputs/wist-end-to-end/summary.json" \
  "$inputs/wist-end-to-end/raw-results.jsonl" \
  "$inputs/tensorrules/results.json" \
  "$output/analysis"
cp "$root/CGO27/ablations/analyze_ablations.py" "$output/source-snapshot/"
cp "$root/Tools/run-cgo27-ablations.sh" "$output/source-snapshot/"
printf '%s\n' "${GITHUB_SHA:-local-uncommitted}" > "$output/COMMIT"
if git -C "$root" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  git -C "$root" status --porcelain=v1 > "$output/git-status.txt"
else
  : > "$output/git-status.txt"
fi
(
  cd "$output"
  find . -type f ! -name MANIFEST.sha256 -print0 | sort -z | xargs -0 sha256sum > MANIFEST.sha256
  sha256sum -c MANIFEST.sha256
)
