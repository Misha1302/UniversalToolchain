#!/usr/bin/env bash
set -euo pipefail

configuration="Release"
skip_docs=false
skip_pack=false

while (($#)); do
  case "$1" in
    --configuration)
      configuration="${2:?missing configuration value}"
      shift 2
      ;;
    --skip-docs)
      skip_docs=true
      shift
      ;;
    --skip-pack)
      skip_pack=true
      shift
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 2
      ;;
  esac
done

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$root"

# Environment variables such as Docker's PLATFORM become global MSBuild properties.
unset PLATFORM || true
export DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1
export MSBUILDDISABLENODEREUSE=1

dotnet_command="${DOTNET:-dotnet}"
solutions=(
  "UniversalToolchain/Wist.sln"
  "UniversalToolchain/PlanFuzz.sln"
)
test_manifest="eng/test-projects.txt"
package_manifest="eng/package-projects.txt"
markdown_sample_projects=(
  samples/Acme.PricingLanguage/Acme.PricingLanguage.csproj
  samples/Wist.RolloutScoring/Wist.RolloutScoring.csproj
)
restore_source_args=()
if [[ -n "${NUGET_CONFIG:-}" ]]; then
  restore_source_args+=(--configfile "$NUGET_CONFIG")
fi

read_manifest() {
  local path="$1"
  sed -e 's/[[:space:]]*$//' -e '/^[[:space:]]*#/d' -e '/^[[:space:]]*$/d' "$path"
}

for solution in "${solutions[@]}"; do
  "$dotnet_command" restore "$solution" \
    --disable-parallel \
    --disable-build-servers \
    "${restore_source_args[@]}" \
    -p:RestoreBuildInParallel=false \
    -p:UseSharedCompilation=false \
    -p:NuGetAudit=false
done

for sample_project in "${markdown_sample_projects[@]}"; do
  "$dotnet_command" restore "$sample_project" \
    --disable-parallel \
    --disable-build-servers \
    "${restore_source_args[@]}" \
    -p:RestoreBuildInParallel=false \
    -p:UseSharedCompilation=false \
    -p:NuGetAudit=false
done

for solution in "${solutions[@]}"; do
  "$dotnet_command" build "$solution" \
    -c "$configuration" \
    --no-restore \
    --disable-build-servers \
    -m:1 \
    -p:BuildInParallel=false \
    -p:UseSharedCompilation=false \
    -p:NuGetAudit=false
done

# Runnable Markdown samples are built by the canonical entrypoint so docs checks
# can execute with --no-build --no-restore.
for sample_project in "${markdown_sample_projects[@]}"; do
  "$dotnet_command" build "$sample_project" \
    -c "$configuration" \
    --no-restore \
    --disable-build-servers \
    -m:1 \
    -p:BuildInParallel=false \
    -p:UseSharedCompilation=false \
    -p:NuGetAudit=false
done

mapfile -t test_projects < <(read_manifest "$test_manifest")
if [[ "${#test_projects[@]}" -eq 0 ]]; then
  echo "No test projects declared in $test_manifest" >&2
  exit 1
fi
for test_project in "${test_projects[@]}"; do
  "$dotnet_command" test "$test_project" \
    -c "$configuration" \
    --no-build \
    --no-restore \
    --disable-build-servers \
    -p:UseSharedCompilation=false \
    -p:NuGetAudit=false
done

if [[ "$skip_pack" == false ]]; then
  rm -rf artifacts/packages
  mkdir -p artifacts/packages
  mapfile -t package_projects < <(read_manifest "$package_manifest")
  if [[ "${#package_projects[@]}" -eq 0 ]]; then
    echo "No package projects declared in $package_manifest" >&2
    exit 1
  fi
  for package_project in "${package_projects[@]}"; do
    "$dotnet_command" pack "$package_project" \
      -c "$configuration" \
      --no-restore \
      --disable-build-servers \
      -o artifacts/packages \
      /p:WarningsAsErrors=NU5118 \
      -p:UseSharedCompilation=false \
      -p:NuGetAudit=false
  done

  python3 Tools/check-language-sdk-package-matrix.py \
    --root "$root" \
    --manifest "$package_manifest" \
    --packages artifacts/packages

  wist_version="$(sed -nE 's:.*<Version>([^<]+)</Version>.*:\1:p' UniversalToolchain/UniversalToolchain.Wist/UniversalToolchain.Wist.csproj)"
  test -n "$wist_version"
  python3 Tools/check-wist-package-surface.py \
    "artifacts/packages/UniversalToolchain.Wist.${wist_version}.nupkg"

  python3 Tools/smoke-language-sdk-packages.py \
    --root "$root" \
    --packages artifacts/packages \
    --dotnet "$dotnet_command"
fi

if [[ "$skip_docs" == false ]]; then
  npm ci --no-audit --no-fund
  npm run docs:status
  npm run docs:links
  npm run docs:build
  python3 .github/scripts/run-markdown-bash-blocks.py
fi
