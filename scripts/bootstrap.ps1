$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet not found"
}

Write-Host "Using SDK policy from UniversalToolchain/global.json"
Write-Host "Restoring UniversalToolchain/Wist.sln"
dotnet restore UniversalToolchain/Wist.sln

Write-Host "Building UniversalToolchain/Wist.sln in Release configuration"
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore

Write-Host "Repository bootstrap completed successfully."
