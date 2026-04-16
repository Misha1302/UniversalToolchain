$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$globalJson = Join-Path $repoRoot "UniversalToolchain/global.json"
$sdkPolicy = Get-Content $globalJson -Raw | ConvertFrom-Json
$sdk = $sdkPolicy.sdk

function Invoke-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

Write-Host "Repository SDK policy: version $($sdk.version), rollForward $($sdk.rollForward), allowPrerelease $($sdk.allowPrerelease)."
Write-Host "Policy source: UniversalToolchain/global.json"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    [Console]::Error.WriteLine("dotnet was not found on PATH. Install a .NET SDK compatible with the repository policy in UniversalToolchain/global.json. The policy may allow roll-forward according to its rollForward setting; it is not an exact-only SDK requirement.")
    exit 127
}

Write-Host "Current dotnet SDK: $(& dotnet --version)"
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Invoke-Dotnet restore UniversalToolchain/Wist.sln
Invoke-Dotnet build UniversalToolchain/Wist.sln -c Release --no-restore
