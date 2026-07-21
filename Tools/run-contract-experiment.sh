#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

out_dir="${1:-artifacts/contract-experiment}"
replicates="${ICSE_EXPERIMENT_REPLICATES:-5}"
project="UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/UniversalToolchain.ContractExperiments.csproj"
analyzer="UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/analyze_results.py"

rm -rf "$out_dir"
mkdir -p "$out_dir/replicates" "$out_dir/analysis"

commit="${GITHUB_SHA:-${ICSE_EXPERIMENT_COMMIT:-unknown}}"
export ICSE_EXPERIMENT_COMMIT="$commit"

dotnet run -c Release --no-restore --project "$project" -- "$out_dir/main"

replicate_args=()
for ((index=1; index<=replicates; index++)); do
  replicate_dir="$out_dir/replicates/run-$index"
  dotnet run -c Release --no-build --no-restore --project "$project" -- "$replicate_dir"
  replicate_args+=(--replicate-summary "$replicate_dir/summary.json")
done

python3 "$analyzer" \
  "$out_dir/main/results.jsonl" \
  "${replicate_args[@]}" \
  --out-dir "$out_dir/analysis"

sha256sum \
  "$out_dir/main/results.jsonl" \
  "$out_dir/main/mutations.csv" \
  "$out_dir/main/environment.json" \
  "$out_dir/analysis/analysis.json" \
  "$out_dir/analysis/analysis.md" \
  > "$out_dir/SHA256SUMS.txt"

sha256sum -c "$out_dir/SHA256SUMS.txt"
