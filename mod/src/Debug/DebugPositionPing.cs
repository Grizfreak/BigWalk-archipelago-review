using System;
using System.Collections.Generic;
using BigWalkArchipelago.Core;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Logs which puzzle the player stands at, to label them against DebugPuzzleMapDump's plot.
    internal static class DebugPositionPing
    {
        internal static void Log()
        {
            var player = Core.LocalPlayer.Find();
            if (player == null)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugPositionPing)}] No local player found.");
                return;
            }

            var position = player.transform.position;

            string nearestLabel = null;
            var nearestDistance = float.MaxValue;

            var gourds = UnityEngine.Object.FindObjectsByType<RewardGourd>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (gourds != null)
            {
                foreach (var gourd in gourds)
                {
                    var prop = gourd != null ? gourd.prop : null;
                    if (prop == null || !GourdRegistry.TryGetLocationId(prop.saveablePropName, out _))
                        continue;

                    var distance = Vector3.Distance(position, prop.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestLabel = $"puzzle {prop.saveablePropName}";
                    }
                }
            }

            foreach (var propName in GourdRegistry.BigKeys)
            {
                var home = GourdRegistry.TryGetHomeFor(propName);
                if (home == null)
                    continue;

                var distance = Vector3.Distance(position, home.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestLabel = $"plinth {propName}";
                }
            }

            Plugin.Log.LogInfo(
                $"[{nameof(DebugPositionPing)}] player x={position.x:F1} y={position.y:F1} z={position.z:F1} | "
                + (nearestLabel != null
                    ? $"nearest {nearestLabel} at {nearestDistance:F1}m"
                    : "nothing known nearby"));
        }
    }
}
