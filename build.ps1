param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipDocs,
    [switch]$SkipPack
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root
$env:PLATFORM = $null
$env:DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER = "1"
$dotnet = if ($env:DOTNET) { $env:DOTNET } else { "dotnet" }
$solution = "UniversalToolchain/Wist.sln"

function Invoke-CheckedNative {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $Command $($Arguments -join ' ')"
    }
}

$restoreArguments = @(
    "restore", $solution,
    "--disable-parallel",
    "-p:RestoreBuildInParallel=false",
    "-p:UseSharedCompilation=false",
    "-p:NuGetAudit=false"
)
if ($env:NUGET_CONFIG) {
    $restoreArguments += @("--configfile", $env:NUGET_CONFIG)
}

Invoke-CheckedNative $dotnet $restoreArguments
Invoke-CheckedNative $dotnet @(
    "build", $solution,
    "-c", $Configuration,
    "--no-restore",
    "-m:1",
    "-p:BuildInParallel=false",
    "-p:UseSharedCompilation=false",
    "-p:NuGetAudit=false"
)

$testProjects = @(
    "UniversalToolchain/Tests/Tests.csproj",
    "UniversalToolchain/UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj",
    "UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj"
)

foreach ($project in $testProjects) {
    Invoke-CheckedNative $dotnet @(
        "test", $project,
        "-c", $Configuration,
        "--no-build",
        "--no-restore",
        "-p:UseSharedCompilation=false",
        "-p:NuGetAudit=false"
    )
}

if (-not $SkipPack) {
    New-Item -ItemType Directory -Force -Path "artifacts/packages" | Out-Null
    Invoke-CheckedNative $dotnet @(
        "pack", "UniversalToolchain/UniversalToolchain.Wist/UniversalToolchain.Wist.csproj",
        "-c", $Configuration,
        "--no-restore",
        "-o", "artifacts/packages",
        "/p:WarningsAsErrors=NU5118",
        "-p:UseSharedCompilation=false",
        "-p:NuGetAudit=false"
    )

    $packages = Get-ChildItem "artifacts/packages" -Filter "*.nupkg" |
        Where-Object { -not $_.Name.EndsWith(".symbols.nupkg") -and -not $_.Name.EndsWith(".snupkg") } |
        ForEach-Object { $_.FullName }
    if ($packages.Count -eq 0) {
        throw "No .nupkg files were produced in artifacts/packages."
    }
    $packageCheckArguments = @("Tools/check-wist-package-surface.py") + @($packages)
    Invoke-CheckedNative "python" $packageCheckArguments
}

if (-not $SkipDocs) {
    Invoke-CheckedNative "npm" @("ci", "--no-audit", "--no-fund")
    Invoke-CheckedNative "npm" @("run", "docs:build")
    Invoke-CheckedNative "python" @("Tools/check_documentation_status.py")
    Invoke-CheckedNative "python" @(".github/scripts/run-markdown-bash-blocks.py")
}
