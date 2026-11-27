<#
.SYNOPSIS
    Starts a second instance of the game on this PC as the loopback guest, joining the first.
.EXAMPLE
    .\launch-guest.ps1 -NetSim wifi
    Starts tools/netsim.py with the wifi preset in its own window; the guest joins through it.
.EXAMPLE
    .\launch-guest.ps1 -NetSim bad -NetSimArgs '--seed 12 --cut 8 --every 60'
#>
[CmdletBinding()]
param(
    # The game's folder: -GameDir, or the BIGWALK_DIR variable.
    [string]$GameDir = ($env:BIGWALK_DIR -split ';')[0],
    [int]$Width = 1280,
    [int]$Height = 720,
    [switch]$NoJoin,
    [switch]$Vanilla,
    [switch]$Force,
    # A preset of tools/netsim-presets.json: the guest joins through tools/netsim.py.
    [string]$NetSim = '',
    # More options for netsim.py, such as '--seed 12 --cut 8 --every 60'.
    [string]$NetSimArgs = '',
    [int]$WatchGuest = 0,
    [string]$Snapshot = '',
    [string]$NetSimStop = ''
)

$ErrorActionPreference = 'Stop'

$appId = '1478500'
$kcpPort = 7777
# netsim.py's listening port; the host keeps $kcpPort.
$netSimPort = 7778
$prefsKey = 'HKCU:\Software\House House\Big Walk'
$unityLogDir = Join-Path (Split-Path $env:LOCALAPPDATA) 'LocalLow\House House\Big Walk'

# The hidden watcher: wait for the guest, then put the screen back.
if ($WatchGuest -ne 0) {
    $guest = Get-Process -Id $WatchGuest -ErrorAction SilentlyContinue
    if ($guest) { $guest.WaitForExit() }

    # Up to three guests share these settings: put them back once the last one is gone.
    do {
        Start-Sleep -Seconds 2
        $guests = @(Get-CimInstance Win32_Process -Filter "Name='Big Walk.exe'" -ErrorAction SilentlyContinue |
            Where-Object { $_.CommandLine -like '*--bwap-guest*' -or $_.CommandLine -like '*--bwap-vanilla*' })
    } while ($guests.Count -gt 0)

    # netsim.py sees the file, writes its stats and exits.
    if ($NetSimStop) { Set-Content -Path $NetSimStop -Value 'stop' }
    if (-not $Snapshot -or -not (Test-Path $Snapshot)) { exit 0 }

    # Not wrapped in @(): Windows PowerShell's ConvertFrom-Json emits an array as one object.
    $entries = Get-Content $Snapshot -Raw | ConvertFrom-Json
    $names = @()
    foreach ($entry in $entries) {
        $value = $entry.Value
        if ($entry.Kind -eq 'Binary') { $value = [byte[]]$value }
        Set-ItemProperty -Path $prefsKey -Name $entry.Name -Value $value -Type $entry.Kind
        $names += $entry.Name
    }

    # A windowed guest also writes values the host never had; left, they would be the host's.
    foreach ($name in (Get-Item $prefsKey).Property | Where-Object { $_ -like 'Screenmanager *' -and $names -notcontains $_ }) {
        Remove-ItemProperty -Path $prefsKey -Name $name
    }

    Remove-Item $Snapshot -ErrorAction SilentlyContinue
    exit 0
}

if (-not $GameDir) { throw 'Pass -GameDir or set BIGWALK_DIR to the game folder.' }
$exe = Join-Path $GameDir 'Big Walk.exe'
if (-not (Test-Path $exe)) { throw "No game at $exe." }

# The host must already be up, and past the interop generation.
$running = @(Get-Process -Name 'Big Walk' -ErrorAction SilentlyContinue)
if ($running.Count -eq 0 -and -not $Force) {
    throw 'No host is running. Launch the game through Steam first and wait for its main menu (-Force to start the guest alone).'
}
if ($running.Count -ge 2) {
    Write-Host "Warning: $($running.Count) instances are already running; this makes one more." -ForegroundColor Yellow
}

$hosting = $false
if ($running.Count -ge 1) {
    $hostProcess = $running | Sort-Object StartTime | Select-Object -First 1
    $hostLog = Join-Path $GameDir 'BepInEx\LogOutput.log'
    $ready = (Test-Path $hostLog) -and (Get-Item $hostLog).LastWriteTime -ge $hostProcess.StartTime -and
        (Select-String -Path $hostLog -Pattern 'Harmony initialized' -Quiet)
    if (-not $ready -and -not $Force) {
        throw 'The host has not finished loading the mod yet (no "Harmony initialized" in LogOutput.log since it started). Wait for its main menu.'
    }

    $hosting = @(Get-NetUDPEndpoint -LocalPort $kcpPort -ErrorAction SilentlyContinue |
        Where-Object { $_.OwningProcess -eq $hostProcess.Id }).Count -gt 0
}

