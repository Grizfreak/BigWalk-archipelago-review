using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Lists the hub arch doors' TrackedPeckStates with their saveGuid and saved value (read-only).
    internal static class DebugTrackedPeckStateLookup
    {
        internal static void DumpSpawnHubGate()
        {
            var all = UnityEngine.Object.FindObjectsByType<TrackedPeckState>(FindObjectsSortMode.None);
            if (all == null || all.Length == 0)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugTrackedPeckStateLookup)}] No TrackedPeckState found in the current area.");
                return;
            }

            var matches = new List<TrackedPeckState> ();
            foreach (var state in all)
            {
                if (state != null && state.savableSystem == SavableSystem.SpawnHubGate)
                    matches.Add(state);
            }

            Plugin.Log.LogInfo($"[{nameof(DebugTrackedPeckStateLookup)}] {all.Length} TrackedPeckState total, {matches.Count} with savableSystem=SpawnHubGate:");
            foreach (var state in matches)
            {
                var saveIdentity = state.saveIdentity;
                var saveGuid = saveIdentity != null ? saveIdentity.saveGuid : "<no SaveIdentity>";
                var currentValue = saveIdentity != null
                    ? SaveManager.GetIntValue(saveGuid, -12345, false).ToString()
                    : "n/a";
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugTrackedPeckStateLookup)}]   {state.gameObject.name}, label='{state.label}', " +
                    $"hasInitialState={state.hasInitialState}, initialState={state.initialState}, " +
                    $"saveGuid={saveGuid}, SaveManager[{saveGuid}]={currentValue}");
            }
        }
    }
}
