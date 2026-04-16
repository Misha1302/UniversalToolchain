#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

command -v dotnet >/dev/null 2>&1 || { echo "dotnet not found"; exit 1; }

echo "Running core tests"
dotnet test UniversalToolchain/Tests/Tests.csproj -c Release

echo "Running module tests"
dotnet test UniversalToolchain/UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj -c Release

echo "Running dialect tests"
dotnet test UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj -c Release

echo "Running example smoke test"
dotnet run --project UniversalToolchain/Example/Example.csproj -c Release

echo "Repository tests completed successfully."
