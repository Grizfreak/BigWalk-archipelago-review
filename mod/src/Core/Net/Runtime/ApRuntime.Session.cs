using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    internal partial class ApRuntime : MonoBehaviour
    {
        // The ledger (ap_gourds_received) survives a reload; only what exists right now resets.
        private static void OnWorldBecameReady()
        {
            ReceivedItemSpawner.ForgetPendingHandovers();

            // Radio unlocks live in RAM only, so granted stations are re-applied per world.
            RadioStations.RearmFromLedger();

            // The game keeps these doors via the key in its plinth, which the mod keeps inert.
            KeyFeatures.RearmFromLedger();
            KeyCustody.RearmFromLedger();

            KeyCustody.ForgetReleaseLogics();

            _looseGourdsRestored = false;
            _looseRestoreBlockedLogged = false;
            _reconciliationLogged = false;
            _looseGourdsRestoredCount = 0;
            _gourdsSpawnedThisSession = 0;
            _restoreTimer = 0f;

            _gadgetsSpawnedThisSession.Clear();
            _looseGadgetsRestored = false;
            _looseGadgetsRestoreBlockedLogged = false;
            _gadgetRestoreTimer = 0f;
        }

        private static bool HasSaveChanged()
        {
            var save = SaveManager.instance != null ? SaveManager.instance.currentData : null;
            if (save == null)
                return false;

            var uuid = !string.IsNullOrWhiteSpace(save.filenameUid) ? save.filenameUid : save.slotName;
            if (string.IsNullOrEmpty(uuid) || uuid == Connection.SaveUuid)
                return false;

            Plugin.Log.LogInfo(
                $"[{nameof(ApRuntime)}] A different save is now loaded; dropping the Archipelago session and reconnecting for it.");

            Connection.Reset();
            _appliedItemCount = -1;
            _retryTimer = 0f;
            return true;
        }

        private static void TickReconnect()
        {
            _retryTimer -= Time.unscaledDeltaTime;
            if (_retryTimer > 0f)
                return;

            _retryTimer = RetryDelaySeconds;

            if (Connection.Status == ApConnection.ConnectionStatus.Failed)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ApRuntime)}] Archipelago connection lost or refused ({Connection.LastError}); retrying in {RetryDelaySeconds:0}s.");
                Connection.Reset();
            }

            if (!ApEndpoint.TryResolve(out var endpoint, out var problem))
            {
                // Once per cause: an empty address would otherwise spam the log on every retry.
                if (problem != _lastConnectProblem)
                {
                    _lastConnectProblem = problem;
                    Plugin.Log.LogInfo($"[{nameof(ApRuntime)}] Not connecting: {problem}.");
                }

                return;
            }

            _lastConnectProblem = string.Empty;
            _connectingSince = Time.unscaledTime;
            Plugin.Log.LogInfo(
                $"[{nameof(ApRuntime)}] Connecting to {endpoint.Host}:{endpoint.Port} as '{endpoint.SlotName}'...");

            _appliedItemCount = -1;
            Connection.BeginConnect(endpoint);
        }

        private static void OnJustConnected()
        {
            ApLocationIds.Configure(Connection.SlotData);

            // Before the cursor: the radio patch reads it on every peck, a world may be loaded.
            RadioStations.Configure(Connection.SlotData.RadioStationItems);

            // The key patch reads it on every pin.
            KeyFeatures.Configure(Connection.SlotData.BigKeyFeatures);
            KeyCustody.Configure(Connection.SlotData.BigKeyItems);
            // Before the ledger below, which it clears by this seed's door ids.
            TunnelDoors.Configure(Connection.SlotData.TunnelDoors);

            _seenItemCount = 0;
            _appliedItemCount = ApItemCursor.SyncTo(Connection.SeedName, Connection.SlotName);

            // A cursor at zero means the save was rebound to another seed: drop its stations.
            if (_appliedItemCount == 0)
            {
                ArchDoors.ClearLedger();
                RadioStations.ClearLedger();
                KeyFeatures.ClearLedger();
                KeyCustody.ClearLedger();
                PuzzleNeeds.ClearLedger();
                ZoneLocks.ClearLedger();
                TunnelDoors.ClearLedger();
                AbilityLocks.ClearLedger();
                PackChecks.ClearLightLedger();
                GauntletStairways.ClearLedger();
                MonumentOrder.ClearLedger();
                TeleportButtons.ClearLedger();
                Traps.ClearLedger();
            }

            // After the ledger, so a rebound save starts with no doors.
            ArchDoors.Configure(Connection.SlotData.LockedArchDoors, Connection.SlotData.LinearArchDoors);

            PuzzleNeeds.Configure(
                Connection.SlotData.LockPuzzleNeeds,
                Connection.SlotData.PuzzleNeedKeys,
                ApLocationIds.Base + Connection.SlotData.PuzzleNeedIdOffset);
            ZoneLocks.Configure(Connection.SlotData.ZoneLocks, Connection.SlotData.ZoneLockedGadgets);
            AbilityLocks.Configure(
                Connection.SlotData.LockAbilities,
                Connection.SlotData.LockRunning,
                Connection.SlotData.RidesRequireSeat,
                Connection.SlotData.AbilityKeys,
                ApLocationIds.Base + Connection.SlotData.AbilityIdOffset);
            GauntletStairways.Configure(
                Connection.SlotData.GauntletMode == "locked_stages",
                Connection.SlotData.GauntletPuzzlesRequired,
                Connection.SlotData.LockGauntletNeeds);
            BlackTowerDoor.Configure(Connection.SlotData.OpenBlackTower);
            TileThiefHelper.Configure(Connection.SlotData.TileThief);
            PegTileShuffler.Configure(Connection.SeedName);
            ResyncStations.Configure(Connection.SlotData.TowerResyncStations, Connection.SlotData.GuestsCanResync);
            CabinFeverWaits.Configure(
                Connection.SlotData.CabinFeverMode, Connection.SlotData.CabinFeverSeconds, Connection.SlotData.CabinFeverHelp,
                Connection.SlotData.CabinFeverHideTimer,
                Connection.SlotData.CabinFeverLongMode, Connection.SlotData.CabinFeverLongSeconds,
                Connection.SlotData.CabinFeverLongHelp, Connection.SlotData.CabinFeverLongHideTimer);
            TeleportButtons.Configure(
                Connection.SlotData.TeleportButtons,
                Connection.SlotData.TeleportIdOffset,
                Connection.SlotData.TeleportDestinations,
                Connection.SlotData.TeleportBackToHub);
            Traps.Configure(Connection.SlotData);
            Traps.AnnounceOverrides();
            PackChecks.Configure(Connection.SlotData);
            Palette.Configure(Connection.SlotData);
            CustomTiles.Configure(Connection.SlotData);
            GourdNames.Configure(Connection.SlotData);
            Connection.ListenForDeathLinks(Traps.ReceivesDeathLink);
            Connection.ListenForTrapLinks(Connection.SlotData.TrapLink);
            SessionJournal.Write(
                "connected", $"{Connection.SlotName} | {Connection.SlotData.Describe()} | {_appliedItemCount} item(s) already applied");

            _lastReportedDepositCount = -1;
            _depositCount = 0;
            _depositBudgetSet = false;
            _depositsOnServer = 0;
            _skipGourdSpawns = 0;


            _gourdsSeenThisSession = 0;
            _connectedAt = Time.unscaledTime;

            var goal = Connection.SlotData.Goal;
            _goalDetectable = ApObjectives.Active
                ? ApObjectives.Detectable
                : goal is "gauntlet" or "ending" or "deposits" or "second_ending";
            _goalSent = !_goalDetectable;
            if (!_goalDetectable)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ApRuntime)}] This slot's goal is '{(ApObjectives.Active ? string.Join(", ", ApObjectives.Keys) : goal)}', which this version of the mod cannot detect. "
                    + "Goal completion will NOT be reported automatically, update the mod to match the apworld.");
            }

            Plugin.Log.LogInfo(
                $"[{nameof(ApRuntime)}] Connected. {Connection.SlotData.Describe()}. "
                + $"{_appliedItemCount} item(s) already applied to this save.");
            WarnIfNewerApworld(Connection.SlotData.WorldVersion);

            // Resend first, so nothing that might throw below can skip it.
            ResendKnownChecks();

            // Stations and doors already in this save are skipped by the cursor: re-arm them.
            RadioStations.RearmFromLedger();
            KeyFeatures.RearmFromLedger();
            KeyCustody.RearmFromLedger();
        }

        // Warns only for a seed from a newer apworld: the mod ignores options it does not know.
        private static void WarnIfNewerApworld(string worldVersion)
        {
            if (!TryMajorMinor(worldVersion, out var seed) || !TryMajorMinor(Plugin.PluginVersion, out var mod) || seed.CompareTo(mod) <= 0)
                return;

            Plugin.Log.LogWarning(
                $"[{nameof(ApRuntime)}] This seed was generated with apworld {worldVersion}, newer than this mod ({Plugin.PluginVersion}): "
                + "every player should update the mod, or the seed may not be finishable.");
            ApNotices.Post($"This seed needs a newer mod (apworld {worldVersion}, mod {Plugin.PluginVersion}): update the mod",
                warning: true, seconds: 30f);
        }

        private static bool TryMajorMinor(string version, out (int Major, int Minor) value)
        {
            value = default;
            var parts = (version ?? string.Empty).Split('.');
            if (parts.Length < 2 || !int.TryParse(parts[0], out var major) || !int.TryParse(parts[1], out var minor))
                return false;

            value = (major, minor);
            return true;
        }
    }
}
