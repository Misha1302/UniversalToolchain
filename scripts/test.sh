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

dotnet test Tests/Tests.csproj -c Release --no-build -m:1
dotnet test UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj -c Release --no-build -m:1

if [ -f UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj ]; then
    dotnet test UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj -c Release --no-build -m:1
fi

output="$(dotnet run --project Example/Example.csproj -c Release --no-build)"
grep -F "All results match: True" <<< "$output"
grep -F "Restricted pricing rejects unsupported statement-style bindings: True" <<< "$output"
