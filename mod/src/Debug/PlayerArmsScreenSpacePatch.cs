using HarmonyLib;

namespace BigWalkArchipelago.Debug
{
    // UpdateScreenSpaceArms throws every frame while the debug free camera is detached.
    [HarmonyPatch(typeof(PlayerArms), "UpdateScreenSpaceArms")]
    internal static class PlayerArmsScreenSpacePatch
    {
        private static bool Prefix() => !DebugFlightTool.IsActive;
    }
}
