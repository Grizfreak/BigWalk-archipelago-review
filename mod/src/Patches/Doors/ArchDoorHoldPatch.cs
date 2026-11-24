using HarmonyLib;

namespace BigWalkArchipelago.Patches.Doors
{
    // Holds a locked arch door shut at its state, which every way of opening it goes through.
    [HarmonyPatch(typeof(TrackedPeckState), nameof(TrackedPeckState.SetState), new[] { typeof(PeckContext) })]
    internal static class ArchDoorHoldPatch
    {
        private static bool Prefix(TrackedPeckState __instance, PeckContext __0)
        {
            if (__instance == null || __0 == null || __0.compressedState < 1)
                return true;

            var system = __instance.savableSystem;
            if (!ArchDoors.IsArchDoor(system) || !ArchDoors.IsHeldClosed(system))
                return true;

            Plugin.Log.LogInfo($"[{nameof(ArchDoorHoldPatch)}] {system} stays closed: its item has not arrived.");
            return false;
        }
    }
}
