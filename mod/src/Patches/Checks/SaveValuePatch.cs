using System;
using BigWalkArchipelago.Core;
using BigWalkArchipelago.Core.Net;
using HarmonyLib;

namespace BigWalkArchipelago.Patches.Checks
{
    // Catches every gourd save, as some paths (PeckEffectSavableHome) bypass RewardGourd.
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SetIntValue))]
    internal static class SaveValuePatch
    {
        private static void Postfix(string key, int value)
        {
#if DEBUG
            if (Debug.DebugStateLog.On)
                Debug.DebugStateLog.ObserveWrite(key, value);
#endif

            // Goal flags first and for every value: a change to zero may be the one that matters.
            if (Enum.TryParse<SavableSystem>(key, out var goalSystem))
            {
                ApGoalFlags.Observe(goalSystem, value);

#if DEBUG
                if (ModConfig.DebugModeEnabled.Value)
                    Debug.DebugSystemWrites.Observe(goalSystem, value);
#endif
            }

            // Zero means unpinned: never a check.
            if (value == 0)
                return;

            if (Enum.TryParse<SaveablePropName>(key, out var propName))
            {
                if (GourdRegistry.TryGetLocationId(propName, out var locationId)
                    && CheckTracker.TryMarkReported(locationId))
                    Plugin.Reporter.ReportCheck(locationId);

                return;
            }

            // SavableSystem keys: only the radio stations are locations, the rest is filtered out.
            if (!Enum.TryParse<SavableSystem>(key, out var system) || system == SavableSystem.NotSavable)
                return;

            // Only the ids the apworld defines resolve here; ApRuntime then filters by slot.
            if (!ApLocationIds.TryResolveLocation(key, out _))
                return;

            if (CheckTracker.TryMarkReported(key))
                Plugin.Reporter.ReportCheck(key);
        }
    }
}
