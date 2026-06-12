#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
EXPECTED_OUTPUT="$SCRIPT_DIR/expected-output.txt"
DIALECT_FILE="UniversalToolchain/Dialects/examples/wist/pricing-restricted/dialect.wistdialect"

cd "$REPOSITORY_ROOT"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: .NET SDK 10.x is required." >&2
    exit 1
fi

# Some container environments expose PLATFORM=linux/amd64. MSBuild treats
# PLATFORM as a solution platform and rejects it because Wist.sln uses Any CPU.
unset PLATFORM || true

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

TEMPORARY_DIRECTORY="$(mktemp -d)"
trap 'rm -rf "$TEMPORARY_DIRECTORY"' EXIT
DEMO_OUTPUT="$TEMPORARY_DIRECTORY/pricing-demo.txt"

echo "== Environment =="
dotnet --version
if command -v git >/dev/null 2>&1 && git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    echo "Commit: $(git rev-parse HEAD)"
else
    echo "Commit: unavailable (source archive without .git metadata)"
fi

echo
echo "== Restore =="
dotnet restore UniversalToolchain/Wist.sln

echo
echo "== Release build =="
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore

echo
echo "== Deterministic pricing-dialect runtime plan =="
dotnet run \
    --project UniversalToolchain/Wistc/Wistc.csproj \
    -c Release \
    --no-build \
    -- \
    dialect-inspect \
    --file "$DIALECT_FILE"

echo
echo "== Pricing demonstration =="
dotnet run \
    --project UniversalToolchain/Example/Example.csproj \
    -c Release \
    --no-build | tee "$DEMO_OUTPUT"

while IFS= read -r expectedLine; do
    if [[ -z "$expectedLine" ]]; then
        continue
    fi

    if ! grep -Fqx "$expectedLine" "$DEMO_OUTPUT"; then
        echo "ERROR: expected demo line was not found: $expectedLine" >&2
        exit 1
    fi
done < "$EXPECTED_OUTPUT"

echo
echo "== Binding and compiled-artifact parity tests =="
dotnet test \
    UniversalToolchain/Tests/Tests.csproj \
    -c Release \
    --no-build \
    --filter "FullyQualifiedName~InterpreterBindingsParityTests|FullyQualifiedName~RuntimeCompiledArtifactParityTests|FullyQualifiedName~DslPricingCalculatorParityTests"

echo
echo "== Dialect parity and restriction tests =="
dotnet test \
    UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj \
    -c Release \
    --no-build \
    --filter "FullyQualifiedName~PricingRestrictedDialectExecutionTests|FullyQualifiedName~WistDialectExecutionParityTests"

echo
echo "LangDev 2026 demonstration verification completed successfully."
