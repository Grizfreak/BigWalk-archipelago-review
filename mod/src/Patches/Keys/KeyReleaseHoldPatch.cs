using BigWalkArchipelago.Core;
using HarmonyLib;

namespace BigWalkArchipelago.Patches.Keys
{
    // Keeps a big key in its stone until its item arrives; every release goes through SetState.
    [HarmonyPatch(typeof(TrackedPeckState), nameof(TrackedPeckState.SetState), new[] { typeof(PeckContext) })]
    internal static class KeyReleaseHoldPatch
    {
        private static bool Prefix(TrackedPeckState __instance, PeckContext __0)
        {
            if (!KeyCustody.KeysAreItems || __0 == null || __0.compressedState < 1)
                return true;

            if (!KeyCustody.IsReleaseHeld(__instance, out var propName))
                return true;

            Plugin.Log.LogInfo(
                $"[{nameof(KeyReleaseHoldPatch)}] {propName} stays in its stone: its item has not arrived.");
            return false;
        }
    }
}
