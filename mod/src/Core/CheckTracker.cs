using System;
using System.Collections.Generic;

namespace BigWalkArchipelago.Core
{
    // Saved dedup: the game re-asserts solved gourds on each zone load, by more than one path.
    internal static class CheckTracker
    {
        private const string KeyPrefix = "ap_reported_";

        // With Archipelago off, dedup stays in memory so a vanilla save is left untouched.
        private static readonly HashSet<string> ReportedThisSessionOnly = new();

        internal static bool IsReported(string locationId)
        {
            if (!ModConfig.ArchipelagoEnabled.Value)
                return ReportedThisSessionOnly.Contains(locationId);

            return SaveManager.GetIntValue(KeyPrefix + locationId, 0, false) != 0;
        }

        internal static bool TryMarkReported(string locationId)
        {
            if (!ModConfig.ArchipelagoEnabled.Value)
                return ReportedThisSessionOnly.Add(locationId);

            var key = KeyPrefix + locationId;
            if (SaveManager.GetIntValue(key, 0, false) != 0)
                return false;

            SaveManager.SetIntValue(key, 1);
            return true;
        }

        // Called when the save is bound to another seed or slot, never on its first binding.
        internal static int ClearReported()
        {
            var data = SaveManager.instance != null ? SaveManager.instance.currentData : null;
            if (data == null || data.entries == null)
                return 0;

            // Collected first: writing a key changes the list being walked.
            var keys = new List<string>();
            var entries = data.entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.value != 0 && entry.key != null && entry.key.StartsWith(KeyPrefix, StringComparison.Ordinal))
                    keys.Add(entry.key);
            }

            foreach (var key in keys)
                SaveManager.SetIntValue(key, 0);

            return keys.Count;
        }

        // Replayed on every connection, so checks made while offline still reach the server.
        internal static IEnumerable<string> GetReportedLocationNames()
        {
            var data = SaveManager.instance != null ? SaveManager.instance.currentData : null;
            if (data == null || data.entries == null)
                yield break;

            // Indexed, not foreach: an IL2CPP list of structs the game may add to meanwhile.
            var entries = data.entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.value == 0 || entry.key == null)
                    continue;

                if (entry.key.StartsWith(KeyPrefix, StringComparison.Ordinal))
                    yield return entry.key.Substring(KeyPrefix.Length);
            }
        }
    }
}
