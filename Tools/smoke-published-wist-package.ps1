[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateNotNullOrEmpty()]
    [string]$PackageVersion
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$NuGetSource = "https://api.nuget.org/v3/index.json"

$smokeDir = Join-Path ([System.IO.Path]::GetTempPath()) ("wist-published-smoke-" + [Guid]::NewGuid().ToString("N"))

function Invoke-Stage {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Stage,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    try {
        & $Action
        if ($LASTEXITCODE -ne 0) {
            throw "command exited with code $LASTEXITCODE"
        }
    }
    catch {
        throw "Published-package smoke failed at stage '$Stage': $($_.Exception.Message)"
    }
}

try {
    New-Item -ItemType Directory -Path $smokeDir -Force | Out-Null

    $env:NUGET_PACKAGES = Join-Path $smokeDir "packages"
    $env:NUGET_HTTP_CACHE_PATH = Join-Path $smokeDir "http-cache"
    $env:DOTNET_CLI_HOME = Join-Path $smokeDir "dotnet-home"
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
    $env:DOTNET_NOLOGO = "1"
    $env:NuGetAudit = "false"

    $nugetConfig = Join-Path $smokeDir "NuGet.Config"
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="$NuGetSource" protocolVersion="3" />
  </packageSources>
</configuration>
"@ | Set-Content -Path $nugetConfig -Encoding utf8

    Invoke-Stage "project-create" {
        dotnet new console --framework net10.0 --output $smokeDir --force | Out-Null
    }

    $projectPath = Join-Path $smokeDir ((Split-Path $smokeDir -Leaf) + ".csproj")
    Invoke-Stage "package-reference" {
        dotnet add $projectPath package UniversalToolchain.Wist --version $PackageVersion --source $NuGetSource --no-restore | Out-Null
    }

    $programPath = Join-Path $smokeDir "Program.cs"
    @'
using UniversalToolchain.Wist;

using var rules = WistEngine.CreateRestrictedArithmetic();

var validFormula = "usage * 0.7 + reliability * 0.3 - incidents * 15.0";
var validation = rules.Validate(
    validFormula,
    new
    {
        usage = 100.0,
        reliability = 90.0,
        incidents = 1.0
    });
if (!validation.IsValid)
{
    throw new InvalidOperationException(
        "stage=validation: expected the restricted numeric formula to validate successfully.");
}

var rejected = rules.Validate(
    "let total = usage * 0.7\ntotal",
    new { usage = 100.0 });
if (rejected.IsValid)
{
    throw new InvalidOperationException(
        "stage=validation: expected the restricted preset to reject statement-style syntax.");
}
Console.WriteLine("STAGE=validation PASS");

WistProgram<Func<double, double, double, double>> compiled;
try
{
    compiled = rules.Compile<Func<double, double, double, double>>(
        validFormula,
        "usage",
        "reliability",
        "incidents");
}
catch (Exception exception)
{
    throw new InvalidOperationException("stage=compile: public facade compilation failed.", exception);
}
Console.WriteLine("STAGE=compile PASS");

double score;
try
{
    score = compiled.CompiledDelegate(100.0, 90.0, 1.0);
}
catch (Exception exception)
{
    throw new InvalidOperationException("stage=invocation: compiled delegate invocation failed.", exception);
}
Console.WriteLine("STAGE=invocation PASS");
Console.WriteLine($"RESULT={score:R}");
'@ | Set-Content -Path $programPath -Encoding utf8

    Invoke-Stage "restore" {
        dotnet restore $projectPath --configfile $nugetConfig --no-cache --force | Out-Null
    }

    Invoke-Stage "build" {
        dotnet build $projectPath -c Release --no-restore | Out-Null
    }

    $runOutput = $null
    Invoke-Stage "validation/compile/invocation" {
        $script:runOutput = @(dotnet run --project $projectPath -c Release --no-build --no-restore)
    }

    foreach ($marker in @("STAGE=validation PASS", "STAGE=compile PASS", "STAGE=invocation PASS")) {
        if ($runOutput -notcontains $marker) {
            throw "Published-package smoke failed at stage '$($marker.Split('=')[1].Split(' ')[0])': success marker was not emitted."
        }
    }

    $resultLine = $runOutput | Where-Object { $_ -like "RESULT=*" } | Select-Object -Last 1
    if ($null -eq $resultLine) {
        throw "Published-package smoke failed at stage 'expected-output': RESULT marker was not emitted."
    }

    $actual = [double]::Parse(
        $resultLine.Substring("RESULT=".Length),
        [System.Globalization.CultureInfo]::InvariantCulture)
    if ([Math]::Abs($actual - 82.0) -gt 1e-9) {
        throw "Published-package smoke failed at stage 'expected-output': expected 82, got $actual."
    }

    $metadataPath = Join-Path $env:NUGET_PACKAGES "universaltoolchain.wist/$PackageVersion/.nupkg.metadata"
    if (-not (Test-Path -LiteralPath $metadataPath)) {
        throw "Published-package smoke failed at stage 'package-source': NuGet metadata was not found for exact version $PackageVersion."
    }
    $metadata = Get-Content -LiteralPath $metadataPath -Raw
    if (-not $metadata.Contains($NuGetSource, [StringComparison]::Ordinal)) {
        throw "Published-package smoke failed at stage 'package-source': package metadata does not record the public NuGet.org feed."
    }

    Write-Host "Published UniversalToolchain.Wist $PackageVersion smoke passed."
}
finally {
    if (Test-Path -LiteralPath $smokeDir) {
        Remove-Item -LiteralPath $smokeDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
