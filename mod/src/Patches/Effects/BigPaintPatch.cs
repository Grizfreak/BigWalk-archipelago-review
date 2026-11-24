using HarmonyLib;

namespace BigWalkArchipelago.Patches.Effects
{
    // Big Paint trap: the colour is swapped in the same call, so nothing flickers.
    [HarmonyPatch(typeof(PlayerLooks), nameof(PlayerLooks.SetColor))]
    internal static class BigPaintPatch
    {
        private static void Prefix(PlayerLooks __instance, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<PlayerLooks.LooksRenderer> looksRenderers, ref PlayerLookSet.LookColor lookColor)
        {
            if (TrapEffects.PaintActive && TrapEffects.TryPaintColour(__instance, looksRenderers, out var colour))
                lookColor = colour;
        }
    }
}
