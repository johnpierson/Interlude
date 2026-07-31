<#
.SYNOPSIS
    Builds Interlude once per Dynamo version and verifies each output.

.DESCRIPTION
    Interlude ships one assembly per supported Dynamo version, driven by versions.json. Each
    build is verified before it is allowed to become a package: exactly one DLL, the XML
    documentation Dynamo reads for port names, and the customization file that puts the nodes
    under a single library category.

    That verification is the point of this script. The failure it exists to catch is a transitive
    package quietly adding a second DLL to the output, which would then be hand-copied into a
    folder Revit shares with every other add-in and would sooner or later collide with someone
    else's copy of the same library.

.PARAMETER Configuration
    Release by default.

.PARAMETER DynamoVersion
    Build only this Dynamo version instead of every active one.

.PARAMETER Pack
    Also lay out a Dynamo package per version under dist/.

.EXAMPLE
    ./scripts/build-all.ps1
    ./scripts/build-all.ps1 -DynamoVersion 4.0 -Pack
#>

[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $DynamoVersion,
    [switch] $Pack
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src\Interlude\Interlude.csproj'
$versionsFile = Join-Path $repoRoot 'versions.json'

if (-not (Test-Path $versionsFile)) {
    throw "versions.json not found at $versionsFile."
}

$manifest = Get-Content $versionsFile -Raw | ConvertFrom-Json
$targets = @($manifest.versions | Where-Object { $_.active })

if ($DynamoVersion) {
    $targets = @($targets | Where-Object { $_.dynamo -eq $DynamoVersion })

    if ($targets.Count -eq 0) {
        throw "No active entry in versions.json for Dynamo $DynamoVersion."
    }
}

# Every file that must be in a package folder, and nothing else may be a DLL.
$requiredFiles = @('Interlude.dll', 'Interlude.xml', 'Interlude_DynamoCustomization.xml')

$results = @()

foreach ($target in $targets) {
    $version = $target.dynamo
    Write-Host ""
    Write-Host "=== Dynamo $version ($($target.targetFramework)) ===" -ForegroundColor Cyan

    & dotnet build $project `
        -c $Configuration `
        -p:DynamoVersion=$version `
        -p:ContinuousIntegrationBuild=true `
        --nologo `
        -v minimal

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for Dynamo $version."
    }

    $outputPath = Join-Path $repoRoot "artifacts\build\$version\$Configuration\$($target.targetFramework)"

    if (-not (Test-Path $outputPath)) {
        throw "Expected build output at $outputPath but it is not there."
    }

    foreach ($file in $requiredFiles) {
        $path = Join-Path $outputPath $file
        if (-not (Test-Path $path)) {
            throw "Dynamo ${version}: $file is missing from $outputPath. Dynamo needs all three."
        }
    }

    # The zero-dependency rule. The csproj enforces this at build time too; repeating it here
    # means the packaging step cannot be bypassed by building the project directly.
    $assemblies = @(Get-ChildItem -Path $outputPath -Filter '*.dll' | Where-Object { $_.Name -ne 'Interlude.dll' })
    if ($assemblies.Count -gt 0) {
        $names = ($assemblies | Select-Object -ExpandProperty Name) -join ', '
        throw "Dynamo ${version}: the output contains assemblies other than Interlude.dll ($names). Interlude must ship exactly one."
    }

    $assembly = Get-Item (Join-Path $outputPath 'Interlude.dll')
    $fileVersion = $assembly.VersionInfo.FileVersion

    Write-Host "  built  : $($assembly.Name) ($fileVersion, $([math]::Round($assembly.Length / 1KB)) KB)" -ForegroundColor Green
    Write-Host "  output : $outputPath"

    $results += [pscustomobject]@{
        Dynamo          = $version
        TargetFramework = $target.targetFramework
        RevitYears      = $target.revitYears -join ', '
        Output          = $outputPath
        FileVersion     = $fileVersion
    }
}

Write-Host ""
Write-Host "Built $($results.Count) assembly/assemblies:" -ForegroundColor Cyan
$results | Format-Table Dynamo, TargetFramework, RevitYears, FileVersion -AutoSize

if ($Pack) {
    & (Join-Path $PSScriptRoot 'pack.ps1') -Configuration $Configuration
}
