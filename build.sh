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

# NuGet.config declares this repository-local feed. It is optional in clean
# checkouts, but NuGet requires every configured local source path to exist.
mkdir -p UniversalToolchain/packages

# Environment variables such as Docker's PLATFORM become global MSBuild properties.
unset PLATFORM || true
export DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1
export MSBUILDDISABLENODEREUSE=1

dotnet_command="${DOTNET:-dotnet}"
solutions=(
  "UniversalToolchain/Wist.sln"
  "UniversalToolchain/PlanFuzz.sln"
)
test_contract="eng/test-counts.json"
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

python3 Tools/run-test-contract.py \
  --root "$root" \
  --manifest "$test_contract" \
  --dotnet "$dotnet_command" \
  --configuration "$configuration"
python3 Tools/test-test-contract-mutants.py \
  --root "$root" \
  --manifest "$test_contract" \
  --results-directory artifacts/test-contract

if [[ "$skip_pack" == false ]]; then
  rm -rf artifacts/packages
  mkdir -p artifacts/packages
  mapfile -t package_projects < <(read_manifest "$package_manifest")
  if [[ "${#package_projects[@]}" -eq 0 ]]; then
    echo "No package projects declared in $package_manifest" >&2
    exit 1
  fi
  # The package manifest may include projects that are not part of either
  # solution (for example, the dotnet template package). Restore every
  # declared package project explicitly before using --no-restore for pack.
  for package_project in "${package_projects[@]}"; do
    "$dotnet_command" restore "$package_project" \
      --disable-parallel \
      --disable-build-servers \
      "${restore_source_args[@]}" \
      -p:RestoreBuildInParallel=false \
      -p:UseSharedCompilation=false \
      -p:NuGetAudit=false
  done
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

  python3 Tools/check-wist-api-compatibility.py
  python3 Tools/test-wist-api-compatibility-mutants.py --root "$root"

  python3 Tools/check-language-sdk-package-matrix.py \
    --root "$root" \
    --manifest "$package_manifest" \
    --packages artifacts/packages

  wist_version="$(sed -nE 's:.*<Version>([^<]+)</Version>.*:\1:p' UniversalToolchain/UniversalToolchain.Wist/UniversalToolchain.Wist.csproj)"
  test -n "$wist_version"
  wist_package="artifacts/packages/UniversalToolchain.Wist.${wist_version}.nupkg"
  wist_reference_assembly="$(find UniversalToolchain/UniversalToolchain.Wist/bin -type f -path "*/$configuration/net10.0/UniversalToolchain.Wist.dll" -print -quit)"
  test -n "$wist_reference_assembly"
  wist_reference_dir="$(dirname "$wist_reference_assembly")"
  wist_compile_reference="$(find UniversalToolchain/UniversalToolchain.Wist/obj -type f -path "*/$configuration/net10.0/ref/UniversalToolchain.Wist.dll" -print -quit)"
  test -n "$wist_compile_reference"
  python3 Tools/check-wist-package-surface.py \
    --reference-dir "$wist_reference_dir" \
    --compile-reference "$wist_compile_reference" \
    "$wist_package"
  python3 Tools/test-wist-package-surface-mutants.py \
    --root "$root" \
    --reference-dir "$wist_reference_dir" \
    --compile-reference "$wist_compile_reference" \
    "$wist_package"
  python3 Tools/smoke-wist-package.py \
    --package-dir artifacts/packages \
    --version "$wist_version" \
    --dotnet "$dotnet_command"

  python3 Tools/smoke-language-sdk-packages.py \
    --root "$root" \
    --packages artifacts/packages \
    --dotnet "$dotnet_command"

  mapfile -t release_artifacts < <(find artifacts/packages -maxdepth 1 -type f \
    \( -name '*.nupkg' -o -name '*.snupkg' \) -printf '%P\n' | sort)
  if [[ "${#release_artifacts[@]}" -eq 0 ]]; then
    echo "No release package artifacts found" >&2
    exit 1
  fi
  release_artifact_paths=()
  for artifact in "${release_artifacts[@]}"; do
    release_artifact_paths+=("packages/$artifact")
  done
  python3 Tools/release-integrity.py write \
    --base artifacts \
    --manifest artifacts/RELEASE-INTEGRITY.json \
    --root-output artifacts/RELEASE-INTEGRITY.root.sha256 \
    "${release_artifact_paths[@]}"
  python3 Tools/release-integrity.py verify \
    --base artifacts \
    --manifest artifacts/RELEASE-INTEGRITY.json \
    --expected-root-file artifacts/RELEASE-INTEGRITY.root.sha256
  python3 Tools/test-release-integrity-mutants.py --root "$root" "$wist_package"
fi

if [[ "$skip_docs" == false ]]; then
  npm ci --no-audit --no-fund
  npm run docs:status
  npm run docs:links
  npm run docs:build
  python3 .github/scripts/run-markdown-bash-blocks.py
fi
