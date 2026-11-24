using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Logs each `SavableSystem` change with the player's position, to find out what a flag is for.
    internal static class DebugSystemWrites
    {
        private static readonly Dictionary<SavableSystem, int> Last = new();

        internal static void Observe(SavableSystem system, int value)
        {
            try
            {
                if (system == SavableSystem.NotSavable)
                    return;

                if (Last.TryGetValue(system, out var before) && before == value)
                    return;

                Last[system] = value;

                var player = Core.LocalPlayer.Find();
                var where = player != null ? player.transform.position.ToString("F1") : "<no player>";
                Plugin.Log.LogInfo(
                    $"[SystemWrites] {Time.realtimeSinceStartup:F0}s {system} ({(int)system}) = {value} at {where}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[SystemWrites] {system}: {ex.Message}");
            }
        }
    }
}
