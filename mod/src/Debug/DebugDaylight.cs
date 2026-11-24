using System;

namespace BigWalkArchipelago.Debug
{
    // Toggles a fixed noon (SetFixedTime also stops the clock). Local only, nothing is saved.
    internal static class DebugDaylight
    {
        private const float Noon = 12f;

        private static bool _frozen;

        internal static void Toggle()
        {
            try
            {
                if (_frozen)
                {
                    SkyManager.ClearFixedTime();
                    _frozen = false;
                    Plugin.Log.LogInfo($"[{nameof(DebugDaylight)}] Clock released.");
                }
                else
                {
                    SkyManager.SetFixedTime(Noon);
                    _frozen = true;
                    Plugin.Log.LogInfo($"[{nameof(DebugDaylight)}] Clock fixed at {Noon:F0}:00.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(DebugDaylight)}] Failed: {ex.Message}");
            }
        }
    }
}
