#!/usr/bin/env bash
set -euo pipefail

configuration="Release"
skip_docs=false
skip_pack=false
serial_build=false
no_build_servers=false
jobs="${WIST_BUILD_JOBS:-}"
jobs_explicit=false
if [[ -n "$jobs" ]]; then
  jobs_explicit=true
fi
baseline_source_archive="${WIST_BASELINE_SOURCE_ARCHIVE:-}"
previous_package_bundle="${WIST_PREVIOUS_PACKAGE_BUNDLE:-}"

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
    --jobs)
      jobs="${2:?missing jobs value}"
      jobs_explicit=true
      shift 2
      ;;
    --serial)
      serial_build=true
      shift
      ;;
    --no-build-servers)
      no_build_servers=true
      shift
      ;;
    --baseline-source-archive)
      baseline_source_archive="${2:?missing baseline source archive path}"
      shift 2
      ;;
    --previous-package-bundle)
      previous_package_bundle="${2:?missing previous package bundle path}"
      shift 2
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

detect_job_count() {
  local detected=""
  if command -v nproc >/dev/null 2>&1; then
    detected="$(nproc)"
  elif command -v getconf >/dev/null 2>&1; then
    detected="$(getconf _NPROCESSORS_ONLN 2>/dev/null || true)"
  elif command -v sysctl >/dev/null 2>&1; then
    detected="$(sysctl -n hw.logicalcpu 2>/dev/null || true)"
  fi
  if [[ ! "$detected" =~ ^[1-9][0-9]*$ ]]; then
    detected=1
  fi
  printf '%s\n' "$detected"
}

if [[ -z "$jobs" ]]; then
  jobs="$(detect_job_count)"
fi
if [[ ! "$jobs" =~ ^[1-9][0-9]*$ ]]; then
  echo "--jobs/WIST_BUILD_JOBS must be a positive integer, got: $jobs" >&2
  exit 2
fi
if [[ "$serial_build" == true && "$jobs_explicit" == true && "$jobs" != "1" ]]; then
  echo "--serial conflicts with an explicit job count of $jobs; remove --jobs/WIST_BUILD_JOBS or set it to 1." >&2
  exit 2
fi

build_in_parallel=true
restore_in_parallel=true
shared_compilation=true
restore_mode_args=()
build_server_args=()

if [[ "$serial_build" == true ]]; then
  jobs=1
  build_in_parallel=false
  restore_in_parallel=false
  restore_mode_args+=(--disable-parallel)
fi

if [[ "$no_build_servers" == true ]]; then
  shared_compilation=false
  build_server_args+=(--disable-build-servers)
  export DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1
  export MSBUILDDISABLENODEREUSE=1
fi

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
    "${restore_mode_args[@]}" \
    "${build_server_args[@]}" \
    "${restore_source_args[@]}" \
    "-p:RestoreBuildInParallel=$restore_in_parallel" \
    "-p:UseSharedCompilation=$shared_compilation" \
    -p:NuGetAudit=false
done

for sample_project in "${markdown_sample_projects[@]}"; do
  "$dotnet_command" restore "$sample_project" \
    "${restore_mode_args[@]}" \
    "${build_server_args[@]}" \
    "${restore_source_args[@]}" \
    "-p:RestoreBuildInParallel=$restore_in_parallel" \
    "-p:UseSharedCompilation=$shared_compilation" \
    -p:NuGetAudit=false
done

for solution in "${solutions[@]}"; do
  "$dotnet_command" build "$solution" \
    -c "$configuration" \
    --no-restore \
    "${build_server_args[@]}" \
    "-m:$jobs" \
    "-p:BuildInParallel=$build_in_parallel" \
    "-p:UseSharedCompilation=$shared_compilation" \
    -p:NuGetAudit=false
done

