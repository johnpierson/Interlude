<#
.SYNOPSIS
    Lays out a Dynamo package per Dynamo version under dist/.

.DESCRIPTION
    A Dynamo package is a folder, not an archive: pkg.json beside bin/, dyf/ and extra/. One is
    produced per Dynamo version, because a package declares a single engine version and the
    assembly inside it is built against a specific framework.

    Copy the folder for your Dynamo version into your packages directory, or point the Dynamo
    package manager at it. See docs/installing.md.

.PARAMETER Configuration
    Must match what build-all.ps1 produced. Release by default.

.PARAMETER Version
    Package version. Defaults to the value in versions.json.

.EXAMPLE
    ./scripts/build-all.ps1 -Pack
    ./scripts/pack.ps1 -Version 1.1.0
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
    # AssemblyVersion is frozen at 1.0.0.0 so graphs never break on upgrade; the package version
    # is the one that actually moves.
    $Version = ($manifest.assemblyVersion -split '\.')[0..2] -join '.'
}

$distRoot = Join-Path $repoRoot 'dist'
if (Test-Path $distRoot) {
    Remove-Item $distRoot -Recurse -Force
}

$description = @'
Declarative forms for Dynamo. Describe a form with nodes, show it, and get typed answers back.

Conditional visibility, computed values and live validation are described declaratively rather
than wired up. Cancelling returns every field's default rather than nulls. Ships as one assembly
with no runtime dependencies, plus a resource file holding the node icons.
'@

foreach ($target in @($manifest.versions | Where-Object { $_.active })) {
    $dynamo = $target.dynamo
    $buildPath = Join-Path $repoRoot "artifacts\build\$dynamo\$Configuration\$($target.targetFramework)"

    if (-not (Test-Path $buildPath)) {
        throw "No build output for Dynamo $dynamo at $buildPath. Run scripts/build-all.ps1 first."
    }

    $packageRoot = Join-Path $distRoot "dynamo-$dynamo\Interlude"
    $binPath = Join-Path $packageRoot 'bin'

    New-Item -ItemType Directory -Path $binPath -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $packageRoot 'dyf') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $packageRoot 'extra') -Force | Out-Null

    foreach ($file in @('Interlude.dll', 'Interlude.xml', 'Interlude_DynamoCustomization.xml')) {
        Copy-Item (Join-Path $buildPath $file) $binPath -Force
    }

    # Node icons. Dynamo looks for them in a sibling assembly whose name is the node library's plus
    # ".customization", in the same folder, and nowhere else. The same file serves all three Dynamo
    # versions: it holds PNGs and no code, so there is nothing in it to build per framework.
    $iconAssembly = Join-Path $repoRoot "src\Interlude.Icons\bin\$Configuration\netstandard2.0\Interlude.customization.dll"

    if (Test-Path $iconAssembly) {
        Copy-Item $iconAssembly $binPath -Force
    }
    else {
        Write-Warning "The icon assembly is missing: this package's nodes will show Dynamo's default icon. Run scripts/build-all.ps1 first."
    }

    # Node help. Dynamo's documentation browser looks for a folder named exactly 'doc' beside bin/
    # and matches each Markdown file to a node by its file name, so the panel beside the graph
    # shows our own help rather than "no documentation available".
    #
    # These are generated from the assembly by the preview harness (--docs) and checked in under
    # docs/nodes; copied rather than generated here because dist/ is wiped on every pack and
    # anything written straight into it would not survive the next one.
    $nodeDocs = Join-Path $repoRoot 'docs\nodes'
    if (Test-Path $nodeDocs) {
        $docPath = Join-Path $packageRoot 'doc'
        New-Item -ItemType Directory -Path $docPath -Force | Out-Null

        # Only the node files and their images. The folder's own README explains how they are
        # generated, which is a thing for contributors rather than something to hand to Dynamo's
        # documentation browser as if it were a node.
        Copy-Item (Join-Path $nodeDocs 'Interlude.*') $docPath -Force
    }
    else {
        Write-Warning "docs/nodes is missing: this package will ship without node help."
    }

    # Samples ship in extra/ so a user can open a real form definition without leaving Dynamo.
    $samples = Join-Path $repoRoot 'samples'
    if (Test-Path $samples) {
        Copy-Item $samples (Join-Path $packageRoot 'extra\samples') -Recurse -Force
    }

    Copy-Item (Join-Path $repoRoot 'LICENSE') (Join-Path $packageRoot 'extra\LICENSE.txt') -Force

    # The SIL Open Font License requires the licence to travel with the font, and the font is
    # embedded in Interlude.dll. See THIRD-PARTY-NOTICES.md.
    Copy-Item (Join-Path $repoRoot 'src\Interlude\Fonts\SpaceGrotesk-OFL.txt') `
              (Join-Path $packageRoot 'extra\SpaceGrotesk-OFL.txt') -Force
    Copy-Item (Join-Path $repoRoot 'THIRD-PARTY-NOTICES.md') `
              (Join-Path $packageRoot 'extra\THIRD-PARTY-NOTICES.md') -Force

    $engineVersion = "$dynamo.0.0"
    $revitYears = $target.revitYears -join ', '

    $pkg = [ordered]@{
        license          = 'MIT'
        file_hash        = $null
        name             = 'Interlude'
        version          = $Version
        description      = $description
        group            = ''
        keywords         = @('forms', 'ui', 'dialog', 'input', 'user interface', 'data shapes')
        dependencies     = @()
        contents         = 'Input, Layout, Behavior, Condition, Compute, Rule, Theme, Form, Result'
        engine_version   = $engineVersion
        engine           = 'dynamo'
        engine_metadata  = "Dynamo $dynamo ($($target.targetFramework)); Revit $revitYears"
        site_url         = 'https://github.com/johnpierson/Interlude'
        repository_url   = 'https://github.com/johnpierson/Interlude'
        contains_binaries = $true
        node_libraries   = @("Interlude, Version=$($manifest.assemblyVersion), Culture=neutral, PublicKeyToken=null")
    }

    # Written without a byte-order mark: Windows PowerShell's -Encoding utf8 adds one, and a BOM
    # in front of the opening brace is the kind of thing a strict JSON reader refuses.
    $json = $pkg | ConvertTo-Json -Depth 5
    [System.IO.File]::WriteAllText(
        (Join-Path $packageRoot 'pkg.json'),
        $json,
        (New-Object System.Text.UTF8Encoding $false))

    Write-Host "packed Dynamo $dynamo -> $packageRoot" -ForegroundColor Green
}

Write-Host ""
Write-Host "Packages are in $distRoot" -ForegroundColor Cyan
