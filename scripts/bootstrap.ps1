$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location (Join-Path $RepoRoot "UniversalToolchain")

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet was not found. Install the .NET SDK selected by UniversalToolchain/global.json."
}

$SdkVersion = dotnet --version
Write-Host "Using dotnet SDK: $SdkVersion"
Write-Host "SDK policy is defined by UniversalToolchain/global.json."

dotnet restore Wist.sln -m:1
dotnet build Wist.sln -c Release --no-restore -m:1
