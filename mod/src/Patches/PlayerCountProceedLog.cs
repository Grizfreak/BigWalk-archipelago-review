using System;
using HarmonyLib;

namespace BigWalkArchipelago.Patches
{
    // Patched on its own in a try/catch: a failing patch in PatchAll would abort the whole mod.
    internal static class PlayerCountProceedLog
    {
        internal static void TryApply(Harmony harmony)
        {
            try
            {
                var target = AccessTools.Method(typeof(PlayerCountMenu), nameof(PlayerCountMenu.ActionProceed));
                var prefix = new HarmonyMethod(typeof(PlayerCountProceedLog), nameof(Prefix));
                harmony.Patch(target, prefix: prefix);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogInfo($"[{nameof(PlayerCountProceedLog)}] Not applied on this build: {ex.Message}");
            }
        }

        private static void Prefix()
        {
            Plugin.Log.LogInfo($"[{nameof(PlayerCountProceedLog)}] Play pressed on the player-count screen.");
        }
    }
}
