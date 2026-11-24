using HarmonyLib;
using Mirror;

namespace BigWalkArchipelago.Patches.Keys
{
    // One check per key segment cut. Hooks OnCutsUpdated: ServerCutSegment is inlined.
    [HarmonyPatch(typeof(KeyBlank), nameof(KeyBlank.OnCutsUpdated))]
    internal static class KeyBlankCutPatch
    {
        // Arguments by index: a name mismatch throws at PatchAll and takes the plugin down.
        private static void Postfix(KeyBlank __instance, int __1, bool __2, bool __3)
        {
            if (!NetworkServer.active)
                return;

            // World load appends entries one at a time; only a false -> true transition is a cut.
            if (!__3 || __2)
                return;

            var prop = __instance != null ? __instance.prop : null;
            if (prop == null)
                return;

            var propName = prop.saveablePropName;
            if (!GourdRegistry.IsBigKey(propName))
                return;

            var locationId = ApLocationIds.CutLocationName(propName, __1);
            if (!CheckTracker.TryMarkReported(locationId))
                return;

            Plugin.Log.LogInfo($"[{nameof(KeyBlankCutPatch)}] Segment {__1} cut out of {propName}.");
            Plugin.Reporter.ReportCheck(locationId);
        }
    }
}
