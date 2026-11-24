using BigWalkArchipelago.Core;

namespace BigWalkArchipelago.Debug
{
    // Unlocks a gourd or big key without its puzzle, to test the check patches.
    internal static class DebugGourdUnlocker
    {
        internal static void UnlockNext()
        {
            var closest = DebugGourdLookup.FindNearestLocked();
            if (closest == null)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugGourdUnlocker)}] No more locked gourd/big key in the current area.");
                return;
            }

            var prop = closest.prop;
            string label = prop != null ? prop.saveablePropName.ToString() : "<unknown prop>";
            Plugin.Log.LogInfo($"[{nameof(DebugGourdUnlocker)}] Forced unlock (nearest one): {label}");

            // Pinned before Loose: GourdStatePatch then unspawns it, and the pin is not saved.
            if (prop != null)
            {
                PropHome home = null;
                if (GourdRegistry.TryGetHomeName(prop.saveablePropName, out var homeName))
                {
                    home = PropHome.GetSaveableHome(homeName);
                    Plugin.Log.LogInfo(
                        $"[{nameof(DebugGourdUnlocker)}] Target home {homeName}: {(home != null ? "found" : "not found")}");
                }

                if (home == null)
                {
                    home = prop.startHome;
                    Plugin.Log.LogInfo(
                        $"[{nameof(DebugGourdUnlocker)}] Falling back to startHome: {(home != null ? "present" : "absent")}");
                }

                if (home != null)
                {
                    Plugin.Log.LogInfo(
                        $"[{nameof(DebugGourdUnlocker)}] Pinning to saveableHomeName={home.saveableHomeName} before Loose transition.");
                    prop.ServerSetPinned(home);
                }
            }

            closest.ServerSetGourdState(GourdFlag.GourdState.Loose);
        }
    }
}
