$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

function Invoke-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

Invoke-Dotnet test UniversalToolchain/Tests/Tests.csproj -c Release --no-build
Invoke-Dotnet test UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj -c Release --no-build

$modulesTests = "UniversalToolchain/UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj"
if (Test-Path $modulesTests) {
    Invoke-Dotnet test $modulesTests -c Release --no-build
}

$output = & dotnet run --project UniversalToolchain/Example/Example.csproj -c Release --no-build
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$outputText = $output -join [Environment]::NewLine

if (-not $outputText.Contains("All results match: True")) {
    Write-Error "Example smoke test output did not contain: All results match: True"
}

if (-not $outputText.Contains("Restricted pricing rejects unsupported statement-style bindings: True")) {
    Write-Error "Example smoke test output did not contain: Restricted pricing rejects unsupported statement-style bindings: True"
}
