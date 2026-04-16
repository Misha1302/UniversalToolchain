#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

dotnet test UniversalToolchain/Tests/Tests.csproj -c Release --no-build
dotnet test UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj -c Release --no-build

if [ -f UniversalToolchain/UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj ]; then
  dotnet test UniversalToolchain/UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj -c Release --no-build
fi

output="$(dotnet run --project UniversalToolchain/Example/Example.csproj -c Release --no-build)"

grep -F "All results match: True" <<< "$output"
grep -F "Restricted pricing rejects unsupported statement-style bindings: True" <<< "$output"
