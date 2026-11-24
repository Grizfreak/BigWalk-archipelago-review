using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Starts a loopback guest on Ctrl+L; same launch as tools/launch-guest.ps1, keep them in step.
    internal static class LoopbackLauncher
    {
        private const string Tag = "[" + nameof(LoopbackLauncher) + "]";
        private const string AppId = "1478500";
        private const int Width = 1280;
        private const int Height = 720;
        private const int MaxGuests = 3;
        private const string PrefsKey = @"Software\House House\Big Walk";

        // withoutTheMod: the guest runs --bwap-vanilla, only the join.
        internal static void LaunchGuest(bool withoutTheMod)
        {
            using var self = Process.GetCurrentProcess();
            var others = Process.GetProcessesByName(self.ProcessName);
            var running = 0;
            foreach (var process in others)
            {
                if (process.Id != self.Id)
                    running++;
                process.Dispose();
            }

            // Up to three guests: four players, the most the game takes.
            if (running >= MaxGuests)
            {
                Plugin.Log.LogInfo($"{Tag} {running} guest(s) already running; {MaxGuests} is the most (four players).");
                return;
            }

            // A distinct id per guest, or the host would see two players as one.
            var index = running + 1;

            var exe = Environment.ProcessPath;
            var gameDir = Path.GetDirectoryName(exe);
            var hosting = NetworkServer.active;
            var unityLog = Path.Combine(Application.persistentDataPath, index > 1 ? $"Player-guest{index}.log" : "Player-guest.log");
            var arguments = (withoutTheMod ? LoopbackGuest.VanillaArgument : LoopbackGuest.GuestArgument)
                + $" {LoopbackGuest.IndexArgument} {index}"
                + (hosting ? " " + LoopbackGuest.JoinArgument : "")
                + $" -logFile \"{unityLog}\" -screen-fullscreen 0 -screen-width {Width} -screen-height {Height}";

            // A BepInEx-guest folder beside BepInEx gives the guest its own plugins.
            var guestLoader = Path.Combine(gameDir, "BepInEx-guest", "core", "BepInEx.Unity.IL2CPP.dll");
            if (!withoutTheMod && File.Exists(guestLoader))
                arguments += $" --doorstop-target-assembly \"{guestLoader}\"";

            // Saved before the first guest only: its watcher waits for every guest.
            var snapshot = index == 1 ? SnapshotScreenSettings() : null;

            Process guest;
            var previous = new Dictionary<string, string>();
            try
            {
                foreach (var name in new[] { "SteamAppId", "SteamGameId" })
                {
                    previous[name] = Environment.GetEnvironmentVariable(name);
                    Environment.SetEnvironmentVariable(name, AppId);
                }

                foreach (System.Collections.DictionaryEntry variable in Environment.GetEnvironmentVariables())
                {
                    var name = (string)variable.Key;
                    if (name.StartsWith("DOORSTOP_", StringComparison.OrdinalIgnoreCase))
                    {
                        previous[name] = (string)variable.Value;
                        Environment.SetEnvironmentVariable(name, null);
                    }
                }

                guest = Process.Start(new ProcessStartInfo(exe)
                {
                    WorkingDirectory = gameDir,
                    Arguments = arguments,
                    UseShellExecute = true,
                });
            }
            finally
            {
                foreach (var pair in previous)
                    Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }

            if (guest == null)
            {
                Plugin.Log.LogWarning($"{Tag} The guest did not start.");
                return;
            }

            Plugin.Log.LogInfo(
                $"{Tag} Guest {index}{(withoutTheMod ? " without the mod" : "")} started: process {guest.Id}, "
                + $"{Width}x{Height} windowed, log BepInEx\\LogOutput.{index}.log "
                + (hosting
                    ? "(joins on its own once its title menu is up)."
                    : "(not hosting yet: host a world, then press Ctrl+L in the guest's window)."));

            if (snapshot != null)
                StartRestoreWatcher(guest.Id, snapshot);
        }

        // Same JSON as tools/launch-guest.ps1, so the watcher restores it the same way.
        private static string SnapshotScreenSettings()
        {
            // Tells the compiler the registry calls are Windows only (CA1416).
            if (!OperatingSystem.IsWindows())
                return null;

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(PrefsKey);
                if (key == null)
                    return null;

                var entries = new List<Dictionary<string, object>>();
                foreach (var name in key.GetValueNames())
                {
                    if (!name.StartsWith("Screenmanager ", StringComparison.Ordinal))
                        continue;

                    var value = key.GetValue(name);
                    if (value is byte[] bytes)
                    {
                        var numbers = new int[bytes.Length];
                        for (var i = 0; i < bytes.Length; i++)
                            numbers[i] = bytes[i];
                        value = numbers;
                    }

                    entries.Add(new Dictionary<string, object>
                    {
                        ["Name"] = name,
                        ["Kind"] = key.GetValueKind(name).ToString(),
                        ["Value"] = value,
                    });
                }

                var file = Path.Combine(Path.GetTempPath(), "bwap-guest-screen-prefs.json");
                File.WriteAllText(file, JsonSerializer.Serialize(entries));
                return file;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Could not save the screen settings; the guest may leave them windowed: {ex.Message}");
                return null;
            }
        }

        private static void StartRestoreWatcher(int guestId, string snapshotFile)
        {
            var file = snapshotFile.Replace("'", "''");
            var script =
                $"$p = Get-Process -Id {guestId} -ErrorAction SilentlyContinue; if ($p) {{ $p.WaitForExit() }}\n"
                // The guests share these settings: put back once the last one is gone.
                + "do { Start-Sleep -Seconds 2; $g = @(Get-CimInstance Win32_Process -Filter \"Name='Big Walk.exe'\" -ErrorAction SilentlyContinue | "
                + "Where-Object { $_.CommandLine -like '*--bwap-guest*' -or $_.CommandLine -like '*--bwap-vanilla*' }) } while ($g.Count -gt 0)\n"
                + $"if (-not (Test-Path '{file}')) {{ exit 0 }}\n"
                + "$k = 'HKCU:\\Software\\House House\\Big Walk'\n"
                + $"$entries = Get-Content '{file}' -Raw | ConvertFrom-Json\n"
                + "$names = @()\n"
                + "foreach ($e in $entries) { $v = $e.Value; if ($e.Kind -eq 'Binary') { $v = [byte[]]$v }; "
                + "Set-ItemProperty -Path $k -Name $e.Name -Value $v -Type $e.Kind; $names += $e.Name }\n"
                + "foreach ($n in (Get-Item $k).Property | Where-Object { $_ -like 'Screenmanager *' -and $names -notcontains $_ }) "
                + "{ Remove-ItemProperty -Path $k -Name $n }\n"
                + $"Remove-Item '{file}'\n";

            try
            {
                Process.Start(new ProcessStartInfo("powershell.exe")
                {
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -EncodedCommand "
                        + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                })?.Dispose();
                Plugin.Log.LogInfo($"{Tag} Screen settings saved; they are put back when the guest exits.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Could not start the screen-settings watcher: {ex.Message}");
            }
        }
    }
}
