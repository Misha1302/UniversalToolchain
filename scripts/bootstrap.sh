#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

global_json="UniversalToolchain/global.json"

read_json_value() {
  local key="$1"
  sed -n "s/.*\"$key\"[[:space:]]*:[[:space:]]*\"\\([^\"]*\\)\".*/\\1/p" "$global_json" | head -n 1
}

sdk_version="$(read_json_value version)"
roll_forward="$(read_json_value rollForward)"
allow_prerelease="$(sed -n 's/.*"allowPrerelease"[[:space:]]*:[[:space:]]*\(true\|false\).*/\1/p' "$global_json" | head -n 1)"

echo "Repository SDK policy: version ${sdk_version:-unknown}, rollForward ${roll_forward:-unknown}, allowPrerelease ${allow_prerelease:-unknown}."
echo "Policy source: $global_json"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet was not found on PATH." >&2
  echo "Install a .NET SDK compatible with the repository policy in $global_json." >&2
  echo "The policy may allow roll-forward according to its rollForward setting; it is not an exact-only SDK requirement." >&2
  exit 127
fi

current_sdk="$(dotnet --version)"
echo "Current dotnet SDK: $current_sdk"

dotnet restore UniversalToolchain/Wist.sln
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore
