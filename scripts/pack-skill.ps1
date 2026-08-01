<#
.SYNOPSIS
    Lays out the Claude Code skill under dist/skill/, validator included.

.DESCRIPTION
    The skill is a separate download from the Dynamo package and shares nothing with it. Nothing
    laid out here ever lands in a folder Revit loads assemblies from, which is what makes it
    reasonable to ship a small executable alongside the Markdown: the "exactly one code assembly"
    rule that governs src/Interlude is about the package, and this is not the package.

    The checker is published at Dynamo 3.0, which is the net8.0-windows build. Interlude's oldest
    supported host is Dynamo 3.0, whose machines have the .NET 8 desktop runtime and may have
    nothing newer; a Dynamo 4.0 machine has .NET 10 and may have nothing older. The project sets
    RollForward to LatestMajor, so one net8.0 build runs on both.

    The schema reference is generated rather than copied. It is checked in as well, because the
    skill has to be readable in the repository and a generated file that only exists at pack time
    is not; SkillTests fails the build when the two disagree.

.PARAMETER Configuration
    Release by default.

.PARAMETER Version
    Skill version. Defaults to the value in versions.json, matching the package.

.EXAMPLE
    ./scripts/pack-skill.ps1
    ./scripts/pack-skill.ps1 -Version 1.1.0
#>

[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$manifest = Get-Content (Join-Path $repoRoot 'versions.json') -Raw | ConvertFrom-Json

if (-not $Version) {
    $Version = ($manifest.assemblyVersion -split '\.')[0..2] -join '.'
}

$skillSource = Join-Path $repoRoot 'skills\interlude-form'
$skillRoot = Join-Path $repoRoot 'dist\skill\interlude-form'

if (Test-Path (Split-Path -Parent $skillRoot)) {
    Remove-Item (Split-Path -Parent $skillRoot) -Recurse -Force
}

New-Item -ItemType Directory -Path $skillRoot -Force | Out-Null

# The skill itself: what Claude reads, and what a person reads before installing it.
foreach ($file in @('SKILL.md', 'README.md')) {
    Copy-Item (Join-Path $skillSource $file) $skillRoot -Force
}

Copy-Item (Join-Path $skillSource 'reference') $skillRoot -Recurse -Force

# The same nine forms the test suite validates on every build. Copied rather than written out
# again so the skill's examples cannot drift from the ones that are checked.
Copy-Item (Join-Path $repoRoot 'samples') (Join-Path $skillRoot 'samples') -Recurse -Force
Remove-Item (Join-Path $skillRoot 'samples\README.md') -Force -ErrorAction SilentlyContinue

Copy-Item (Join-Path $repoRoot 'LICENSE') (Join-Path $skillRoot 'LICENSE.txt') -Force

# The validator. Framework-dependent: a self-contained publish would also run everywhere and would
# put ninety megabytes in a zip whose other contents are five Markdown files.
$binPath = Join-Path $skillRoot 'bin'

Write-Host "publishing the checker..." -ForegroundColor Cyan

dotnet publish (Join-Path $repoRoot 'tools\Interlude.Check\Interlude.Check.csproj') `
    -c $Configuration `
    -p:DynamoVersion=3.0 `
    -o $binPath `
    --nologo `
    -v quiet

if ($LASTEXITCODE -ne 0) {
    throw "Publishing the checker failed."
}

# Publishing a project that references Interlude brings its XML documentation and the Dynamo
# customization file along; both are for Dynamo's library and help panel, and there is neither
# here. ProtoGeometry and DynamoUnits arrive with the ZeroTouchLibrary meta-package rather than
# because anything uses them — Interlude compiles against exactly one Dynamo assembly,
# DynamoServices, for the attributes on its public types. Dropping them takes a third off the
# download, and the sample run below is what proves nothing loads them.
$unwanted = @(
    'Interlude.xml'
    'Interlude_DynamoCustomization.xml'
    '*.pdb'
    'ProtoGeometry.dll'
    'DynamoUnits.dll'
)

foreach ($file in $unwanted) {
    Remove-Item (Join-Path $binPath $file) -Force -ErrorAction SilentlyContinue
}

$checker = Join-Path $binPath 'interlude-check.exe'

if (-not (Test-Path $checker)) {
    throw "The checker did not appear at $checker."
}

# Proof the thing in the box works, against the samples in the box. A skill whose validator does
# not run is worse than one with no validator, because the skill will claim the form was checked.
Write-Host "checking the bundled samples..." -ForegroundColor Cyan

& $checker (Join-Path $skillRoot 'samples')

if ($LASTEXITCODE -ne 0) {
    throw "The bundled checker rejected the bundled samples."
}

Write-Host ""
Write-Host "skill $Version -> $skillRoot" -ForegroundColor Green
