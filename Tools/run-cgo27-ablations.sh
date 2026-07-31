#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
output="${1:-$root/artifacts/cgo27-ablations}"
inputs="$output/inputs"
rm -rf "$output"
mkdir -p "$inputs" "$output/source-snapshot" "$root/UniversalToolchain/packages"
CONTRACT_EXPERIMENT_REPLICATES=1 bash "$root/Tools/run-contract-experiment.sh" "$inputs/boundary"
mechanism_project="$root/UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/UniversalToolchain.MechanismAblations.csproj"
commit="$(git -C "$root" rev-parse HEAD)"
dotnet restore "$mechanism_project" -p:NuGetAudit=false
dotnet build "$mechanism_project" -c Release --no-restore -p:WarningsAsErrors=true
CGO27_EXPERIMENT_COMMIT="$commit" dotnet run -c Release --no-build --no-restore --project "$mechanism_project" -- \
  "$inputs/mechanisms"
bash "$root/Tools/run-cgo27-end-to-end.sh" "$inputs/wist-end-to-end"
bash "$root/UniversalToolchain/Experiments/UniversalToolchain.TensorRules/run.sh" "$inputs/tensorrules"
python3 "$root/CGO27/ablations/analyze_ablations.py" \
  "$inputs/boundary/analysis/analysis.json" \
  "$inputs/boundary/main/results.jsonl" \
  "$inputs/wist-end-to-end/summary.json" \
  "$inputs/wist-end-to-end/raw-results.jsonl" \
  "$inputs/tensorrules/results.json" \
  "$inputs/mechanisms/mechanism-ablations.json" \
  "$output/analysis"
python3 "$root/CGO27/ablations/render_paper_tables.py" \
  "$output/analysis/ablations.json" \
  "$output/paper-tables"
cmp "$output/paper-tables/mechanism-ablation-table.tex" \
  "$root/CGO27/paper/generated/mechanism-ablation-table.tex"
cmp "$output/paper-tables/policy-ablation-table.tex" \
  "$root/CGO27/paper/generated/policy-ablation-table.tex"
cp "$root/CGO27/ablations/analyze_ablations.py" "$output/source-snapshot/"
cp "$root/CGO27/ablations/render_paper_tables.py" "$output/source-snapshot/"
cp "$root/Tools/run-cgo27-ablations.sh" "$output/source-snapshot/"
cp "$root/UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/Cgo27Program.MechanismAblations.cs" "$output/source-snapshot/"
cp "$root/UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/UniversalToolchain.MechanismAblations.csproj" "$output/source-snapshot/"
printf '%s\n' "$commit" > "$output/COMMIT"
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
