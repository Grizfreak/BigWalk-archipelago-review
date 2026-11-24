using HarmonyLib;

namespace BigWalkArchipelago.Patches.Hub
{
    // A hint sign this player may not write on does not open for writing.
    [HarmonyPatch(typeof(PeckEffectTextInput), nameof(PeckEffectTextInput.Peck))]
    internal static class HintSignEditPatch
    {
        private static bool Prefix(PeckEffectTextInput __instance) => !HintBoards.IsLocked(__instance);
    }
}
