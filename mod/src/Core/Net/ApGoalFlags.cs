namespace BigWalkArchipelago.Core.Net
{
    // Goal flags latch on a change while playable: EndingGate rests at 1 and is written at load.
    internal static partial class ApGoalFlags
    {
        private const string KeyPrefix = "ap_flag_";

        // Cleared on unload, not on load: the baseline writes arrive before the world is ready.
        private static readonly Dictionary<SavableSystem, int> LoadBaseline = new();

        internal static void OnWorldUnloaded()
        {
            LoadBaseline.Clear();
        }

        private static bool IsGoalFlag(SavableSystem system)
        {
            return system == SavableSystem.EndingGate || system == SavableSystem.GauntletComplete;
        }

        // Called for every write to these keys, zero included.
        internal static void Observe(SavableSystem system, int value)
        {
            if (!IsGoalFlag(system) || IsLatched(system))
                return;

            if (!WorldManager.isReadyForEffects)
            {
                LoadBaseline[system] = value;
                Plugin.Log.LogInfo(
                    $"[{nameof(ApGoalFlags)}] {system}={value} written while the world was still loading: baseline, not the goal.");
                return;
            }

            if (LoadBaseline.TryGetValue(system, out var baseline) && baseline == value)
                return;

            // A zero is never the goal: a refused bell ring writes EndingGate = 0.
            if (value < 1)
            {
                LoadBaseline[system] = value;
                return;
            }

            var previous = LoadBaseline.TryGetValue(system, out var known) ? known.ToString() : "unknown";
            LoadBaseline[system] = value;

            var key = KeyPrefix + system;
            SaveManager.SetIntValue(key, 1);
            Plugin.Log.LogInfo(
                $"[{nameof(ApGoalFlags)}] {system} changed during play ({previous} -> {value}): goal reached, latched as {key}.");
        }

        internal static bool IsLatched(SavableSystem system)
        {
            return SaveManager.GetIntValue(KeyPrefix + system, 0, false) != 0;
        }
    }
}
