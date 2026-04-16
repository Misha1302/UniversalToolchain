#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

command -v dotnet >/dev/null 2>&1 || { echo "dotnet not found"; exit 1; }

echo "Using SDK policy from UniversalToolchain/global.json"
echo "Restoring UniversalToolchain/Wist.sln"
dotnet restore UniversalToolchain/Wist.sln

echo "Building UniversalToolchain/Wist.sln in Release configuration"
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore

echo "Repository bootstrap completed successfully."
