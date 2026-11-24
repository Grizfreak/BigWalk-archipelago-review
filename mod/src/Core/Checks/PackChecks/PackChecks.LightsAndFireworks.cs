using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Checks
{
    internal static partial class PackChecks
    {
        internal static void OnState(TrackedPeckState state, int value)
        {
            if (value == 0 || state == null || !NetworkServer.active)
                return;

            OnLight(state);

            var key = LauncherKey(state.transform);
            if (key == null)
                return;

            if (ModConfig.ArchipelagoEnabled.Value && SaveManager.GetIntValue(FiredPrefix + key, 0, false) == 0)
                SaveManager.SetIntValue(FiredPrefix + key, 1);

            if (_fireworkSanity && Array.IndexOf(_fireworkKeys, key) >= 0)
                Report(ApLocationIds.FireworkPrefix + key, $"firework launcher in {key} fired");
        }

        private static void OnLight(TrackedPeckState state)
        {
            var slot = ApRuntime.SlotData;
            if (slot == null || slot.LightSanity == "off" && slot.LightsCounted.Length == 0)
                return;

            // Loading a world resets every light to its default, which is not a player action.
            if (_worldReadySince < 0f || Time.unscaledTime - _worldReadySince < LightSettleSeconds)
                return;

            var prop = state.GetComponent<Prop>();
            string key = null;
            if (prop != null)
            {
                if (ReceivedItemSpawner.IsCosmeticClone(prop) || prop.gameObject.name.Contains("(AP", StringComparison.Ordinal))
                    return;
                key = prop.savablePropGuid;
            }
            else
            {
                key = state.transform.name;
                for (var t = state.transform.parent; t != null; t = t.parent)
                    key = t.name + "/" + key;
            }

            if (string.IsNullOrEmpty(key))
                return;

            if (Array.IndexOf(slot.LightsCounted, key) >= 0 && SaveManager.GetIntValue(LitPrefix + key, 0, false) == 0)
            {
                SaveManager.SetIntValue(LitPrefix + key, 1);
                Plugin.Log.LogInfo($"{Tag} light counted: {key}");
            }

            if (slot.LightSanity == "off" || Array.IndexOf(slot.LightKeys, key) < 0)
                return;
            if (slot.LightSanity == "lamps" && prop == null)
                return;

            Report(ApLocationIds.LightPrefix + key, $"{(prop != null ? prop.gameObject.name : "light switch")} switched on");
        }

        private const float LightSettleSeconds = 10f;
        private static float _worldReadySince = -1f;

        private const string LitPrefix = "ap_lit_";

        internal static int LightsOn()
        {
            var counted = ApRuntime.SlotData?.LightsCounted;
            if (counted == null || counted.Length == 0)
                return 0;
            var on = 0;
            foreach (var key in counted)
            {
                if (SaveManager.GetIntValue(LitPrefix + key, 0, false) != 0
                    || CheckTracker.IsReported(ApLocationIds.LightPrefix + key))
                    on++;
            }
            return on;
        }

        internal static void ClearLightLedger()
        {
            var data = SaveManager.instance != null ? SaveManager.instance.currentData : null;
            if (data?.entries == null)
                return;
            var lit = new List<string>();
            foreach (var entry in data.entries)
            {
                if (entry.value != 0 && entry.key != null && entry.key.StartsWith(LitPrefix, StringComparison.Ordinal))
                    lit.Add(entry.key);
            }
            foreach (var key in lit)
                SaveManager.SetIntValue(key, 0);
        }

        private const string FiredPrefix = "ap_firework_fired_";

        // Known to the mod too, so a launcher is recognised without a connection.
        private static readonly string[] Landmarks =
        {
            "Hub", "Lookout_RedCone", "Lookout_YellowZigzag", "Lookout_BlueTube", "Lookout_GreenHourglass",
            "PeakMarker", "SouthPeak", "FireworksPlatform_Intro",
        };

        internal static string LandmarkOf(Transform launcher)
        {
            for (var t = launcher != null ? launcher.parent : null; t != null; t = t.parent)
            {
                if (Array.IndexOf(Landmarks, t.name) >= 0)
                    return t.name;
            }

            return null;
        }

        internal static bool LauncherFired(string key)
        {
            return SaveManager.GetIntValue(FiredPrefix + key, 0, false) != 0
                   || CheckTracker.IsReported(ApLocationIds.FireworkPrefix + key);
        }

        internal static string LauncherKey(Transform transform)
        {
            var inLauncher = false;
            for (var t = transform; t != null; t = t.parent)
            {
                if (!inLauncher)
                {
                    inLauncher = t.name.StartsWith(LauncherName, StringComparison.Ordinal);
                    continue;
                }

                if (Array.IndexOf(Landmarks, t.name) >= 0)
                    return t.name;
            }

            return null;
        }
    }
}
