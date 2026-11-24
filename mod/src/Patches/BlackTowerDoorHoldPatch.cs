using BigWalkArchipelago.Core;
using HarmonyLib;

namespace BigWalkArchipelago.Patches
{
    // The monuments' combinator shuts the Black Tower door on each change; refuse that when held.
    [HarmonyPatch(typeof(TrackedPeckState), nameof(TrackedPeckState.SetState), new[] { typeof(PeckContext) })]
    internal static class BlackTowerDoorHoldPatch
    {
        private static bool Prefix(TrackedPeckState __instance, PeckContext __0)
        {
            if (__0 == null || __0.compressedState >= 1 || !BlackTowerDoor.IsTheDoor(__instance))
                return true;

            return !BlackTowerDoor.Held;
        }
    }
}
