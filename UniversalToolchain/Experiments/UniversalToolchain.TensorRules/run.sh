#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
project="$root/UniversalToolchain/Experiments/UniversalToolchain.TensorRules/UniversalToolchain.TensorRules.csproj"
output="${1:-$root/artifacts/cgo27-tensorrules}"
rm -rf "$output"
mkdir -p "$output/source-snapshot" "$root/UniversalToolchain/packages"
if grep -Eiq 'Wist|ModuleContracts|BasicCore|IntermediateRepresentation' "$project"; then
  echo "TensorRules project crossed the public SDK boundary" >&2
  exit 1
fi
unset PLATFORM || true
dotnet restore "$project" -p:NuGetAudit=false
dotnet build "$project" -c Release --no-restore -p:WarningsAsErrors=true
dotnet run --project "$project" -c Release --no-build -- "$output"
cp "$project" "$output/source-snapshot/"
cp "$(dirname "$project")/Program.cs" "$output/source-snapshot/"
cp "$(dirname "$project")/README.md" "$output/source-snapshot/"
cp "$(dirname "$project")/run.sh" "$output/source-snapshot/"
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