# Runnable Markdown samples are built by the canonical entrypoint so docs checks
# can execute with --no-build --no-restore.
for sample_project in "${markdown_sample_projects[@]}"; do
  "$dotnet_command" build "$sample_project" \
    -c "$configuration" \
    --no-restore \
    "${build_server_args[@]}" \
    "-m:$jobs" \
    "-p:BuildInParallel=$build_in_parallel" \
    "-p:UseSharedCompilation=$shared_compilation" \
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
python3 Tools/check-retired-surface.py --root "$root"
python3 Tools/test-retired-surface-mutants.py --root "$root"
python3 Tools/check_documentation_status.py
python3 Tools/test-documentation-status-mutants.py --root "$root"
python3 Tools/check-build-topology.py --root "$root"
python3 Tools/test-build-topology-mutants.py --root "$root"

if [[ "$skip_pack" == false ]]; then
  if [[ -z "$baseline_source_archive" || ! -f "$baseline_source_archive" ]]; then
    echo "Packaging requires --baseline-source-archive (or WIST_BASELINE_SOURCE_ARCHIVE) pointing to the reviewed previous source ZIP." >&2
    exit 1
  fi
  if [[ -z "$previous_package_bundle" || ! -f "$previous_package_bundle" ]]; then
    echo "Packaging requires --previous-package-bundle (or WIST_PREVIOUS_PACKAGE_BUNDLE) pointing to the reviewed previous package bundle." >&2
    exit 1
  fi
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
      "${restore_mode_args[@]}" \
      "${build_server_args[@]}" \
      "${restore_source_args[@]}" \
      "-p:RestoreBuildInParallel=$restore_in_parallel" \
      "-p:UseSharedCompilation=$shared_compilation" \
      -p:NuGetAudit=false
  done
  for package_project in "${package_projects[@]}"; do
    "$dotnet_command" pack "$package_project" \
      -c "$configuration" \
      --no-restore \
      "${build_server_args[@]}" \
      "-m:$jobs" \
      "-p:BuildInParallel=$build_in_parallel" \
      -o artifacts/packages \
      /p:WarningsAsErrors=NU5118 \
      "-p:UseSharedCompilation=$shared_compilation" \
      -p:NuGetAudit=false
  done
  mapfile -t packed_archives < <(find artifacts/packages -maxdepth 1 -type f \
    \( -name '*.nupkg' -o -name '*.snupkg' \) -print | sort)
  python3 Tools/repack-nupkg-deterministic.py "${packed_archives[@]}"

  python3 Tools/check-package-version-provenance.py \
    --previous-bundle "$previous_package_bundle" \
    --current-packages artifacts/packages \
    --baseline-contract eng/package-release-baseline.json
  python3 Tools/test-package-version-provenance-mutants.py --root "$root"
  python3 Tools/check-wist-api-compatibility.py --baseline-source-archive "$baseline_source_archive"
  python3 Tools/test-wist-api-compatibility-mutants.py \
    --root "$root" \
    --baseline-source-archive "$baseline_source_archive"

  python3 Tools/check-language-sdk-package-matrix.py \
    --root "$root" \
    --manifest "$package_manifest" \
    --packages artifacts/packages
  python3 Tools/package_metadata.py \
    --root "$root" \
    --manifest "$package_manifest" \
    --packages artifacts/packages \
    --previous-bundle "$previous_package_bundle" \
    --baseline-contract eng/package-release-baseline.json \
    --report artifacts/package-metadata-report.json
  python3 Tools/test-package-metadata-mutants.py --root "$root"

  wist_version="$(sed -nE 's:.*<Version>([^<]+)</Version>.*:\1:p' UniversalToolchain/UniversalToolchain.Wist/UniversalToolchain.Wist.csproj)"
  test -n "$wist_version"
  wist_package="artifacts/packages/UniversalToolchain.Wist.${wist_version}.nupkg"
  wist_reference_assembly="UniversalToolchain/UniversalToolchain.Wist/bin/$configuration/net10.0/UniversalToolchain.Wist.dll"
  test -f "$wist_reference_assembly"
  wist_reference_dir="$(dirname "$wist_reference_assembly")"
  wist_compile_reference="UniversalToolchain/UniversalToolchain.Wist/obj/$configuration/net10.0/ref/UniversalToolchain.Wist.dll"
  test -f "$wist_compile_reference"
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
