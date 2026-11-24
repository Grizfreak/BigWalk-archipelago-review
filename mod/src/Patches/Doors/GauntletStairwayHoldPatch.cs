using BigWalkArchipelago.Core;
using HarmonyLib;

namespace BigWalkArchipelago.Patches.Doors
{
    // Keeps a Gauntlet stairway shut while `locked_stages` holds it; only a raise is refused.
    [HarmonyPatch(typeof(TrackedPeckState), nameof(TrackedPeckState.SetState), new[] { typeof(PeckContext) })]
    internal static class GauntletStairwayHoldPatch
    {
        private static bool Prefix(TrackedPeckState __instance, PeckContext __0)
        {
            if ((!GauntletStairways.Enabled && !ObjectiveBoard.HoldsTheLastStairway) || __0 == null || __0.compressedState < 1)
                return true;

            if (!GauntletStairways.IsHeldShut(__instance, out var level))
                return true;

            Plugin.Log.LogInfo(
                $"[{nameof(GauntletStairwayHoldPatch)}] Stage {level + 1}'s {__instance.transform.parent?.name} stays shut: "
                + (GauntletStairways.Enabled && !GauntletStairways.IsGranted(level) ? "its item has not arrived." : "the objectives are not done."));
            if (!GauntletStairways.Enabled || GauntletStairways.IsGranted(level))
                ObjectiveBoard.SayEndingIsShut("big_goodbye");
            return false;
        }
    }
}
