$ErrorActionPreference = "Stop"

function Assert-OutputContains {
    param(
        [string] $Output,
        [string] $Expected
    )

    if (-not $Output.Contains($Expected)) {
        throw "Expected example output to contain: $Expected"
    }
}

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

dotnet test Tests/Tests.csproj -c Release --no-build -m:1
dotnet test UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj -c Release --no-build -m:1

if (Test-Path "UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj") {
    dotnet test UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj -c Release --no-build -m:1
}

$Output = dotnet run --project Example/Example.csproj -c Release --no-build | Out-String
Assert-OutputContains -Output $Output -Expected "All results match: True"
Assert-OutputContains -Output $Output -Expected "Restricted pricing rejects unsupported statement-style bindings: True"
