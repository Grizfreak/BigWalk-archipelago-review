using HarmonyLib;

namespace BigWalkArchipelago.Patches.Gourds
{
    // Loose (out of the vise) is the check; then the gourd is hidden and unspawned for all clients.
    [HarmonyPatch(typeof(RewardGourd), nameof(RewardGourd.ServerSetGourdState))]
    internal static class GourdStatePatch
    {
        private static void Postfix(RewardGourd __instance, GourdFlag.GourdState newGourdState)
        {
            if (newGourdState != GourdFlag.GourdState.Loose)
                return;

            var prop = __instance.prop;
            if (prop == null)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(GourdStatePatch)}] RewardGourd has no associated prop, Loose transition ignored.");
                return;
            }

            if (!GourdRegistry.TryGetLocationId(prop.saveablePropName, out var locationId))
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(GourdStatePatch)}] '{prop.saveablePropName}' not in registry (test/notSavable), ignored.");
                return;
            }

            if (CheckTracker.TryMarkReported(locationId))
                Plugin.Reporter.ReportCheck(locationId);

            __instance.ServerSetGourdState(GourdFlag.GourdState.Hidden);

            // A sealed-box gourd goes Loose inside the pick-up Command: removing it here breaks it.
            PuzzleGourdRetirer.Retire(__instance);
        }
    }
}
