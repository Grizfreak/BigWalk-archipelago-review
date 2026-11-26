<#
.SYNOPSIS
    Builds the mod and the all-in-one zip (BepInEx plus the plugin) for another player.
#>
[CmdletBinding()]
param(
    # The game's folder: -GameDir, or the BIGWALK_DIR variable.
    [string]$GameDir = ($env:BIGWALK_DIR -split ';')[0],
    [string]$Configuration = 'Release',
    [string]$OutDir = (Join-Path $PSScriptRoot '..\dist')
)

$ErrorActionPreference = 'Stop'

$repo = Resolve-Path (Join-Path $PSScriptRoot '..')
$sln = Join-Path $repo 'mod\BigWalkArchipelago.sln'
$buildOut = Join-Path $repo "mod\src\bin\$Configuration\net6.0"

if (-not $GameDir) { throw 'Pass -GameDir or set BIGWALK_DIR to the game folder.' }
if (-not (Test-Path $GameDir)) {
    throw "Game folder not found: $GameDir"
}
if (-not (Test-Path (Join-Path $GameDir 'BepInEx\core\BepInEx.Unity.IL2CPP.dll'))) {
    throw "No BepInEx IL2CPP in $GameDir - nothing to package."
}

Write-Host '== Build ==' -ForegroundColor Cyan
dotnet build $sln -c $Configuration
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

# Leaving out one of the three DLLs fails the whole mod: BepInEx resolves deps in its folder.
$pluginDlls = @(
    'BigWalkArchipelago.dll',
    'Archipelago.MultiClient.Net.dll',
    'Newtonsoft.Json.dll'
)

$version = (Get-Item (Join-Path $buildOut 'BigWalkArchipelago.dll')).VersionInfo.FileVersion
$stamp = Get-Date -Format 'yyyyMMdd'
$stage = Join-Path $env:TEMP "bigwalk-ap-package-$stamp-$PID"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage -Force | Out-Null

Write-Host '== Staging ==' -ForegroundColor Cyan

# The loader (Doorstop), at the root of the game folder.
Copy-Item (Join-Path $GameDir 'doorstop_config.ini') $stage
Copy-Item (Join-Path $GameDir 'winhttp.dll') $stage
Copy-Item (Join-Path $GameDir 'dotnet') (Join-Path $stage 'dotnet') -Recurse

# BepInEx, without interop/, cache/, config/ or the logs.
foreach ($sub in @('core', 'patchers')) {
    $src = Join-Path $GameDir "BepInEx\$sub"
    if (Test-Path $src) {
        Copy-Item $src (Join-Path $stage "BepInEx\$sub") -Recurse
    }
}

# Every license the package owes, and the index of which is whose (THIRD-PARTY-NOTICES.txt).
$packaging = Join-Path $repo 'mod\packaging'
Copy-Item (Join-Path $packaging 'THIRD-PARTY-NOTICES.txt') $stage
Copy-Item (Join-Path $packaging 'licenses') (Join-Path $stage 'BepInEx\licenses') -Recurse
# The plugin's own license, from the repository root.
Copy-Item (Join-Path $repo 'LICENSE') (Join-Path $stage 'BepInEx\licenses\BigWalkArchipelago.MIT.txt')

$pluginDir = Join-Path $stage 'BepInEx\plugins\BigWalkArchipelago'
New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
foreach ($dll in $pluginDlls) {
    $src = Join-Path $buildOut $dll
    if (-not (Test-Path $src)) { throw "DLL missing from the build output: $dll" }
    Copy-Item $src $pluginDir
}

# The zip's own instructions, kept next to the licenses it ships with.
Copy-Item (Join-Path $packaging 'README.txt') $stage

if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir -Force | Out-Null }
$zip = Join-Path (Resolve-Path $OutDir) 'BigWalkArchipelago.zip'
if (Test-Path $zip) { Remove-Item $zip -Force }

Write-Host '== Compressing ==' -ForegroundColor Cyan
# Not Compress-Archive: PS 5.1 writes backslashes in entry names, which Linux unzip keeps.
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$stageRoot = (Resolve-Path $stage).Path.TrimEnd('\') + '\'
$archive = [System.IO.Compression.ZipFile]::Open($zip, 'Create')
try {
    foreach ($file in Get-ChildItem $stage -Recurse -File) {
        $entry = $file.FullName.Substring($stageRoot.Length).Replace('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $archive, $file.FullName, $entry, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
}
finally {
    $archive.Dispose()
}
Remove-Item $stage -Recurse -Force

$size = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host ''
Write-Host "Package ready: $zip, version $version ($size MB)" -ForegroundColor Green
