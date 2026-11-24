using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // PeckDevHelper.Trigger is the game's own dev cheat: it fires every DevUnlock it matches.
    internal static class DebugPeckDevHelper
    {
        internal static void DumpNearby(float radius)
        {
            var localPlayer = Core.LocalPlayer.Find();
            if (localPlayer == null)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugPeckDevHelper)}] Local player not found.");
                return;
            }

            var origin = localPlayer.transform.position;
            var sqrRadius = radius * radius;

            var all = UnityEngine.Object.FindObjectsByType<PeckDevHelper>(FindObjectsSortMode.None);
            if (all == null || all.Length == 0)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugPeckDevHelper)}] No PeckDevHelper found in the current area.");
                return;
            }

            var entries = new List<(PeckDevHelper helper, float sqrDistance)>();
            foreach (var helper in all)
            {
                if (helper == null)
                    continue;

                var sqrDistance = (helper.transform.position - origin).sqrMagnitude;
                if (sqrDistance > sqrRadius)
                    continue;

                entries.Add((helper, sqrDistance));
            }

            entries.Sort((a, b) => a.sqrDistance.CompareTo(b.sqrDistance));

            Plugin.Log.LogInfo($"[{nameof(DebugPeckDevHelper)}] {entries.Count} PeckDevHelper within a radius of {radius}m:");
            foreach (var (helper, sqrDistance) in entries)
            {
                var distance = Math.Sqrt(sqrDistance).ToString("F1");
                var rules = helper.unlockRules;
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugPeckDevHelper)}]   {helper.gameObject.name}, distance={distance}m, " +
                    $"unlockRules(unlocks={rules.unlocks}, lights={rules.lights}, chairlift={rules.chairlift}, " +
                    $"train={rules.train}, bell={rules.bell}, tunnel={rules.tunnel}, map={rules.map}, gourd={rules.gourd}), " +
                    $"fireWithUnlocks={helper.fireWithUnlocks}, fireWithLights={helper.fireWithLights}, fireWithTrain={helper.fireWithTrain}");
            }
        }

        internal static void Trigger(PeckDevHelper.UnlockRules rules)
        {
            Plugin.Log.LogInfo(
                $"[{nameof(DebugPeckDevHelper)}] Trigger: unlocks={rules.unlocks}, lights={rules.lights}, " +
                $"chairlift={rules.chairlift}, train={rules.train}, bell={rules.bell}, tunnel={rules.tunnel}, " +
                $"map={rules.map}, gourd={rules.gourd}");
            PeckDevHelper.Trigger(rules);
        }
    }
}
