using HarmonyLib;

namespace BigWalkArchipelago.Patches.Doors
{
    // Stops a placed big key from opening its door when the key features are Archipelago items.
    [HarmonyPatch(typeof(Prop), nameof(Prop.SetPinDirectControlSystem))]
    internal static class PropPinDoorPatch
    {
        // What a big key opens, as the players read it (the apworld's feature names).
        private static string FeatureName(SaveablePropName key) => key.ToString() switch
        {
            "bigKeyIntro" => "Drawbridge",
            "bigKeyRedZone" => "Map Room",
            "bigKeyGreenZone" => "Chairlift",
            "bigKeyBlueZone" => "Train",
            "bigKeyYellowZone" => "Tunnels",
            "bigKeyBoss" => "Big Wall Door",
            "bigKeyOverflow" => "Spawn Secret Door",
            _ => null,
        };

        // By index: interop parameter names are not guaranteed, and a missing one breaks PatchAll.
        private static void Prefix(Prop __instance, PropHome __0, bool __1,
            out TrackedPeckState __state)
        {
            __state = null;

            if (!ShouldSuppress(__instance, propHome: __0, pinned: __1))
                return;

            __state = __0.pinDirectControlSystem;
            __0.pinDirectControlSystem = null;

            // A held-back door looks like a bug, so the log says so.
            Plugin.Log.LogInfo(
                $"[{nameof(PropPinDoorPatch)}] {__instance.saveablePropName} placed in "
                + $"{__0.saveableHomeName}: its door was held back"
                + (KeyFeatures.IsGranted(__instance.saveablePropName)
                    ? " (already granted, so it is open anyway)."
                    : ", it opens when the matching item arrives."));

            // And on screen, for the same reason.
            var feature = FeatureName(__instance.saveablePropName);
            if (feature != null && !KeyFeatures.IsGranted(__instance.saveablePropName))
                Core.Net.ApNotices.Post($"{feature}: opens when its item arrives");
        }

        private static void Postfix(PropHome __0, TrackedPeckState __state)
        {
            // Always put back: a plinth left blanked would never open, even once its item arrives.
            if (__state != null)
                __0.pinDirectControlSystem = __state;
        }

        private static bool ShouldSuppress(Prop prop, PropHome propHome, bool pinned)
        {
            // Without key features as items, the game is left alone.
            if (!KeyFeatures.FeaturesInPlay)
                return false;

            if (!pinned)
                return false;

            if (prop == null || propHome == null)
                return false;

            // Plinth only: Prop.Start pins each key into its tower's base home on every load.
            return GourdRegistry.IsBigKey(prop.saveablePropName)
                   && GourdRegistry.IsBigKeyPlinth(propHome.saveableHomeName);
        }
    }
}