$join = $hosting -and -not $NoJoin
if ($hosting) {
    Write-Host "The host is hosting (UDP $kcpPort is open)." -ForegroundColor Green
} else {
    Write-Host "The host is not hosting yet (nothing on UDP $kcpPort): host a world, then press Ctrl+L in the guest's window." -ForegroundColor Yellow
}

# The relay: started here with -NetSim, or already running from an earlier guest or by hand.
function Test-NetSim { @(Get-NetUDPEndpoint -LocalPort $netSimPort -ErrorAction SilentlyContinue).Count -gt 0 }
$netSimStop = ''
if ($NetSim -and -not (Test-NetSim)) {
    $python = $null
    foreach ($candidate in @(@('py', '-3'), @('python'))) {
        if (-not (Get-Command $candidate[0] -ErrorAction SilentlyContinue)) { continue }
        $version = & $candidate[0] @($candidate | Select-Object -Skip 1) -c 'import sys; print(sys.version_info >= (3, 12))' 2>$null
        if ($version -eq 'True') { $python = $candidate; break }
    }
    if (-not $python) {
        throw 'netsim needs Python 3.12 or newer (py -3 or python on the PATH). Install it from python.org, or launch without -NetSim.'
    }

    $netSimScript = Join-Path $PSScriptRoot 'netsim.py'
    $netSimStop = Join-Path $env:TEMP "bwap-netsim-stop-$PID"
    Remove-Item $netSimStop -ErrorAction SilentlyContinue
    $netSimArguments = @($python | Select-Object -Skip 1) + @("`"$netSimScript`"", '--preset', $NetSim,
        '--listen', $netSimPort, '--target', $kcpPort, '--stop-file', "`"$netSimStop`"")
    if ($NetSimArgs) { $netSimArguments += $NetSimArgs }
    Start-Process -FilePath $python[0] -ArgumentList $netSimArguments -WorkingDirectory $PSScriptRoot | Out-Null

    $deadline = (Get-Date).AddSeconds(10)
    while ((Get-Date) -lt $deadline -and -not (Test-NetSim)) { Start-Sleep -Milliseconds 300 }
    if (-not (Test-NetSim)) {
        throw "netsim did not start. Run it by hand to see why: $($python -join ' ') `"$netSimScript`" --preset $NetSim $NetSimArgs"
    }
    Write-Host "netsim started in its own window (preset $NetSim): UDP $netSimPort -> $kcpPort. Its log is in tools\netsim-logs." -ForegroundColor Green
}
$viaNetSim = Test-NetSim
if ($viaNetSim -and -not $NetSim) {
    Write-Host "UDP $netSimPort is open (netsim.py): the guest joins through it, as Ctrl+L does." -ForegroundColor Yellow
}

# Which guest this is, 1 to 3; each one after the first sends the host its own identifier.
$index = [Math]::Max(1, $running.Count)

# Save the screen settings the guest will overwrite (first guest only: its watcher waits).
$snapshotFile = Join-Path $env:TEMP 'bwap-guest-screen-prefs.json'
$saved = 0
if ($index -eq 1 -and (Test-Path $prefsKey)) {
    $key = Get-Item $prefsKey
    $entries = @(foreach ($name in $key.Property | Where-Object { $_ -like 'Screenmanager *' }) {
        [pscustomobject]@{ Name = $name; Kind = $key.GetValueKind($name).ToString(); Value = $key.GetValue($name) }
    })
    ConvertTo-Json -InputObject $entries -Depth 3 | Set-Content -Path $snapshotFile -Encoding utf8
    $saved = $entries.Count
}

# Start the guest through the shell, or it would print Unity's boot output in this console.
$unityLog = Join-Path $unityLogDir $(if ($index -gt 1) { "Player-guest$index.log" } else { 'Player-guest.log' })
$role = if ($Vanilla) { "--bwap-vanilla" } else { "--bwap-guest" }
$arguments = $role + " --bwap-guest-index $index" + $(if ($join) { " --bwap-join" } else { "" }) +
    $(if ($viaNetSim) { " --bwap-port $netSimPort" } else { "" }) +
    " -logFile `"$unityLog`" -screen-fullscreen 0 -screen-width $Width -screen-height $Height"

# A BepInEx-guest folder beside BepInEx gives the guest its own plugins and .cfg, as Ctrl+L does.
$guestLoader = Join-Path $GameDir 'BepInEx-guest\core\BepInEx.Unity.IL2CPP.dll'
$ownBepInEx = -not $Vanilla -and (Test-Path $guestLoader)
if ($ownBepInEx) {
    $arguments += " --doorstop-target-assembly `"$guestLoader`""
    Write-Host 'The guest loads BepInEx-guest (its own plugins and .cfg).'
}
$logDirs = @(Join-Path $GameDir 'BepInEx') + $(if ($ownBepInEx) { @(Join-Path $GameDir 'BepInEx-guest') } else { @() })

$previous = @{}
foreach ($name in 'SteamAppId', 'SteamGameId') {
    $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
    [Environment]::SetEnvironmentVariable($name, $appId, 'Process')
}
foreach ($variable in @(Get-ChildItem env: | Where-Object { $_.Name -like 'DOORSTOP_*' })) {
    $previous[$variable.Name] = $variable.Value
    [Environment]::SetEnvironmentVariable($variable.Name, $null, 'Process')
}

$started = Get-Date
try {
    $guest = Start-Process -FilePath $exe -WorkingDirectory $GameDir -ArgumentList $arguments -PassThru
} finally {
    foreach ($name in $previous.Keys) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
}
Write-Host ("Guest started: process {0}, {1}x{2} windowed." -f $guest.Id, $Width, $Height) -ForegroundColor Green

if ($saved -gt 0 -or $netSimStop) {
    $watchArguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"", '-WatchGuest', $guest.Id)
    if ($saved -gt 0) { $watchArguments += @('-Snapshot', "`"$snapshotFile`"") }
    if ($netSimStop) { $watchArguments += @('-NetSimStop', "`"$netSimStop`"") }
    Start-Process powershell -WindowStyle Hidden -ArgumentList $watchArguments
    if ($saved -gt 0) { Write-Host "  $saved screen setting(s) saved; they are put back when the guest exits." }
    if ($netSimStop) { Write-Host '  netsim stops and writes its stats once the last guest exits.' }
}

# Say where its logs are, which also proves BepInEx loaded.
Write-Host '  Waiting for the guest''s BepInEx log...'
$bepLog = $null
$deadline = $started.AddSeconds(90)
while ((Get-Date) -lt $deadline -and -not $guest.HasExited) {
    # Found by content, not name: LogOutput.1.log when the host holds LogOutput.log.
    $bepLog = Get-ChildItem $logDirs -Filter 'LogOutput*.log' -ErrorAction SilentlyContinue |
        Where-Object { $_.LastWriteTime -ge $started } |
        Where-Object { Select-String -Path $_.FullName -Pattern 'Role: loopback guest' -Quiet } |
        Select-Object -First 1
    if ($bepLog) { break }
    Start-Sleep -Seconds 2
}

if ($bepLog) {
    Write-Host "  BepInEx log: $($bepLog.FullName)" -ForegroundColor Green
    Select-String -Path $bepLog.FullName -Pattern 'loaded \(build|\[LoopbackGuest\]' | ForEach-Object { "    $($_.Line)" }
} elseif ($guest.HasExited) {
    Write-Host "  The guest exited (code $($guest.ExitCode)) before BepInEx wrote a log." -ForegroundColor Red
} else {
    Write-Host '  No guest log naming its role after 90 s. Is Debug.Enabled on in the guest''s .cfg?' -ForegroundColor Yellow
}
Write-Host "  Unity log: $unityLog"

# When it joins on its own, say how far it got.
if ($bepLog -and $join) {
    Write-Host '  Waiting for the guest to join (it may be sitting on the microphone check: click through it)...'
    $deadline = (Get-Date).AddSeconds(180)
    $shown = 0
    while ((Get-Date) -lt $deadline -and -not $guest.HasExited) {
        $lines = @(Select-String -Path $bepLog.FullName -Pattern '\[LoopbackGuestMonitor\]|\[LoopbackGuest\] OnClientConnect')
        foreach ($line in $lines | Select-Object -Skip $shown) { Write-Host "    $($line.Line)" }
        $shown = $lines.Count
        if ($lines | Where-Object { $_.Line -match 'Connection: in the world' }) { break }
        Start-Sleep -Seconds 2
    }
}
