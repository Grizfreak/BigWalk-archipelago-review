using BigWalkArchipelago.Core;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Dumps the radio state: learned dial slots, every BroadcastStation and the ledger.
    internal static class DebugRadioLookup
    {
        internal static void Dump()
        {
            Plugin.Log.LogInfo($"[{nameof(DebugRadioLookup)}] === radio dump ===");
            Plugin.Log.LogInfo(
                $"[{nameof(DebugRadioLookup)}] Archipelago items in play: {RadioStations.ItemsInPlay}");

            DumpDial();
            DumpBroadcastStations();
            DumpLedger();

            Plugin.Log.LogInfo($"[{nameof(DebugRadioLookup)}] === end of radio dump ===");
        }

        private static void DumpDial()
        {
            // FmRadioManager.instance throws when there is no instance.
            if (!RadioStations.TryGetManager(out var manager))
            {
                Plugin.Log.LogInfo($"[{nameof(DebugRadioLookup)}] No FmRadioManager in this world.");
                return;
            }

            var groups = manager.stationTrackGroups;
            if (groups == null)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugRadioLookup)}] FmRadioManager has no stationTrackGroups.");
                return;
            }

            Plugin.Log.LogInfo($"[{nameof(DebugRadioLookup)}] stationTrackGroups: {groups.Length} entries.");
            for (var i = 0; i < groups.Length; i++)
            {
                var group = groups[i];
                var groupName = group != null ? group.name : "<null>";

                var owner = "<not learned>";
                foreach (var system in RadioStations.All)
                {
                    if (RadioStations.LearnedDialPosition(system) == i)
                    {
                        owner = system.ToString();
                        break;
                    }
                }

                var unlocked = FmRadioManager.GetUnlockState(i, out var transitioning, out var transitionTime);
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugRadioLookup)}]   [{i}] '{groupName}' | learned owner {owner} "
                    + $"| unlocked={unlocked} transitioning={transitioning} ({transitionTime:0.00}s)");
            }
        }

        private static void DumpBroadcastStations()
        {
            var stations = UnityEngine.Object.FindObjectsByType<BroadcastStation>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            // Printed even when zero, so an empty result is not mistaken for no run.
            Plugin.Log.LogInfo(
                $"[{nameof(DebugRadioLookup)}] BroadcastStation instances loaded: {stations.Length}"
                + (stations.Length == 0 ? " (the towers stream in with the world; walk nearer one)" : string.Empty));

            var localPlayer = Core.LocalPlayer.Find();
            Vector3? playerPosition = localPlayer != null ? localPlayer.transform.position : (Vector3?)null;
            if (!playerPosition.HasValue)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugRadioLookup)}] NO LOCAL PLAYER FOUND, the distances below are meaningless.");
            }

            foreach (var station in stations)
            {
                if (station == null)
                    continue;

                // Every station is loaded everywhere, so distance is what tells zones apart.
                var distance = playerPosition.HasValue
                    ? Vector3.Distance(station.transform.position, playerPosition.Value)
                    : -1f;

                var groupName = station.musicGroup != null ? station.musicGroup.name : "<null>";
                var peckSystem = station.peckSystemReference.peckSystem;
                var systemName = peckSystem != null ? peckSystem.savableSystem.ToString() : "<no peck system>";
                var known = RadioStations.TryGetSystem(station, out var system);
                var dialIndex = known ? RadioStations.LearnedDialPosition(system) : -1;
                var saved = known ? SaveManager.GetIntValue(system.ToString(), 0, false) : 0;

                Plugin.Log.LogInfo(
                    $"[{nameof(DebugRadioLookup)}]   {distance.ToString("0.0").PadLeft(7)}m  musicGroup '{groupName}' "
                    + $"| savableSystem {systemName} | known={known} learnedDialPos={dialIndex} "
                    + $"| SaveManager value={saved} | granted={(known && RadioStations.IsGranted(system))}");
            }
        }

        private static void DumpLedger()
        {
            foreach (var system in RadioStations.All)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugRadioLookup)}]   ledger {system}: granted={RadioStations.IsGranted(system)} "
                    + $"| game value={SaveManager.GetIntValue(system.ToString(), 0, false)}");
            }
        }
    }
}
