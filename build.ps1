param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipDocs,
    [switch]$SkipPack,
    [int]$Jobs = 0,
    [switch]$Serial,
    [switch]$NoBuildServers,
    [string]$BaselineSourceArchive = $env:WIST_BASELINE_SOURCE_ARCHIVE,
    [string]$PreviousPackageBundle = $env:WIST_PREVIOUS_PACKAGE_BUNDLE
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

# NuGet.config declares this repository-local feed. It is optional in clean
# checkouts, but NuGet requires every configured local source path to exist.
New-Item -ItemType Directory -Force -Path "UniversalToolchain/packages" | Out-Null

$env:PLATFORM = $null

if ($Jobs -eq 0) {
    if ($env:WIST_BUILD_JOBS) {
        if (-not [int]::TryParse($env:WIST_BUILD_JOBS, [ref]$Jobs) -or $Jobs -lt 1) {
            throw "WIST_BUILD_JOBS must be a positive integer, got: $($env:WIST_BUILD_JOBS)"
        }
    }
    else {
        $Jobs = [Math]::Max(1, [Environment]::ProcessorCount)
    }
}
elseif ($Jobs -lt 1) {
    throw "-Jobs must be a positive integer, got: $Jobs"
}

if ($Serial -and $Jobs -ne 1 -and ($PSBoundParameters.ContainsKey("Jobs") -or $env:WIST_BUILD_JOBS)) {
    throw "-Serial conflicts with an explicit job count. Remove -Jobs/WIST_BUILD_JOBS or set it to 1."
}

$buildInParallel = if ($Serial) { "false" } else { "true" }
$restoreInParallel = if ($Serial) { "false" } else { "true" }
$sharedCompilation = if ($NoBuildServers) { "false" } else { "true" }
$restoreModeArguments = if ($Serial) { @("--disable-parallel") } else { @() }
$buildServerArguments = if ($NoBuildServers) { @("--disable-build-servers") } else { @() }
if ($Serial) {
    $Jobs = 1
}
if ($NoBuildServers) {
    $env:DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER = "1"
    $env:MSBUILDDISABLENODEREUSE = "1"
}

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

    $arguments = @("restore", $Project) + $restoreModeArguments + $buildServerArguments + @(
        "-p:RestoreBuildInParallel=$restoreInParallel",
        "-p:UseSharedCompilation=$sharedCompilation",
        "-p:NuGetAudit=false"
    )
    if ($env:NUGET_CONFIG) {
        $arguments += @("--configfile", $env:NUGET_CONFIG)
    }
    return $arguments
}

Invoke-CheckedNative "python" @("Tools/check-build-topology.py", "--root", $root)
Invoke-CheckedNative "python" @("Tools/test-build-topology-mutants.py", "--root", $root)

foreach ($solution in $solutions) {
    Invoke-CheckedNative $dotnet (New-RestoreArguments $solution)
}
foreach ($project in $markdownSampleProjects) {
    Invoke-CheckedNative $dotnet (New-RestoreArguments $project)
}

foreach ($solution in $solutions) {
    Invoke-CheckedNative $dotnet (@(
        "build", $solution,
        "-c", $Configuration,
        "--no-restore"
    ) + $buildServerArguments + @(
        "-m:$Jobs",
        "-p:BuildInParallel=$buildInParallel",
        "-p:UseSharedCompilation=$sharedCompilation",
        "-p:NuGetAudit=false"
    ))
}

Invoke-CheckedNative "python" @(
    "Tools/test-build-topology-runtime.py",
    "--root", $root,
    "--dotnet", $dotnet,
    "--configuration", $Configuration
)

