using Mirror;

namespace BigWalkArchipelago.Core.Items
{
    // Applies received items through SaveManager; Prop.Start() pins the live prop on the next load.
    internal static class ItemApplier
    {
        // Gourds are monument currency only: never written under a gourd's name, never pinned.
        internal static bool ApplyGourdItem(bool toPlayer)
        {
            using var activity = ModActivity.Enter("apply gourd item");
            if (!NetworkServer.active)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ItemApplier)}] Ignored: gourd received while not host.");
                return false;
            }

            // Null when no InventorySpawn is loaded yet (just after a save loads): caller retries.
            if (ReceivedItemSpawner.SpawnCosmeticPickup(toPlayer) == null)
                return false;

            Plugin.Log.LogInfo($"[{nameof(ItemApplier)}] Generic gourd applied (cosmetic spawn only).");
            return true;
        }

        // Filler gadgets: a cosmetic spawn and nothing else, like a gourd.
        internal static bool ApplyGadgetItem(GadgetKind kind, bool toPlayer)
        {
            using var activity = ModActivity.Enter("apply gadget item");
            if (!NetworkServer.active)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ItemApplier)}] Ignored: {kind} received while not host.");
                return false;
            }

            if (GadgetItemSpawner.SpawnCosmeticPickup(kind, toPlayer) == null)
                return false;

            Plugin.Log.LogInfo($"[{nameof(ItemApplier)}] Gadget item applied ({kind}, cosmetic spawn only).");
            return true;
        }

        // The 1:1 pin model stays for seeds whose apworld does not send `big_key_features`.
        internal static bool ApplyBigKeyItem(SaveablePropName propName)
        {
            if (KeyFeatures.FeaturesInPlay)
                return KeyFeatures.Grant(propName);

            return ApplyBigKeyPin(propName);
        }

        private static bool ApplyBigKeyPin(SaveablePropName propName)
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ItemApplier)}] Ignored: {propName} received while not host.");
                return false;
            }

            if (!GourdRegistry.TryGetHomeName(propName, out var homeName))
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ItemApplier)}] No known home for {propName}, item ignored.");
                return false;
            }

            // Marked first: Prop.Start() rewrites this key later, which must not look like a check.
            if (GourdRegistry.TryGetLocationId(propName, out var locationId)
                && CheckTracker.TryMarkReported(locationId))
                Plugin.Reporter.ReportCheck(locationId);

            SaveManager.SetIntValue(propName.ToString(), (int)homeName);

            TryApplyLiveEffect(propName, homeName);

            // No cosmetic spawn here: a gourd per key would add to the monument currency.

            Plugin.Log.LogInfo($"[{nameof(ItemApplier)}] Item applied: {propName} -> {homeName}.");
            return true;
        }

        // Only for props without a RewardGourd; otherwise Prop.Start() pins it on the next load.
        private static void TryApplyLiveEffect(SaveablePropName propName, SaveableHomeName homeName)
        {
            Prop targetProp = null;
            foreach (var prop in Prop.allProps)
            {
                if (prop != null && prop.saveablePropName == propName)
                {
                    targetProp = prop;
                    break;
                }
            }

            if (targetProp == null)
                return;

            if (targetProp.GetComponent<RewardGourd>() != null)
                return;

            var propHome = GourdRegistry.TryGetHome(homeName);
            if (propHome == null)
                return;

            targetProp.ServerSetPinned(propHome);
            Plugin.Log.LogInfo(
                $"[{nameof(ItemApplier)}] Immediate effect applied (no RewardGourd): {propName} pinned live to {homeName}.");
        }
    }
}
