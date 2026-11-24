using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Checks
{
    // FM stations as items. The game's unlock lives in RAM only, so grants are re-applied.
    internal static class RadioStations
    {
        // A prefix no game key uses, so SaveValuePatch ignores these writes.
        private const string KeyPrefix = "ap_radio_";

        // Stored as position + 1, so 0 means "never learned".
        private const string DialKeyPrefix = "ap_radio_dial_";

        // SavableSystem also has FmStation7/8/9, which the apworld leaves out.
        internal static readonly SavableSystem[] All =
        {
            SavableSystem.FmStationSleuthFm,
            SavableSystem.FmStationKosmische,
            SavableSystem.FmStationDanceFm,
            SavableSystem.FmStationBreathwork,
            SavableSystem.FmStationJourneyBeat,
            SavableSystem.FmStationAFJ,
            SavableSystem.FmStationFourthSpace,
        };

        // Not cleared on disconnect: a network hiccup must not hand out free stations.
        internal static bool ItemsInPlay { get; private set; }

        private static readonly HashSet<SavableSystem> Pending = new();

        private static bool _pendingBlockedLogged;

        internal static void Configure(bool itemsInPlay)
        {
            if (ItemsInPlay == itemsInPlay)
                return;

            ItemsInPlay = itemsInPlay;
            Plugin.Log.LogInfo(
                $"[{nameof(RadioStations)}] Radio stations are "
                + (itemsInPlay
                    ? "Archipelago items: the game's own unlock is suppressed until each one arrives."
                    : "left vanilla: switching one on unlocks its music on the spot."));
        }

        internal static bool IsGranted(SavableSystem system)
        {
            // A guest has no ledger of its own and believes the host.
            if (!NetworkServer.active && NetworkClient.active)
                return MirroredGrants.Contains(system);

            return SaveManager.GetIntValue(KeyPrefix + system, 0, false) != 0;
        }


        private static readonly HashSet<SavableSystem> MirroredGrants = new();

        // A world reload rebuilds FmRadioManager, so this is forgotten and re-applied then.
        private static readonly HashSet<SavableSystem> MirrorUnlockedThisWorld = new();

        internal static void ApplyFromHost(bool itemsInPlay, List<SavableSystem> granted)
        {
            Configure(itemsInPlay);

            foreach (var system in granted)
            {
                if (MirroredGrants.Add(system))
                    Plugin.Log.LogInfo($"[{nameof(RadioStations)}] The host was granted {system}; playing it here too.");
            }

            if (!itemsInPlay)
                return;

            foreach (var system in granted)
            {
                if (!MirrorUnlockedThisWorld.Contains(system))
                    Pending.Add(system);
            }

            if (Pending.Count == 0)
                return;

            try
            {
                TickPending();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(RadioStations)}] Could not unlock the host's stations here: {ex.Message}");
            }

            foreach (var system in granted)
            {
                if (!Pending.Contains(system))
                    MirrorUnlockedThisWorld.Add(system);
            }
        }

        internal static void ForgetMirroredWorld()
        {
            MirrorUnlockedThisWorld.Clear();
        }

        internal static void ForgetMirror()
        {
            MirroredGrants.Clear();
            MirrorUnlockedThisWorld.Clear();
            Pending.Clear();
            Configure(false);
        }

        // Persisted first: the ledger survives a reload, the live unlock does not.
        internal static bool Grant(SavableSystem system)
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(RadioStations)}] Ignored: {system} received while not host.");
                return false;
            }

            SaveManager.SetIntValue(KeyPrefix + system, 1);
            Pending.Add(system);

            var unlocked = TickPending();
            Plugin.Log.LogInfo(
                $"[{nameof(RadioStations)}] {system} granted"
                + (unlocked ? " and playing." : "; it will start playing once the radio is loaded."));

            return true;
        }

        // A save rebound to another seed must not keep music that seed never gave it.
        internal static void ClearLedger()
        {
            var cleared = 0;
            foreach (var system in All)
            {
                if (!IsGranted(system))
                    continue;

                SaveManager.SetIntValue(KeyPrefix + system, 0);
                cleared++;
            }

            Pending.Clear();
            _pendingBlockedLogged = false;

            if (cleared > 0)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(RadioStations)}] {cleared} station grant(s) cleared: this save is being replayed "
                    + "against a different seed or slot.");
            }
        }

        // Also called on connect into a loaded world, since the cursor skips the replayed stations.
        internal static void RearmFromLedger()
        {
            Pending.Clear();
            _pendingBlockedLogged = false;

            if (!ItemsInPlay)
                return;

            foreach (var system in All)
            {
                if (IsGranted(system))
                    Pending.Add(system);
            }

            if (Pending.Count > 0)
                TickPending();
        }

        // FmRadioManager may not exist yet right after a world loads; retried each tick.
        internal static bool TickPending()
        {
            if (Pending.Count == 0)
                return true;

            if (!TryGetManager(out var manager))
            {
                LogBlockedOnce("no FmRadioManager in the world yet");
                return false;
            }

            // Learned for every station so a later session can unlock one with no tower loaded.
            LearnDialPositions(manager);

            // Copied first: Unlock runs the game's listeners, which may touch this set.
            var attempts = new List<SavableSystem>(Pending);
            foreach (var system in attempts)
            {
                if (!TryUnlock(manager, system))
                    continue;

                Pending.Remove(system);
                Plugin.Log.LogInfo($"[{nameof(RadioStations)}] {system} is now playing.");
            }

            if (Pending.Count > 0)
            {
                LogBlockedOnce("the radio is there but none of its stations could be matched");
                return false;
            }

            _pendingBlockedLogged = false;
            return true;
        }

        private static void LogBlockedOnce(string reason)
        {
            if (_pendingBlockedLogged)
                return;

            _pendingBlockedLogged = true;
            Plugin.Log.LogInfo(
                $"[{nameof(RadioStations)}] {Pending.Count} station(s) waiting: {reason}. Still trying.");
        }

        private static bool TryUnlock(FmRadioManager manager, SavableSystem system)
        {
            var group = FindMusicGroup(manager, system);
            if (group == null)
                return false;

            try
            {
                FmRadioManager.Unlock(group);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[{nameof(RadioStations)}] FmRadioManager.Unlock failed for {system}: {ex}");
                return false;
            }
        }

        // The dial order does not match the FmStation* enum order, so the dial position is learned.
        private static MusicGroup FindMusicGroup(FmRadioManager manager, SavableSystem system)
        {
            var fromStation = FindMusicGroupFromBroadcastStation(system);
            if (fromStation != null)
                return fromStation;

            var groups = manager.stationTrackGroups;
            var learned = SaveManager.GetIntValue(DialKeyPrefix + system, 0, false) - 1;
            if (groups == null || learned < 0 || learned >= groups.Length)
                return null;

            var group = groups[learned];
            if (group != null)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(RadioStations)}] {system}: no BroadcastStation loaded, using the dial position "
                    + $"this save learned earlier ({learned} -> '{group.name}').");
            }

            return group;
        }

        private static void LearnDialPositions(FmRadioManager manager)
        {
            var groups = manager.stationTrackGroups;
            if (groups == null)
                return;

            var stations = UnityEngine.Object.FindObjectsByType<BroadcastStation>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var station in stations)
            {
                if (station == null || station.musicGroup == null
                    || !TryGetSystem(station, out var system))
                    continue;

                for (var i = 0; i < groups.Length; i++)
                {
                    if (groups[i] == null || groups[i].Pointer != station.musicGroup.Pointer)
                        continue;

                    // Position + 1: GetIntValue returns 0 for a missing key.
                    var key = DialKeyPrefix + system;
                    if (SaveManager.GetIntValue(key, 0, false) != i + 1)
                    {
                        SaveManager.SetIntValue(key, i + 1);
                        Plugin.Log.LogInfo(
                            $"[{nameof(RadioStations)}] Learned {system} -> dial position {i} ('{groups[i].name}').");
                    }

                    break;
                }
            }
        }

        // FmRadioManager.instance throws when there is no instance; always go through here.
        internal static bool TryGetManager(out FmRadioManager manager)
        {
            try
            {
                manager = FmRadioManager.instance;
            }
            catch (Exception)
            {
                manager = null;
            }

            return manager != null;
        }

        private static MusicGroup FindMusicGroupFromBroadcastStation(SavableSystem system)
        {
            var stations = UnityEngine.Object.FindObjectsByType<BroadcastStation>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var station in stations)
            {
                if (station == null || station.musicGroup == null)
                    continue;

                if (TryGetSystem(station, out var stationSystem) && stationSystem == system)
                    return station.musicGroup;
            }

            return null;
        }

        internal static bool TryGetSystem(BroadcastStation station, out SavableSystem system)
        {
            system = SavableSystem.NotSavable;
            if (station == null)
                return false;

            var peckSystem = station.peckSystemReference.peckSystem;
            if (peckSystem == null)
                return false;

            system = peckSystem.savableSystem;
            return IsRealStation(system);
        }

        // Not the position in All: the two orders are unrelated.
        internal static int LearnedDialPosition(SavableSystem system)
        {
            return SaveManager.GetIntValue(DialKeyPrefix + system, 0, false) - 1;
        }

        // Shared by the location and item ids, so they always agree on which stations exist.
        internal static bool IsRealStation(SavableSystem system)
        {
            return Array.IndexOf(All, system) >= 0;
        }
    }
}
