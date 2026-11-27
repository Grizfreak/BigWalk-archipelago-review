using HarmonyLib;

namespace BigWalkArchipelago.Debug
{
    // UpdateScreenSpaceArms throws every frame while the debug free camera is detached.
    internal static class PlayerArmsScreenSpacePatch
    {
        // Applied by Plugin.Load only with Debug.Enabled, never by PatchAll.
        internal static void TryApply(Harmony harmony)
        {
            try
            {
                harmony.Patch(AccessTools.Method(typeof(PlayerArms), "UpdateScreenSpaceArms"),
                    prefix: new HarmonyMethod(typeof(PlayerArmsScreenSpacePatch), nameof(Prefix)));
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(PlayerArmsScreenSpacePatch)}] Not applied: {ex.Message}");
            }
        }

        private static bool Prefix() => !DebugFlightTool.IsActive;
    }
}