foreach ($project in $markdownSampleProjects) {
    Invoke-CheckedNative $dotnet (@(
        "build", $project,
        "-c", $Configuration,
        "--no-restore"
    ) + $buildServerArguments + @(
        "-m:$Jobs",
        "-p:BuildInParallel=$buildInParallel",
        "-p:UseSharedCompilation=$sharedCompilation",
        "-p:NuGetAudit=false"
    ))
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
Invoke-CheckedNative "python" @("Tools/check-retired-surface.py", "--root", $root)
Invoke-CheckedNative "python" @("Tools/test-retired-surface-mutants.py", "--root", $root)
Invoke-CheckedNative "python" @("Tools/check_documentation_status.py")
Invoke-CheckedNative "python" @("Tools/test-documentation-status-mutants.py", "--root", $root)

if (-not $SkipPack) {
    if (-not $BaselineSourceArchive -or -not (Test-Path -LiteralPath $BaselineSourceArchive -PathType Leaf)) {
        throw "Packaging requires -BaselineSourceArchive (or WIST_BASELINE_SOURCE_ARCHIVE) pointing to the reviewed previous source ZIP."
    }
    if (-not $PreviousPackageBundle -or -not (Test-Path -LiteralPath $PreviousPackageBundle -PathType Leaf)) {
        throw "Packaging requires -PreviousPackageBundle (or WIST_PREVIOUS_PACKAGE_BUNDLE) pointing to the reviewed previous package bundle."
    }
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
        Invoke-CheckedNative $dotnet (@(
            "pack", $project,
            "-c", $Configuration,
            "--no-restore"
        ) + $buildServerArguments + @(
            "-m:$Jobs",
            "-p:BuildInParallel=$buildInParallel",
            "-o", "artifacts/packages",
            "/p:WarningsAsErrors=NU5118",
            "-p:UseSharedCompilation=$sharedCompilation",
            "-p:NuGetAudit=false"
        ))
    }
    $packedArchives = @(Get-ChildItem "artifacts/packages" -File |
        Where-Object { $_.Name.EndsWith(".nupkg") -or $_.Name.EndsWith(".snupkg") } |
        Sort-Object Name |
        ForEach-Object { $_.FullName })
    Invoke-CheckedNative "python" (@("Tools/repack-nupkg-deterministic.py") + $packedArchives)

    Invoke-CheckedNative "python" @(
        "Tools/check-package-version-provenance.py",
        "--previous-bundle", $PreviousPackageBundle,
        "--current-packages", "artifacts/packages",
        "--baseline-contract", "eng/package-release-baseline.json"
    )
    Invoke-CheckedNative "python" @("Tools/test-package-version-provenance-mutants.py", "--root", $root)
    Invoke-CheckedNative "python" @("Tools/check-wist-api-compatibility.py", "--baseline-source-archive", $BaselineSourceArchive)
    Invoke-CheckedNative "python" @(
        "Tools/test-wist-api-compatibility-mutants.py",
        "--root", $root,
        "--baseline-source-archive", $BaselineSourceArchive
    )

    Invoke-CheckedNative "python" @(
        "Tools/check-language-sdk-package-matrix.py",
        "--root", $root,
        "--manifest", $packageManifest,
        "--packages", "artifacts/packages"
    )

    Invoke-CheckedNative "python" @(
        "Tools/package_metadata.py",
        "--root", $root,
        "--manifest", $packageManifest,
        "--packages", "artifacts/packages",
        "--previous-bundle", $PreviousPackageBundle,
        "--baseline-contract", "eng/package-release-baseline.json",
        "--report", "artifacts/package-metadata-report.json"
    )
    Invoke-CheckedNative "python" @("Tools/test-package-metadata-mutants.py", "--root", $root)

    $wistVersionNode = Select-Xml `
        -Path "UniversalToolchain/UniversalToolchain.Wist/UniversalToolchain.Wist.csproj" `
        -XPath "//*[local-name()='Version']" |
        Select-Object -First 1
    $wistVersion = if ($wistVersionNode) { [string]$wistVersionNode.Node.InnerText } else { $null }
    if (-not $wistVersion) {
        throw "UniversalToolchain.Wist package version was not found."
    }
    $wistPackage = "artifacts/packages/UniversalToolchain.Wist.$wistVersion.nupkg"
    $wistReferenceAssemblyPath = Join-Path $root "UniversalToolchain/UniversalToolchain.Wist/bin/$Configuration/net10.0/UniversalToolchain.Wist.dll"
    if (-not (Test-Path -LiteralPath $wistReferenceAssemblyPath -PathType Leaf)) { throw "Trusted Wist build output was not found." }
    $wistReferenceDir = Split-Path -Parent $wistReferenceAssemblyPath
    $wistCompileReferencePath = Join-Path $root "UniversalToolchain/UniversalToolchain.Wist/obj/$Configuration/net10.0/ref/UniversalToolchain.Wist.dll"
    if (-not (Test-Path -LiteralPath $wistCompileReferencePath -PathType Leaf)) { throw "Trusted Wist compile reference was not found." }
    Invoke-CheckedNative "python" @(
        "Tools/check-wist-package-surface.py",
        "--reference-dir", $wistReferenceDir,
        "--compile-reference", $wistCompileReferencePath,
        $wistPackage
    )
    Invoke-CheckedNative "python" @(
        "Tools/test-wist-package-surface-mutants.py",
        "--root", $root,
        "--reference-dir", $wistReferenceDir,
        "--compile-reference", $wistCompileReferencePath,
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
