#!/usr/bin/env bash
set -Eeuo pipefail

PACKAGE_VERSION="${1:?usage: $0 <published-version>}"
NUGET_SOURCE="${NUGET_SOURCE:-https://api.nuget.org/v3/index.json}"
SMOKE_DIR="$(mktemp -d)"

cleanup() {
    rm -rf "$SMOKE_DIR"
}
trap cleanup EXIT

cat > "$SMOKE_DIR/NuGet.Config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="$NUGET_SOURCE" protocolVersion="3" />
  </packageSources>
</configuration>
EOF

dotnet new console --framework net10.0 --output "$SMOKE_DIR" --force >/dev/null

export NUGET_PACKAGES="$SMOKE_DIR/packages"
export NUGET_HTTP_CACHE_PATH="$SMOKE_DIR/http-cache"
export DOTNET_CLI_HOME="$SMOKE_DIR/dotnet-home"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_NOLOGO=1
export NuGetAudit=false

(
    cd "$SMOKE_DIR"
    dotnet add package UniversalToolchain.Wist \
        --version "$PACKAGE_VERSION" \
        --source "$NUGET_SOURCE"
)

cat > "$SMOKE_DIR/Program.cs" <<'EOF'
using UniversalToolchain.Wist;

using var rules = WistEngine.CreateRestrictedArithmetic();

var pricing = rules.Compile<Func<double, double, double>>(
    "price * 0.9 + fee",
    "price",
    "fee");

double pricingResult = pricing.CompiledDelegate(100.0, 5.0);
AssertClose(95.0, pricingResult, "pricing compiled result");

double evaluatedResult = rules.Evaluate<double>(
    "price * 0.9 + fee",
    new
    {
        price = 100.0,
        fee = 5.0
    });
AssertClose(95.0, evaluatedResult, "pricing evaluated result");

var rollout = rules.Compile<Func<double, double, double, double>>(
    "usage * 0.7 + reliability * 0.3 - incidents * 15.0",
    "usage",
    "reliability",
    "incidents");
AssertClose(82.0, rollout.CompiledDelegate(100.0, 90.0, 1.0), "rollout result");

var lms = rules.Compile<Func<double, double, double, double, double>>(
    "correct * pointsPerTask - penalties * penaltyPoints",
    "correct",
    "pointsPerTask",
    "penalties",
    "penaltyPoints");
AssertClose(84.0, lms.CompiledDelegate(18.0, 5.0, 2.0, 3.0), "LMS result");

var rejected = rules.Validate(
    "let total = price * 0.9\ntotal",
    new
    {
        price = 100.0
    });

if (rejected.IsValid)
{
    throw new InvalidOperationException("Expected the restricted preset to reject statement-style bindings.");
}

static void AssertClose(double expected, double actual, string label)
{
    if (Math.Abs(expected - actual) > 1e-9)
    {
        throw new InvalidOperationException($"Expected {label} {expected}, got {actual}.");
    }
}
EOF

dotnet run --project "$SMOKE_DIR" -c Release --no-restore >/dev/null

metadata_path="$NUGET_PACKAGES/universaltoolchain.wist/$PACKAGE_VERSION/.nupkg.metadata"
test -f "$metadata_path"
grep -Fq "$NUGET_SOURCE" "$metadata_path"

printf 'Published UniversalToolchain.Wist %s smoke passed.\n' "$PACKAGE_VERSION"
