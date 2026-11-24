using BigWalkArchipelago.Core;

namespace BigWalkArchipelago.Debug
{
    // Pins the seven big keys at once through ItemApplier.
    internal static class DebugForceEndingFlags
    {
        private static readonly SaveablePropName[] BigKeys =
        {
            SaveablePropName.bigKeyIntro,
            SaveablePropName.bigKeyRedZone,
            SaveablePropName.bigKeyGreenZone,
            SaveablePropName.bigKeyBlueZone,
            SaveablePropName.bigKeyYellowZone,
            SaveablePropName.bigKeyBoss,
            SaveablePropName.bigKeyOverflow,
        };

        internal static void ForceAll()
        {
            Plugin.Log.LogInfo($"[{nameof(DebugForceEndingFlags)}] Pinning the 7 big keys...");

            foreach (var bigKey in BigKeys)
            {
                var applied = ItemApplier.ApplyBigKeyItem(bigKey);
                Plugin.Log.LogInfo($"[{nameof(DebugForceEndingFlags)}]   {bigKey} -> {(applied ? "OK" : "failed (see warning above)")}");
            }

            // Never write the goal flags here: GauntletComplete would latch a real goal.
            Plugin.Log.LogInfo($"[{nameof(DebugForceEndingFlags)}] Done. The goal flags are deliberately left alone, reach them in-game.");
        }
    }
}
