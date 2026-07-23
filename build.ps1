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
$env:MSBUILDDISABLENODEREUSE = "1"
$dotnet = if ($env:DOTNET) { $env:DOTNET } else { "dotnet" }
$solution = "UniversalToolchain/Wist.sln"
$testManifest = "eng/test-projects.txt"
$packageManifest = "eng/package-projects.txt"
$markdownSampleProjects = @(
    "samples/Acme.PricingLanguage/Acme.PricingLanguage.csproj",
    "samples/Wist.RolloutScoring/Wist.RolloutScoring.csproj"
)

function Invoke-CheckedNative {
    param(
        [Parameter(Mandatory = $true)][string]$Command,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $Command $($Arguments -join ' ')"
    }
}

function Read-ValidationManifest {
    param([Parameter(Mandatory = $true)][string]$Path)

    return @(Get-Content $Path |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ -and -not $_.StartsWith("#") })
}

function New-RestoreArguments {
    param([Parameter(Mandatory = $true)][string]$Project)

    $arguments = @(
        "restore", $Project,
        "--disable-parallel",
        "--disable-build-servers",
        "-p:RestoreBuildInParallel=false",
        "-p:UseSharedCompilation=false",
        "-p:NuGetAudit=false"
    )
    if ($env:NUGET_CONFIG) {
        $arguments += @("--configfile", $env:NUGET_CONFIG)
    }
    return $arguments
}

Invoke-CheckedNative $dotnet (New-RestoreArguments $solution)
foreach ($project in $markdownSampleProjects) {
    Invoke-CheckedNative $dotnet (New-RestoreArguments $project)
}

Invoke-CheckedNative $dotnet @(
    "build", $solution,
    "-c", $Configuration,
    "--no-restore",
    "--disable-build-servers",
    "-m:1",
    "-p:BuildInParallel=false",
    "-p:UseSharedCompilation=false",
    "-p:NuGetAudit=false"
)

foreach ($project in $markdownSampleProjects) {
    Invoke-CheckedNative $dotnet @(
        "build", $project,
        "-c", $Configuration,
        "--no-restore",
        "--disable-build-servers",
        "-m:1",
        "-p:BuildInParallel=false",
        "-p:UseSharedCompilation=false",
        "-p:NuGetAudit=false"
    )
}

$testProjects = Read-ValidationManifest $testManifest
if ($testProjects.Count -eq 0) {
    throw "No test projects declared in $testManifest"
}
foreach ($project in $testProjects) {
    Invoke-CheckedNative $dotnet @(
        "test", $project,
        "-c", $Configuration,
        "--no-build",
        "--no-restore",
        "--disable-build-servers",
        "-p:UseSharedCompilation=false",
        "-p:NuGetAudit=false"
    )
}

if (-not $SkipPack) {
    Remove-Item "artifacts/packages" -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path "artifacts/packages" | Out-Null
    $packageProjects = Read-ValidationManifest $packageManifest
    if ($packageProjects.Count -eq 0) {
        throw "No package projects declared in $packageManifest"
    }
    foreach ($project in $packageProjects) {
        Invoke-CheckedNative $dotnet @(
            "pack", $project,
            "-c", $Configuration,
            "--no-restore",
            "--disable-build-servers",
            "-o", "artifacts/packages",
            "/p:WarningsAsErrors=NU5118",
            "-p:UseSharedCompilation=false",
            "-p:NuGetAudit=false"
        )
    }

    Invoke-CheckedNative "python" @(
        "Tools/check-language-sdk-package-matrix.py",
        "--root", $root,
        "--manifest", $packageManifest,
        "--packages", "artifacts/packages"
    )

    $wistVersionNode = Select-Xml `
        -Path "UniversalToolchain/UniversalToolchain.Wist/UniversalToolchain.Wist.csproj" `
        -XPath "//*[local-name()='Version']" |
        Select-Object -First 1
    $wistVersion = if ($wistVersionNode) { [string]$wistVersionNode.Node.InnerText } else { $null }
    if (-not $wistVersion) {
        throw "UniversalToolchain.Wist package version was not found."
    }
    Invoke-CheckedNative "python" @(
        "Tools/check-wist-package-surface.py",
        "artifacts/packages/UniversalToolchain.Wist.$wistVersion.nupkg"
    )

    Invoke-CheckedNative "python" @(
        "Tools/smoke-language-sdk-packages.py",
        "--root", $root,
        "--packages", "artifacts/packages",
        "--dotnet", $dotnet
    )
}

if (-not $SkipDocs) {
    Invoke-CheckedNative "npm" @("ci", "--no-audit", "--no-fund")
    Invoke-CheckedNative "npm" @("run", "docs:status")
    Invoke-CheckedNative "npm" @("run", "docs:links")
    Invoke-CheckedNative "npm" @("run", "docs:build")
    Invoke-CheckedNative "python" @(".github/scripts/run-markdown-bash-blocks.py")
}
