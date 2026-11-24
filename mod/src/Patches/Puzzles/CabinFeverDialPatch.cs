using BigWalkArchipelago.Core;
using HarmonyLib;

namespace BigWalkArchipelago.Patches.Puzzles
{
    // Draws the Cabin Fever dial animation after the game has written its own text.
    [HarmonyPatch(typeof(TimerTextController), nameof(TimerTextController.Update))]
    internal static class CabinFeverDialPatch
    {
        private static void Postfix(TimerTextController __instance)
        {
            CabinFeverDial.Apply(__instance);
        }
    }
}
