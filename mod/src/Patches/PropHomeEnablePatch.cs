using System.Collections.Generic;
using BigWalkArchipelago.Core;
using HarmonyLib;

namespace BigWalkArchipelago.Patches
{
    // Some PropHomes load late, so the fill tracker subscribes to each one as it is enabled.
    [HarmonyPatch(typeof(PropHome), nameof(PropHome.OnEnable))]
    internal static class PropHomeEnablePatch
    {
        // OnEnable runs again on reactivation.
        private static readonly HashSet<PropHome> RegisteredHomes = new HashSet<PropHome>();

        private static void Postfix(PropHome __instance)
        {
            if (__instance == null || !RegisteredHomes.Add(__instance))
                return;

            CosmeticMonumentFillTracker.RegisterHome(__instance);
        }
    }
}
