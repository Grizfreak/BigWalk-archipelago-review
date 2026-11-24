using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Lookup shared between the debug tools (DebugGourdUnlocker, DebugItemSimulator).
    internal static class DebugGourdLookup
    {
        // Scans Prop.allProps, not RewardGourd: big keys have no RewardGourd component.
        internal static void LogNearby(int count)
        {
            var all = Prop.allProps;
            if (all == null || all.Count == 0)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugGourdLookup)}] No Prop found in the current area.");
                return;
            }

            var localPlayer = Core.LocalPlayer.Find();
            Vector3? playerPosition = localPlayer != null ? localPlayer.transform.position : (Vector3?)null;

            // Prop.allProps is an Il2Cpp List with no IEnumerable<T>, so it is copied before LINQ.
            var entries = new List<(Prop prop, RewardGourd gourdComponent, bool active, bool saved, float sqrDistance)>();
            foreach (var prop in all)
            {
                if (prop == null || prop.saveablePropName == SaveablePropName.notSavable)
                    continue;

                var sqrDistance = playerPosition.HasValue
                    ? (prop.transform.position - playerPosition.Value).sqrMagnitude
                    : 0f;
                entries.Add((
                    prop,
                    prop.GetComponent<RewardGourd>(),
                    prop.gameObject.activeInHierarchy,
                    SaveManager.GetIntValue(prop.saveablePropName.ToString(), 0, false) != 0,
                    sqrDistance));
            }

            entries.Sort((a, b) => a.sqrDistance.CompareTo(b.sqrDistance));

            var playerLabel = playerPosition.HasValue ? playerPosition.Value.ToString() : "<local player not found>";
            Plugin.Log.LogInfo($"[{nameof(DebugGourdLookup)}] Player position: {playerLabel}");
            Plugin.Log.LogInfo($"[{nameof(DebugGourdLookup)}] {entries.Count} savable Prop in the area, the {Math.Min(count, entries.Count)} nearest:");
            for (var i = 0; i < entries.Count && i < count; i++)
            {
                var entry = entries[i];
                var distance = playerPosition.HasValue ? Math.Sqrt(entry.sqrDistance).ToString("F1") : "?";
                var gourdState = entry.gourdComponent != null ? entry.gourdComponent.gourdState.ToString() : "n/a (no RewardGourd)";
                Plugin.Log.LogInfo($"[{nameof(DebugGourdLookup)}]   {entry.prop.saveablePropName}, gourdState={gourdState}, already saved={entry.saved}, active={entry.active}, distance={distance}m");
            }
        }

        // Every loaded RewardGourd with isVariantChallenge, sorted by distance from the player.
        internal static void LogAllLoaded()
        {
            var all = UnityEngine.Object.FindObjectsByType<RewardGourd>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            var localPlayer = Core.LocalPlayer.Find();
            Vector3? playerPosition = localPlayer != null ? localPlayer.transform.position : (Vector3?)null;

            // Every gourd is loaded everywhere, so only distance says where something is.
            var entries = new List<(string name, bool variant, bool known, string state, float distance)>();
            var loaded = new HashSet<string>(StringComparer.Ordinal);
            var variants = 0;

            foreach (var gourd in all)
            {
                if (gourd == null)
                    continue;

                var prop = gourd.prop;
                var propName = prop != null ? prop.saveablePropName.ToString() : "<no prop>";
                var known = prop != null && GourdRegistry.TryGetLocationId(prop.saveablePropName, out _);

                if (gourd.isVariantChallenge)
                    variants++;

                var distance = playerPosition.HasValue && prop != null
                    ? Vector3.Distance(prop.transform.position, playerPosition.Value)
                    : -1f;

                entries.Add((propName, gourd.isVariantChallenge, known, gourd.gourdState.ToString(), distance));
                loaded.Add(propName);
            }

            entries.Sort((a, b) => a.distance.CompareTo(b.distance));

            foreach (var entry in entries)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugGourdLookup)}]   {entry.distance.ToString("0.0").PadLeft(7)}m  {entry.name} "
                    + $"| variantChallenge={entry.variant} | isALocation={entry.known} | state={entry.state}");
            }

            // "none here" and "the key did not register" must never look alike.
            Plugin.Log.LogInfo(
                $"[{nameof(DebugGourdLookup)}] {all.Length} RewardGourd(s) in the world, {variants} of them purple"
                + " (variantChallenge), nearest first"
                + (playerPosition.HasValue ? "." : ", NO LOCAL PLAYER FOUND, so every distance is meaningless."));

            LogPuzzlesNotLoaded(loaded);
        }

        // Puzzles with no RewardGourd loaded here: this press cannot speak for their zone.
        private static void LogPuzzlesNotLoaded(HashSet<string> loaded)
        {
            var missing = new List<string>();

            foreach (SaveablePropName propName in Enum.GetValues(typeof(SaveablePropName)))
            {
                var name = propName.ToString();
                if (!name.StartsWith("gourd", StringComparison.Ordinal))
                    continue;

                // Dev-only values and the gourd the world leaves out are not puzzles.
                if (!GourdRegistry.TryGetLocationId(propName, out _))
                    continue;

                if (!loaded.Contains(name))
                    missing.Add(name);
            }

            if (missing.Count == 0)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugGourdLookup)}] Every puzzle gourd is loaded here, so the list above is the whole set.");
                return;
            }

            Plugin.Log.LogInfo(
                $"[{nameof(DebugGourdLookup)}] {missing.Count} puzzle gourd(s) NOT loaded here, never read, status unknown:");
            foreach (var name in missing)
                Plugin.Log.LogInfo($"[{nameof(DebugGourdLookup)}]   (absent) {name}");
        }

        internal static void LogMonumentHomes()
        {
            var all = PropHome.allPropHomes;
            if (all == null || all.Count == 0)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugGourdLookup)}] No PropHome found in the current area.");
                return;
            }

            var matches = new List<PropHome>();
            foreach (var home in all)
            {
                if (home != null && home.saveableHomeName.ToString().Contains("monoument", StringComparison.OrdinalIgnoreCase))
                    matches.Add(home);
            }

            Plugin.Log.LogInfo($"[{nameof(DebugGourdLookup)}] {all.Count} PropHome total in the area, {matches.Count} containing 'monoument':");
            foreach (var home in matches)
            {
                var pinnedLabel = home.pinnedProp != null ? home.pinnedProp.saveablePropName.ToString() : "<empty>";
                Plugin.Log.LogInfo($"[{nameof(DebugGourdLookup)}]   {home.saveableHomeName}, pinnedProp={pinnedLabel}");
            }
        }

        // Gourds and big keys not yet written to SaveManager; big keys have no gourdState.
        internal static Prop FindNearestUncollectedProp()
        {
            var all = Prop.allProps;
            if (all == null || all.Count == 0)
                return null;

            var localPlayer = Core.LocalPlayer.Find();
            Vector3? playerPosition = localPlayer != null ? localPlayer.transform.position : (Vector3?)null;

            Prop closest = null;
            float closestSqrDistance = float.MaxValue;

            foreach (var prop in all)
            {
                if (prop == null || prop.saveablePropName == SaveablePropName.notSavable)
                    continue;

                if (SaveManager.GetIntValue(prop.saveablePropName.ToString(), 0, false) != 0)
                    continue;

                if (playerPosition == null)
                    return prop;

                float sqrDistance = (prop.transform.position - playerPosition.Value).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closest = prop;
                }
            }

            return closest;
        }

        internal static RewardGourd FindNearestLocked()
        {
            var all = UnityEngine.Object.FindObjectsByType<RewardGourd>(FindObjectsSortMode.None);
            if (all == null || all.Length == 0)
                return null;

            // The closest to the local player, so the effect is visible at once.
            var localPlayer = Core.LocalPlayer.Find();
            Vector3? playerPosition = localPlayer != null ? localPlayer.transform.position : (Vector3?)null;

            RewardGourd closest = null;
            float closestSqrDistance = float.MaxValue;

            foreach (var gourd in all)
            {
                if (gourd == null || gourd.gourdState != GourdFlag.GourdState.Locked)
                    continue;

                if (playerPosition == null)
                    return gourd;

                float sqrDistance = (gourd.transform.position - playerPosition.Value).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closest = gourd;
                }
            }

            return closest;
        }
    }
}
