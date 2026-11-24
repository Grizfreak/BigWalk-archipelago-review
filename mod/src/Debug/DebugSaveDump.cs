using System;

namespace BigWalkArchipelago.Debug
{
    // Full SaveManager dump, to diff before and after an action whose save key is not visible.
    internal static class DebugSaveDump
    {
        internal static void DumpAll()
        {
            var data = SaveManager.instance != null ? SaveManager.instance.currentData : null;
            if (data == null || data.entries == null)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugSaveDump)}] No SaveData loaded.");
                return;
            }

            Plugin.Log.LogInfo($"[{nameof(DebugSaveDump)}]   skipAidsActive={data.skipAidsActive}");

            var entries = data.entries;
            Plugin.Log.LogInfo($"[{nameof(DebugSaveDump)}] --- full dump ({entries.Count} int entries) ---");

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                Plugin.Log.LogInfo($"[{nameof(DebugSaveDump)}]   {entry.key}={entry.value}");
            }

            Plugin.Log.LogInfo($"[{nameof(DebugSaveDump)}] --- end of dump ---");
        }
    }
}
