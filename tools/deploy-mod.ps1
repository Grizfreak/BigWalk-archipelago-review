<#
.SYNOPSIS
    Builds the mod, deploys it to every known install and checks they carry the same binary.
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Debug',
    # The game folders: -Targets, or the BIGWALK_DIR variable (several are separated by ';').
    [string[]]$Targets = @($env:BIGWALK_DIR -split ';' | Where-Object { $_ }),
    # A folder that also gets the mod's DLL alone, for a second machine.
    [string]$SharedDrop = '',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

if ($Targets.Count -eq 0) { throw 'Pass -Targets or set BIGWALK_DIR to the game folder.' }

$repo = Resolve-Path (Join-Path $PSScriptRoot '..')
$sln = Join-Path $repo 'mod\BigWalkArchipelago.sln'
$buildOut = Join-Path $repo "mod\src\bin\$Configuration\net6.0"

# Leaving out one of the three DLLs fails the whole mod: BepInEx resolves deps in its folder.
$dlls = @(
    'BigWalkArchipelago.dll',
    'Archipelago.MultiClient.Net.dll',
    'Newtonsoft.Json.dll'
)

if (-not $SkipBuild) {
    Write-Host '== Build ==' -ForegroundColor Cyan
    dotnet build $sln -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
}

foreach ($dll in $dlls) {
    if (-not (Test-Path (Join-Path $buildOut $dll))) {
        throw "DLL missing from the build output: $dll"
    }
}

Write-Host ''
Write-Host '== Deploy ==' -ForegroundColor Cyan

$results = @()

foreach ($target in $Targets) {
    if (-not (Test-Path $target)) {
        Write-Host ("  {0,-45} not found, skipped" -f $target) -ForegroundColor DarkGray
        continue
    }

    $pluginDir = Join-Path $target 'BepInEx\plugins\BigWalkArchipelago'
    if (-not (Test-Path $pluginDir)) {
        New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
    }

    $failed = $null
    foreach ($dll in $dlls) {
        try {
            Copy-Item (Join-Path $buildOut $dll) $pluginDir -Force
        }
        catch {
            # Usually the game holding the DLL; said out loud, since a partial deploy is the bug.
            $failed = $_.Exception.Message
            break
        }
    }

    if ($failed) {
        Write-Host ("  {0,-45} FAILED - is the game still running?" -f $target) -ForegroundColor Red
        Write-Host "      $failed" -ForegroundColor DarkRed
        continue
    }

    $hash = (Get-FileHash (Join-Path $pluginDir 'BigWalkArchipelago.dll') -Algorithm SHA256).Hash
    $results += [pscustomobject]@{ Target = $target; Hash = $hash }
    Write-Host ("  {0,-45} OK" -f $target) -ForegroundColor Green
}

# The second machine takes its copy from the share's root: one copy on its side per update.
if ($SharedDrop -and (Test-Path $SharedDrop)) {
    try {
        Copy-Item (Join-Path $buildOut 'BigWalkArchipelago.dll') $SharedDrop -Force
        Write-Host ("  {0,-45} OK (DLL for the other machine)" -f $SharedDrop) -ForegroundColor Green
    }
    catch {
        Write-Host ("  {0,-45} FAILED" -f $SharedDrop) -ForegroundColor Red
        Write-Host "      $($_.Exception.Message)" -ForegroundColor DarkRed
    }
}

Write-Host ''

if ($results.Count -eq 0) {
    Write-Host 'No installation was updated.' -ForegroundColor Red
    exit 1
}

# Forced into an array: with one fingerprint Select-Object returns a string, [0] a char.
$distinct = @($results.Hash | Select-Object -Unique)
Write-Host ("Mod fingerprint: {0}" -f $distinct[0].Substring(0, 16))

if ($distinct.Count -eq 1) {
    Write-Host ("{0} installation(s) up to date and identical." -f $results.Count) -ForegroundColor Green
} else {
    # Should not happen, since everything comes from the same build output.
    Write-Host 'WARNING: the installations do not carry the same binary.' -ForegroundColor Red
    $results | ForEach-Object { "  {0}  {1}" -f $_.Hash.Substring(0, 16), $_.Target }
}
