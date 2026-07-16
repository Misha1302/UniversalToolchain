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
# They must not redirect outputs or invalidate solution platform mappings.
unset PLATFORM || true
export DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1

dotnet_command="${DOTNET:-dotnet}"
solution="UniversalToolchain/Wist.sln"
markdown_sample_projects=(
  samples/Wist.RolloutScoring/Wist.RolloutScoring.csproj
)
restore_source_args=()
if [[ -n "${NUGET_CONFIG:-}" ]]; then
  restore_source_args+=(--configfile "$NUGET_CONFIG")
fi

"$dotnet_command" restore "$solution" \
  --disable-parallel \
  "${restore_source_args[@]}" \
  -p:RestoreBuildInParallel=false \
  -p:UseSharedCompilation=false \
  -p:NuGetAudit=false

for sample_project in "${markdown_sample_projects[@]}"; do
  "$dotnet_command" restore "$sample_project" \
    --disable-parallel \
    "${restore_source_args[@]}" \
    -p:RestoreBuildInParallel=false \
    -p:UseSharedCompilation=false \
    -p:NuGetAudit=false
done

"$dotnet_command" build "$solution" \
  -c "$configuration" \
  --no-restore \
  -m:1 \
  -p:BuildInParallel=false \
  -p:UseSharedCompilation=false \
  -p:NuGetAudit=false

# The Markdown checker rewrites runnable `dotnet run --project` fences to
# `--no-build --no-restore`. Keep their Release outputs owned by the canonical
# build entrypoint instead of allowing documentation validation to perform an
# implicit restore or build.
for sample_project in "${markdown_sample_projects[@]}"; do
  "$dotnet_command" build "$sample_project" \
    -c "$configuration" \
    --no-restore \
    -m:1 \
    -p:BuildInParallel=false \
    -p:UseSharedCompilation=false \
    -p:NuGetAudit=false
done

for test_project in \
  UniversalToolchain/Tests/Tests.csproj \
  UniversalToolchain/UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj \
  UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj; do
  "$dotnet_command" test "$test_project" \
    -c "$configuration" \
    --no-build \
    --no-restore \
    -p:UseSharedCompilation=false \
    -p:NuGetAudit=false
 done

if [[ "$skip_pack" == false ]]; then
  mkdir -p artifacts/packages
  "$dotnet_command" pack \
    UniversalToolchain/UniversalToolchain.Wist/UniversalToolchain.Wist.csproj \
    -c "$configuration" \
    --no-restore \
    -o artifacts/packages \
    /p:WarningsAsErrors=NU5118 \
    -p:UseSharedCompilation=false \
    -p:NuGetAudit=false
  python3 Tools/check-wist-package-surface.py artifacts/packages/*.nupkg
fi

if [[ "$skip_docs" == false ]]; then
  npm ci --no-audit --no-fund
  npm run docs:build
  python3 Tools/check_documentation_status.py
  python3 .github/scripts/run-markdown-bash-blocks.py
fi
