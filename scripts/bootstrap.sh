#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root/UniversalToolchain"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "dotnet was not found. Install the .NET SDK selected by UniversalToolchain/global.json." >&2
    exit 1
fi

echo "Using dotnet SDK: $(dotnet --version)"
echo "SDK policy is defined by UniversalToolchain/global.json."

dotnet restore Wist.sln -m:1
dotnet build Wist.sln -c Release --no-restore -m:1
