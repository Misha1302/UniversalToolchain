$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet not found"
}

Write-Host "Running core tests"
dotnet test UniversalToolchain/Tests/Tests.csproj -c Release

Write-Host "Running module tests"
dotnet test UniversalToolchain/UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj -c Release

Write-Host "Running dialect tests"
dotnet test UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj -c Release

Write-Host "Running example smoke test"
dotnet run --project UniversalToolchain/Example/Example.csproj -c Release

Write-Host "Repository tests completed successfully."
