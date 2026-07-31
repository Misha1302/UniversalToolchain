#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$root/UniversalToolchain/Experiments/UniversalToolchain.EndToEndExperiments/UniversalToolchain.EndToEndExperiments.csproj"
output="${1:-$root/artifacts/cgo27-end-to-end}"
rm -rf "$output"
mkdir -p "$output/source-snapshot" "$root/UniversalToolchain/packages"

unset PLATFORM || true
dotnet restore "$project" -p:NuGetAudit=false
dotnet build "$project" -c Release --no-restore -p:WarningsAsErrors=true

dll="$(find "$(dirname "$project")/bin" -type f -name 'UniversalToolchain.EndToEndExperiments.dll' -print | sort | tail -n 1)"
if [[ -z "$dll" || ! -f "$dll" ]]; then
  echo "end-to-end experiment assembly was not found" >&2
  exit 1
fi

dotnet "$dll" "$output"

cp "$project" "$output/source-snapshot/"
cp "$(dirname "$project")/Program.cs" "$output/source-snapshot/"
cp "$(dirname "$project")/Cgo27FaultOptimizer.cs" "$output/source-snapshot/"
cp "$(dirname "$project")/README.md" "$output/source-snapshot/"
cp "$root/Tools/run-cgo27-end-to-end.sh" "$output/source-snapshot/"
git -C "$root" status --porcelain=v1 > "$output/git-status.txt"
printf '%s\n' "${GITHUB_SHA:-${CGO27_EXPERIMENT_COMMIT:-local-uncommitted}}" > "$output/COMMIT"
find "$output" -type f ! -name MANIFEST.sha256 -print0 \
  | sort -z \
  | xargs -0 sha256sum \
  > "$output/MANIFEST.sha256"
(cd "$output" && sha256sum -c MANIFEST.sha256)
