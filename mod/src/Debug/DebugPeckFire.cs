using System;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Pecks a PeckSwitch by name from afar, to test two-player buttons alone.
    internal static class DebugPeckFire
    {
        // Many PeckSwitch names are reused across the map; only fire the nearby ones.
        private const float MaxFireRadius = 40f;

        internal static void FireByName(string nameSubstring)
        {
            if (string.IsNullOrWhiteSpace(nameSubstring))
            {
                Plugin.Log.LogInfo($"[{nameof(DebugPeckFire)}] No name configured (ModConfig.RemotePeckSwitchName empty), nothing to trigger.");
                return;
            }

            var localPlayer = Core.LocalPlayer.Find();
            if (localPlayer == null)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugPeckFire)}] Local player not found.");
                return;
            }

            var origin = localPlayer.transform.position;
            var sqrRadius = MaxFireRadius * MaxFireRadius;

            var all = UnityEngine.Object.FindObjectsByType<PeckSwitch>(FindObjectsSortMode.None);
            if (all == null || all.Length == 0)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugPeckFire)}] No PeckSwitch found in the current area.");
                return;
            }

            var fired = 0;
            foreach (var sw in all)
            {
                if (sw == null)
                    continue;

                string goName;
                float sqrDistance;
                try
                {
                    goName = sw.gameObject.name;
                    sqrDistance = (sw.transform.position - origin).sqrMagnitude;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(DebugPeckFire)}] Unreadable PeckSwitch (name/position): {ex.Message}");
                    continue;
                }

                if (goName.IndexOf(nameSubstring, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                // Filtered last so the log can tell "unknown name" from "too far".
                if (sqrDistance > sqrRadius)
                {
                    Plugin.Log.LogInfo(
                        $"[{nameof(DebugPeckFire)}]   {goName} matches the name but is {Math.Sqrt(sqrDistance):F1}m away (> {MaxFireRadius}m), skipped for safety.");
                    continue;
                }

                try
                {
                    sw.Peck();
                    fired++;
                    Plugin.Log.LogInfo($"[{nameof(DebugPeckFire)}] Peck() triggered remotely on {goName}.");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(DebugPeckFire)}] {goName}, exception during Peck(), ignored: {ex.Message}");
                }
            }

            if (fired == 0)
                Plugin.Log.LogInfo($"[{nameof(DebugPeckFire)}] No PeckSwitch whose name contains '{nameSubstring}' found within {MaxFireRadius}m.");
        }
    }
}
