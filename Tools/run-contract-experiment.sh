#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

out_dir="${1:-artifacts/contract-experiment}"
replicates="${ICSE_EXPERIMENT_REPLICATES:-5}"
project="UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/UniversalToolchain.ContractExperiments.csproj"
experiment_dir="UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments"
analyzer="$experiment_dir/analyze_results.py"

rm -rf "$out_dir"
mkdir -p "$out_dir/replicates" "$out_dir/analysis" "$out_dir/source" "$out_dir/environment"

commit="${GITHUB_SHA:-${ICSE_EXPERIMENT_COMMIT:-unknown}}"
export ICSE_EXPERIMENT_COMMIT="$commit"
printf '%s\n' "$commit" > "$out_dir/environment/commit.txt"
git status --porcelain=v1 > "$out_dir/environment/git-status.txt" 2>/dev/null || true
dotnet --info > "$out_dir/environment/dotnet-info.txt"
uname -a > "$out_dir/environment/uname.txt"

cp "$experiment_dir/Program.cs" "$out_dir/source/Program.cs"
cp "$experiment_dir/analyze_results.py" "$out_dir/source/analyze_results.py"
cp "$experiment_dir/UniversalToolchain.ContractExperiments.csproj" "$out_dir/source/UniversalToolchain.ContractExperiments.csproj"
cp "$experiment_dir/README.md" "$out_dir/source/README.md"
cp "$experiment_dir/STUDY_PROTOCOL_V2.md" "$out_dir/source/STUDY_PROTOCOL_V2.md"
cp "Tools/run-contract-experiment.sh" "$out_dir/source/run-contract-experiment.sh"

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

if [[ -n "${ICSE_CI_EVIDENCE_DIR:-}" && -d "${ICSE_CI_EVIDENCE_DIR}" ]]; then
  cp -a "${ICSE_CI_EVIDENCE_DIR}" "$out_dir/environment/ci-evidence"
fi

(
  cd "$out_dir"
  find . -type f ! -name SHA256SUMS.txt -print0 \
    | sort -z \
    | xargs -0 sha256sum \
    > SHA256SUMS.txt
  sha256sum -c SHA256SUMS.txt
)
