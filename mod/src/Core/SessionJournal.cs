using System;
using System.Globalization;
using System.IO;
using BepInEx;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Appended event log that outlives LogOutput.log; two instances can write it. Never throws.
    internal static class SessionJournal
    {
        private const string FileName = "session-journal.tsv";
        private const string OldFileName = "session-journal.old.tsv";
        private const long MaxBytes = 4 * 1024 * 1024;
        private const string Header = "launch	time	uptime_s	role	event	detail	x	y	z	nearest_puzzle	distance";

        private static string _path;
        private static string _launchId;
        private static bool _opened;

        internal static void Write(string kind, string detail, bool withPosition = true)
        {
            try
            {
                if (!_opened)
                    Open();

                var role = NetworkServer.active ? "host" : NetworkClient.active ? "guest" : "solo";
                var where = withPosition ? Position() : "				";
                AppendLine(string.Join("	", new[]
                {
                    _launchId,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                    Time.realtimeSinceStartup.ToString("F1", CultureInfo.InvariantCulture),
                    role,
                    kind,
                    detail ?? string.Empty,
                    where,
                }));
            }
            catch (Exception)
            {
            }
        }

        private static void Open()
        {
            _opened = true;
            _path = Path.Combine(Paths.BepInExRootPath, FileName);

            // Process start time: one id per launch, different for the guest.
            var started = DateTime.Now.AddSeconds(-Time.realtimeSinceStartup);
            _launchId = started.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

            try
            {
                if (File.Exists(_path) && new FileInfo(_path).Length > MaxBytes)
                    File.Move(_path, Path.Combine(Paths.BepInExRootPath, OldFileName), overwrite: true);
            }
            catch (Exception)
            {
                // The other instance has the file: keep appending, rotate at the next launch.
            }

            if (!File.Exists(_path))
                AppendLine(Header);

            AppendLine(string.Join("	", new[]
            {
                _launchId, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture), "0",
                "-", "launch", $"{Plugin.PluginName} v{Plugin.PluginVersion}", "", "", "", "", "",
            }));
        }

        private static void AppendLine(string line)
        {
            using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream);
            writer.WriteLine(line);
        }

        // Empty columns when there is no player yet (menu, loading screen).
        private static string Position()
        {
            const string none = "\t\t\t\t";
            try
            {
                var player = LocalPlayer.Find();
                if (player == null)
                    return none;

                var at = player.transform.position;
                var nearest = "<none>";
                var best = float.MaxValue;
                var gourds = UnityEngine.Object.FindObjectsByType<RewardGourd>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (gourds != null)
                {
                    foreach (var gourd in gourds)
                    {
                        var prop = gourd != null ? gourd.prop : null;
                        if (prop == null || !GourdRegistry.TryGetLocationId(prop.saveablePropName, out _))
                            continue;

                        var distance = Vector3.Distance(at, prop.transform.position);
                        if (distance < best)
                        {
                            best = distance;
                            nearest = prop.saveablePropName.ToString();
                        }
                    }
                }

                return string.Join("\t", new[]
                {
                    at.x.ToString("F1", CultureInfo.InvariantCulture),
                    at.y.ToString("F1", CultureInfo.InvariantCulture),
                    at.z.ToString("F1", CultureInfo.InvariantCulture),
                    nearest,
                    best == float.MaxValue ? string.Empty : best.ToString("F1", CultureInfo.InvariantCulture),
                });
            }
            catch (Exception)
            {
                return none;
            }
        }
    }
}
