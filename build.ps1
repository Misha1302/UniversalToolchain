param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipDocs,
    [switch]$SkipPack
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

# NuGet.config declares this repository-local feed. It is optional in clean
# checkouts, but NuGet requires every configured local source path to exist.
New-Item -ItemType Directory -Force -Path "UniversalToolchain/packages" | Out-Null

$env:PLATFORM = $null
$env:DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER = "1"
$env:MSBUILDDISABLENODEREUSE = "1"
$dotnet = if ($env:DOTNET) { $env:DOTNET } else { "dotnet" }
$solutions = @(
    "UniversalToolchain/Wist.sln",
    "UniversalToolchain/PlanFuzz.sln"
)
$testContract = "eng/test-counts.json"
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

foreach ($solution in $solutions) {
    Invoke-CheckedNative $dotnet (New-RestoreArguments $solution)
}
foreach ($project in $markdownSampleProjects) {
    Invoke-CheckedNative $dotnet (New-RestoreArguments $project)
}

foreach ($solution in $solutions) {
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
}

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

Invoke-CheckedNative "python" @(
    "Tools/run-test-contract.py",
    "--root", $root,
    "--manifest", $testContract,
    "--dotnet", $dotnet,
    "--configuration", $Configuration
)
Invoke-CheckedNative "python" @(
    "Tools/test-test-contract-mutants.py",
    "--root", $root,
    "--manifest", $testContract,
    "--results-directory", "artifacts/test-contract"
)

if (-not $SkipPack) {
    Remove-Item "artifacts/packages" -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path "artifacts/packages" | Out-Null
    $packageProjects = Read-ValidationManifest $packageManifest
    if ($packageProjects.Count -eq 0) {
        throw "No package projects declared in $packageManifest"
    }
    # Some package projects are intentionally outside the solution graphs.
    # Restore every manifest entry before the --no-restore pack step so a
    # clean checkout cannot depend on stale obj/project.assets.json files.
    foreach ($project in $packageProjects) {
        Invoke-CheckedNative $dotnet (New-RestoreArguments $project)
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

    Invoke-CheckedNative "python" @("Tools/check-wist-api-compatibility.py")
    Invoke-CheckedNative "python" @("Tools/test-wist-api-compatibility-mutants.py", "--root", $root)

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
    $wistPackage = "artifacts/packages/UniversalToolchain.Wist.$wistVersion.nupkg"
    $wistReferenceAssembly = Get-ChildItem "UniversalToolchain/UniversalToolchain.Wist/bin" -Recurse -File -Filter "UniversalToolchain.Wist.dll" |
        Where-Object { $_.FullName -match "[\\/]$Configuration[\\/]net10\.0[\\/]UniversalToolchain\.Wist\.dll$" } |
        Select-Object -First 1
    if (-not $wistReferenceAssembly) { throw "Trusted Wist build output was not found." }
    $wistReferenceDir = $wistReferenceAssembly.Directory.FullName
    $wistCompileReference = Get-ChildItem "UniversalToolchain/UniversalToolchain.Wist/obj" -Recurse -File -Filter "UniversalToolchain.Wist.dll" |
        Where-Object { $_.FullName -match "[\/]$Configuration[\/]net10\.0[\/]ref[\/]UniversalToolchain\.Wist\.dll$" } |
        Select-Object -First 1
    if (-not $wistCompileReference) { throw "Trusted Wist compile reference was not found." }
    Invoke-CheckedNative "python" @(
        "Tools/check-wist-package-surface.py",
        "--reference-dir", $wistReferenceDir,
        "--compile-reference", $wistCompileReference.FullName,
        $wistPackage
    )
    Invoke-CheckedNative "python" @(
        "Tools/test-wist-package-surface-mutants.py",
        "--root", $root,
        "--reference-dir", $wistReferenceDir,
        "--compile-reference", $wistCompileReference.FullName,
        $wistPackage
    )
    Invoke-CheckedNative "python" @(
        "Tools/smoke-wist-package.py",
        "--package-dir", "artifacts/packages",
        "--version", $wistVersion,
        "--dotnet", $dotnet
    )

    Invoke-CheckedNative "python" @(
        "Tools/smoke-language-sdk-packages.py",
        "--root", $root,
        "--packages", "artifacts/packages",
        "--dotnet", $dotnet
    )


    $releaseArtifacts = Get-ChildItem "artifacts/packages" -File |
        Where-Object { $_.Name.EndsWith(".nupkg") -or $_.Name.EndsWith(".snupkg") } |
        Sort-Object Name
    if ($releaseArtifacts.Count -eq 0) {
        throw "No release package artifacts found."
    }
    $releaseArtifactPaths = @($releaseArtifacts | ForEach-Object { "packages/$($_.Name)" })
    $integrityWriteArgs = @(
        "Tools/release-integrity.py", "write",
        "--base", "artifacts",
        "--manifest", "artifacts/RELEASE-INTEGRITY.json",
        "--root-output", "artifacts/RELEASE-INTEGRITY.root.sha256"
    ) + $releaseArtifactPaths
    Invoke-CheckedNative "python" $integrityWriteArgs
    Invoke-CheckedNative "python" @(
        "Tools/release-integrity.py", "verify",
        "--base", "artifacts",
        "--manifest", "artifacts/RELEASE-INTEGRITY.json",
        "--expected-root-file", "artifacts/RELEASE-INTEGRITY.root.sha256"
    )
    Invoke-CheckedNative "python" @(
        "Tools/test-release-integrity-mutants.py",
        "--root", $root,
        $wistPackage
    )
}

if (-not $SkipDocs) {
    Invoke-CheckedNative "npm" @("ci", "--no-audit", "--no-fund")
    Invoke-CheckedNative "npm" @("run", "docs:status")
    Invoke-CheckedNative "npm" @("run", "docs:links")
    Invoke-CheckedNative "npm" @("run", "docs:build")
    Invoke-CheckedNative "python" @(".github/scripts/run-markdown-bash-blocks.py")
}
