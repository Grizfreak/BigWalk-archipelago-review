using BigWalkArchipelago.Core;
using HarmonyLib;

namespace BigWalkArchipelago.Patches.Hub
{
    // Tells WorldButtons when one of its buttons is pressed; a flag keeps other presses cheap.
    [HarmonyPatch(typeof(TrackedPeckState), nameof(TrackedPeckState.SetState), new[] { typeof(PeckContext) })]
    internal static class WorldButtonPatch
    {
        private static void Postfix(TrackedPeckState __instance, PeckContext __0)
        {
#if DEBUG
            if (Debug.DebugStateLog.On)
                Debug.DebugStateLog.ObserveState(__instance, __0.state);
#endif

            PackChecks.OnState(__instance, __0.state);
            PuzzleFailures.OnState(__instance, __0.state);

            if (!WorldButtons.HasNative || __instance == null)
                return;

            WorldButtons.OnState(__instance, __0);
        }
    }
}
