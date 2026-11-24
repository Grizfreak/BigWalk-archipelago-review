using BigWalkArchipelago.Core;
using HarmonyLib;
using Mirror;

namespace BigWalkArchipelago.Patches.Checks
{
    // Skips the station's own unlock, which writes nothing to the save: the check still goes out.
    [HarmonyPatch(typeof(BroadcastStation), nameof(BroadcastStation.Unlock))]
    internal static class BroadcastStationUnlockPatch
    {
        private static bool Prefix(BroadcastStation __instance, PeckContext __0)
        {
            // On a guest ItemsInPlay is only true once a modded host has said so.
            if (!ModConfig.ArchipelagoEnabled.Value || !RadioStations.ItemsInPlay)
                return true;

            // Below 1 is the switch going off or idling, and unlocks nothing.
            if (__0 == null || __0.compressedState < 1)
                return true;

            if (!RadioStations.TryGetSystem(__instance, out var system))
            {
                // A station the apworld has no item for: holding it back would lose its music.
                Plugin.Log.LogInfo(
                    $"[{nameof(BroadcastStationUnlockPatch)}] '{__instance.name}' has no station this world knows "
                    + "about; leaving its unlock alone.");
                return true;
            }

            if (RadioStations.IsGranted(system))
                return true;

            Plugin.Log.LogInfo(
                $"[{nameof(BroadcastStationUnlockPatch)}] {system} switched on; its music waits for the Archipelago "
                + "item.");
            return false;
        }
    }
}
