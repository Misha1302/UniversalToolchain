#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

out_dir="${1:-artifacts/contract-experiment}"
replicates="${CONTRACT_EXPERIMENT_REPLICATES:-5}"
project="UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/UniversalToolchain.ContractExperiments.csproj"
experiment_dir="UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments"
analyzer="$experiment_dir/analyze_results.py"

if [[ ! "$replicates" =~ ^[1-9][0-9]*$ ]]; then
  echo "CONTRACT_EXPERIMENT_REPLICATES must be a positive integer, got: $replicates" >&2
  exit 2
fi

rm -rf "$out_dir"
mkdir -p "$out_dir/replicates" "$out_dir/analysis" "$out_dir/source" "$out_dir/environment"
mkdir -p UniversalToolchain/packages
unset PLATFORM || true

commit="${GITHUB_SHA:-${CGO27_EXPERIMENT_COMMIT:-${CONTRACT_EXPERIMENT_COMMIT:-}}}"
if [[ -z "$commit" ]]; then
  commit="$(git rev-parse HEAD 2>/dev/null || printf 'unknown')"
fi
export CGO27_EXPERIMENT_COMMIT="$commit"
printf '%s\n' "$commit" > "$out_dir/environment/commit.txt"
git status --porcelain=v1 > "$out_dir/environment/git-status.txt" 2>/dev/null || true
dotnet --info > "$out_dir/environment/dotnet-info.txt"
uname -a > "$out_dir/environment/uname.txt"

for file in \
  Program.cs \
  Cgo27Program.Core.cs \
  Cgo27Program.Corpus.cs \
  Cgo27Program.VerificationPipeline.cs \
  Cgo27Program.AirChallenge.cs \
  Cgo27Program.Validation.cs \
  Cgo27Program.Controls.cs \
  Cgo27Program.Performance.cs \
  analyze_results.py \
  UniversalToolchain.ContractExperiments.csproj \
  README.md \
  STUDY_PROTOCOL_V2.md \
  STUDY_PROTOCOL_V3.md \
  raw-result-schema-v3.json; do
  cp "$experiment_dir/$file" "$out_dir/source/$file"
done
cp "Tools/run-contract-experiment.sh" "$out_dir/source/run-contract-experiment.sh"

dotnet restore "$project" -p:NuGetAudit=false
dotnet build "$project" -c Release --no-restore -p:NuGetAudit=false

CGO27_RUN_ID="${commit}-main" \
  dotnet run -c Release --no-build --no-restore --project "$project" -- "$out_dir/main"

replicate_args=()
for ((index=1; index<=replicates; index++)); do
  replicate_dir="$out_dir/replicates/run-$index"
  CGO27_RUN_ID="${commit}-replicate-$index" \
    dotnet run -c Release --no-build --no-restore --project "$project" -- "$replicate_dir"
  replicate_args+=(--replicate-summary "$replicate_dir/summary.json")
done

python3 "$analyzer" \
  "$out_dir/main/results.jsonl" \
  "${replicate_args[@]}" \
  --out-dir "$out_dir/analysis"

if [[ -n "${CONTRACT_EXPERIMENT_BASELINE_MUTATIONS:-}" ]]; then
  cmp --silent "$CONTRACT_EXPERIMENT_BASELINE_MUTATIONS" "$out_dir/main/mutations.csv" || {
    echo "Frozen mutation/operator catalog differs from baseline: $CONTRACT_EXPERIMENT_BASELINE_MUTATIONS" >&2
    exit 3
  }
fi

if [[ -n "${CONTRACT_EXPERIMENT_CI_EVIDENCE_DIR:-}" && -d "${CONTRACT_EXPERIMENT_CI_EVIDENCE_DIR}" ]]; then
  cp -a "${CONTRACT_EXPERIMENT_CI_EVIDENCE_DIR}" "$out_dir/environment/ci-evidence"
fi

(
  cd "$out_dir"
  find . -type f ! -name MANIFEST.sha256 -print0 \
    | sort -z \
    | xargs -0 sha256sum \
    > MANIFEST.sha256
  sha256sum -c MANIFEST.sha256
)
