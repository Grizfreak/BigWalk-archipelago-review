using System.Collections.Generic;

namespace BigWalkArchipelago.Core.Net
{
    internal static partial class ApGoalFlags
    {
        // Debug (Alt + 4): latches both flags to test sending the goal.
        internal static void DebugLatchBoth()
        {
            SaveManager.SetIntValue(KeyPrefix + SavableSystem.EndingGate, 1);
            SaveManager.SetIntValue(KeyPrefix + SavableSystem.GauntletComplete, 1);
            Plugin.Log.LogInfo($"[{nameof(ApGoalFlags)}] Debug: the bell and the Gauntlet are latched.");
        }
    }
}
