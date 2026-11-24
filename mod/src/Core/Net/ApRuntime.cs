using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    // Client main loop, host only. Items are applied on the main thread, never from the socket.
    internal class ApRuntime : MonoBehaviour
    {
        // Required by Il2CppInterop for any type injected into IL2CPP.
        public ApRuntime(IntPtr ptr) : base(ptr)
        {
        }

        private const float RetryDelaySeconds = 10f;
        private const float PollIntervalSeconds = 1f;

        // A reconnect replays every item, so spread them over frames to avoid a freeze.
        private const int MaxItemsPerFrame = 4;

        // How long to wait for the server's replay before an empty queue means "all done".
        private const float ReplayGraceSeconds = 3f;

        // The client library has no reliable timeout of its own.
        private const float ConnectTimeoutSeconds = 15f;


        // Queued so a check made while disconnected is not lost.
        private static readonly ConcurrentQueue<string> PendingLocationNames = new();

        private static readonly ApConnection Connection = new();

        internal static void ScoutAsHints(long[] locationIds) => Connection.ScoutAsHints(locationIds);

        internal static ApSlotData SlotData => Connection.SlotData;

        internal static ApConnection Current => Connection;

        internal static bool ShufflesPegTiles =>
            Connection.Status == ApConnection.ConnectionStatus.Connected && Connection.SlotData != null;

        // A trap from this world's pool, sent to the other games (TrapLink).
        internal static bool SendTrapLink(string trapName) => Connection.SendTrapLink(trapName);

        internal static void ApplyDeathLinkSetting()
        {
            Connection.ListenForDeathLinks(Traps.ReceivesDeathLink);
            Plugin.Log.LogInfo($"[{nameof(ApRuntime)}] DeathLink {(Traps.DeathLinkOn ? "on (as the seed says)" : "off")} in the settings.");
        }

        internal static string LocationName(long locationId) => Connection.LocationName(locationId);

        private static float _lastDeathLink = -10f;

        internal static void SendDeathLink()
        {
            if (Time.unscaledTime - _lastDeathLink < 3f)
                return;

            _lastDeathLink = Time.unscaledTime;
            SendDeathLink("pressed the DeathLink key", announce: true);
        }

        internal static void SendDeathLink(string what, bool announce = false)
        {
            var cause = $"{Connection.SlotName} {what}";
            var sent = Connection.SendDeathLink(cause);
            if (sent)
                ApSounds.DeathLinkAlert();
            if (announce)
                ApNotices.Post(sent ? $"DeathLink sent: {what}" : "DeathLink not sent: not connected", warning: !sent);
            Plugin.Log.LogInfo($"[{nameof(ApRuntime)}] DeathLink {(sent ? "sent" : "NOT sent (not connected)")}: {cause}.");
            SessionJournal.Write("deathlink", $"{(sent ? "sent" : "not sent")} ({cause})");
        }

        internal static bool SessionSettled => _looseGourdsRestored;
        private static readonly List<long> SendBuffer = new();

        // _seen counts this session's queue; _applied is what this save already has (ApItemCursor).
        private static int _seenItemCount;
        private static int _appliedItemCount;

        private static bool _worldWasReady;

        private static bool _worldWasPresent;
        private static bool _looseGourdsRestored;
        private static bool _looseRestoreBlockedLogged;
        private static bool _reconciliationLogged;
        private static int _looseGourdsRestoredCount;
        private static int _gourdsSeenThisSession;
        private static int _gourdsSpawnedThisSession;
        private static float _restoreTimer;
        private static float _connectedAt;

        // Gadgets have no deposit sink: what is owed is "received minus spawned this session".
        private static readonly Dictionary<GadgetKind, int> _gadgetsSpawnedThisSession = new();
        private static bool _looseGadgetsRestored;
        private static bool _looseGadgetsRestoreBlockedLogged;
        private static float _gadgetRestoreTimer;

        private static int _lastReportedDepositCount = -1;

        // Set once a world is ready: an empty save count would refill slots the save already holds.
        private static bool _depositBudgetSet;

        private static int _depositsOnServer;

        // Replayed gourds that go back into monuments instead of being dropped at the hub.
        private static int _skipGourdSpawns;

        // Cached on the poll tick: GetFilledMonumentCount scans the whole save.
        private static int _depositCount;
        internal static int DepositCount => _depositCount;

        private static bool _goalSent;

        private static bool _goalDetectable;
        private static float _retryTimer;
        private static float _connectingSince;
        private static float _pollTimer;
        private static bool _disabledNoticeLogged;
        private static string _lastConnectProblem = string.Empty;

        // Decided here, not in the overlay, since OnGUI runs several times per frame.
        internal static string StatusMessage { get; private set; }
        internal static bool StatusIsWarning { get; private set; }

        internal static string GoalLine { get; private set; }

        internal static bool GoalLineIsWarning { get; private set; }

        internal static void QueueLocation(string locationName)
        {
            // A non-host never connects, so its queue would only grow.
            if (string.IsNullOrEmpty(locationName) || !NetworkServer.active)
                return;

            // A check made with Archipelago off belongs to no seed.
            if (!ModConfig.ArchipelagoEnabled.Value)
                return;

            PendingLocationNames.Enqueue(locationName);
        }

        // Checks belong to the save going away; the ap_reported_* ledger resends them later.
        private static void ReleasePendingChecksWithTheWorld()
        {
            if (Connection.Status == ApConnection.ConnectionStatus.Connected)
                FlushPendingChecks();

            var dropped = 0;
            while (PendingLocationNames.TryDequeue(out _))
                dropped++;

            if (dropped > 0)
                Plugin.Log.LogInfo(
                    $"[{nameof(ApRuntime)}] {dropped} check(s) still waiting for the server went with the world that made "
                    + "them; its save resends them on its next connection.");
        }

        // Host only, one at a time on the whole map, so two restocks never pull the same items.
        internal static bool ResyncAll(Vector3? gatherAt = null, bool gadgets = true)
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogInfo($"[{nameof(ApRuntime)}] Resync ignored: only the host can do this.");
                ApNotices.Post("Only the host can resync", warning: true);
                return false;
            }

            if (ResyncRunning)
            {
                ApNotices.Post("A resync is already running", warning: true);
                return false;
            }

            if (gatherAt != null)
                ReceivedItemSpawner.GatherAt(gatherAt.Value);

            if (!ResyncGourds())
                return false;

            ResyncKeys();
            if (gadgets)
                ResyncGadgets();

            _resyncSince = Time.unscaledTime;
            _resyncGadgets = gadgets;
            return true;
        }

        private static float _resyncSince = -1f;
        private static bool _resyncGadgets;
        private const float ResyncShortest = 3f;
        private const float ResyncLongest = 60f;

        // Times out after a minute so a restock that cannot finish never locks the stations.
        internal static bool ResyncRunning
        {
            get
            {
                if (_resyncSince < 0f)
                    return false;

                var age = Time.unscaledTime - _resyncSince;
                var restocking = !_looseGourdsRestored || (_resyncGadgets && !_looseGadgetsRestored);
                if (age < ResyncShortest || (restocking && age < ResyncLongest))
                    return true;

                _resyncSince = -1f;
                return false;
            }
        }

        internal static void ResyncKeys()
        {
            var moved = KeyCustody.ResyncToSpawn();
            if (moved <= 0)
                return;

            Plugin.Log.LogInfo($"[{nameof(ApRuntime)}] {moved} big key(s) sent back to the spawn point.");
            ApNotices.Post($"Resync: {moved} key(s) sent back to the spawn point");
        }

        internal static bool ResyncGourds()
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogInfo($"[{nameof(ApRuntime)}] Gourd resync ignored: only the host can do this.");
                return false;
            }

            // The rebuild only runs on a live connection; sweeping now would give nothing back.
            if (Connection.Status != ApConnection.ConnectionStatus.Connected)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ApRuntime)}] Gourd resync refused: not connected to Archipelago, so the gourds could not be put back. Try again once reconnected.");
                ApNotices.Post("Resync refused: not connected to Archipelago", warning: true);
                return false;
            }

            var removed = ReceivedItemSpawner.DestroyLooseCosmeticGourds(out var keptGourds);

            _looseGourdsRestored = false;
            _looseRestoreBlockedLogged = false;
            _reconciliationLogged = false;
            _looseGourdsRestoredCount = 0;

            // Gourds in backpacks, cartons and belts still exist; zero would restock them twice.
            _gourdsSpawnedThisSession = keptGourds;
            _restoreTimer = 0f;

            Plugin.Log.LogInfo(
                $"[{nameof(ApRuntime)}] Gourd resync: {removed} loose gourd(s) cleared and {keptGourds} left where "
                + "they were stowed, restocking the hub from the ledger.");
            ApNotices.Post($"Resync: {removed} gourd(s) cleared, restocking");
            return true;
        }

        // Sweep and tally reset go together: one without the other duplicates or loses gadgets.
        internal static void ResyncGadgets()
        {
            if (!NetworkServer.active || Connection.Status != ApConnection.ConnectionStatus.Connected)
                return;

            var removed = GadgetItemSpawner.DestroyLooseCosmeticGadgets(out var keptGadgets);

            _gadgetsSpawnedThisSession.Clear();
            foreach (var pair in keptGadgets)
                _gadgetsSpawnedThisSession[pair.Key] = pair.Value;
            _looseGadgetsRestored = false;
            _looseGadgetsRestoreBlockedLogged = false;
            _gadgetRestoreTimer = 0f;

            Plugin.Log.LogInfo(
                $"[{nameof(ApRuntime)}] Gadget resync: {removed} loose gadget(s) cleared, restocking from the ledger.");
            ApNotices.Post($"Resync: {removed} item(s) cleared, restocking from the ledger");
        }

        private static void RefreshStatusMessage()
        {
            // A non-host never connects by design, so a warning would be a lie.
            if (!NetworkServer.active)
            {
                StatusMessage = null;
                GoalLine = null;
                return;
            }

            RefreshGoalLine();

            // Otherwise a session played with the switch off shows no sign of it.
            if (!ModConfig.ArchipelagoEnabled.Value)
            {
                StatusMessage = "Archipelago: off";
                StatusIsWarning = false;
                return;
            }

            switch (Connection.Status)
            {
                case ApConnection.ConnectionStatus.Connecting:
                    StatusMessage = "Archipelago: connecting...";
                    StatusIsWarning = false;
                    break;

                case ApConnection.ConnectionStatus.Connected:
                    StatusMessage = "Archipelago: connected";
                    StatusIsWarning = false;
                    break;

                default:
                    StatusMessage = string.IsNullOrEmpty(Connection.LastError)
                        ? "Archipelago: not connected"
                        : $"Archipelago: disconnected - {Connection.LastError} (retrying)";
                    StatusIsWarning = true;
                    break;
            }
        }

        // Uses the cached count: this runs every frame and the tracker walks the whole save.
        private static void RefreshGoalLine()
        {
            RefreshGoalLineCore();

            var towers = MonumentOrder.HudLine();
            if (towers != null)
                GoalLine = string.IsNullOrEmpty(GoalLine) ? towers : GoalLine + "\n" + towers;
        }

        private static void RefreshGoalLineCore()
        {
            // SlotData is null until login, and this runs from the first hosting frame.
            var slotData = Connection.SlotData;
            if (slotData == null || !ModConfig.ArchipelagoEnabled.Value)
            {
                GoalLine = null;
                return;
            }

            if (ApObjectives.Active)
            {
                GoalLine = ModConfig.ShowObjectives.Value ? ApObjectives.Summary() : null;
                GoalLineIsWarning = !ApObjectives.Detectable;
                return;
            }

            string goal;
            switch (slotData.Goal)
            {
                case "gauntlet":
                    goal = "Big Goodbye, complete the Silent Gauntlet behind the Big Wall";
                    break;

                case "ending":
                    goal = "Big Wall, break the bell inside the Big Wall";
                    break;

                case "second_ending":
                    goal = "Big Game, reach the secret ending behind the Spawn Secret Door";
                    break;

                case "deposits":
                    var target = slotData.DepositGoalAmount;
                    goal = $"Big Collection, place {target} {GourdNames.Plural} in the towers' slots ({_depositCount}/{target})";
                    break;

                default:
                    GoalLine = $"Goal: {slotData.Goal} - this build cannot detect it";
                    GoalLineIsWarning = true;
                    return;
            }

            // "reached" means reported to the server, not merely met.
            GoalLine = _goalSent ? $"Goal: {goal} - reached" : $"Goal: {goal}";
            GoalLineIsWarning = false;
        }

        private void Update()
        {
            RefreshStatusMessage();

            ApNotices.Prune();

            // Every frame: a delivered key is pulled back by the game, a 1s poll is too slow.
            KeyCustody.SettleDelivered();

            // The game can flip this lock the instant a monument fills; the 1s poll is too slow.
            KeyCustody.EnforceCustodyTick();

            // Before the early returns: debug simulators deliver items with Archipelago off.
            ReceivedItemSpawner.DrainPendingHandover();

            // Tracked apart from _worldWasReady: the switch can change between two worlds.
            var worldPresent = NetworkServer.active && WorldManager.isReadyForEffects;
            if (!worldPresent && _worldWasPresent)
                ReleasePendingChecksWithTheWorld();
            _worldWasPresent = worldPresent;

            if (!ModConfig.ArchipelagoEnabled.Value)
            {
                if (!_disabledNoticeLogged)
                {
                    _disabledNoticeLogged = true;
                    Plugin.Log.LogInfo(
                        $"[{nameof(ApRuntime)}] Archipelago disabled in the config; checks are logged locally only.");
                }

                return;
            }

            // On "world ready", not "connected": a reload destroys loose gourds, the socket stays.
            var worldReady = NetworkServer.active && WorldManager.isReadyForEffects;
            if (worldReady && !_worldWasReady)
                OnWorldBecameReady();

            // Goal flags baseline from writes made while a world loads, so reset when it goes away.
            if (!worldReady && _worldWasReady)
            {
                ApGoalFlags.OnWorldUnloaded();
                PegTileShuffler.ForgetWorld();

                ApNotices.Clear();
            }

            _worldWasReady = worldReady;

            if (!NetworkServer.active)
                return;

            switch (Connection.Status)
            {
                case ApConnection.ConnectionStatus.Idle:
                case ApConnection.ConnectionStatus.Failed:
                    TickReconnect();
                    return;

                case ApConnection.ConnectionStatus.Connecting:
                    // TryConnectAndLogin may never return against a dead port.
                    if (Time.unscaledTime - _connectingSince > ConnectTimeoutSeconds)
                    {
                        Plugin.Log.LogWarning(
                            $"[{nameof(ApRuntime)}] Connection attempt gave no answer after {ConnectTimeoutSeconds:0}s; abandoning it and retrying.");
                        Connection.AbandonAttempt($"no answer after {ConnectTimeoutSeconds:0}s");
                        _retryTimer = 0f;
                    }

                    return;
            }

            if (_appliedItemCount < 0)
                OnJustConnected();

            // Hosting another save keeps the socket open; rebuild so the old cursor is not reused.
            if (HasSaveChanged())
                return;

            FlushPendingChecks();

            while (Connection.TryDequeueDeathLink(out var deathLinkCause))
                Traps.OnDeathLinkReceived(deathLinkCause);

            while (Connection.TryDequeueTrapLink(out var linkSource, out var linkTrap))
                Traps.OnTrapLinkReceived(linkSource, linkTrap);

            if (WorldManager.isReadyForEffects)
            {
                // Before the replay, since it decides what the replay drops.
                SetDepositBudgetOnce();
                DrainIncomingItems();

                RestoreLooseGourds();
                RestoreLooseGadgets();
                PuzzleFailures.TickPresses();
            }

            _pollTimer -= Time.unscaledDeltaTime;
            if (_pollTimer > 0f)
                return;

            _pollTimer = PollIntervalSeconds;

            if (WorldManager.isReadyForEffects)
            {
                _depositCount = CosmeticMonumentFillTracker.GetFilledMonumentCount();

                StoreDepositsIfRaised();
                ReportDeposits();

                RadioStations.TickPending();
                KeyFeatures.TickPending();

                KeyCustody.Tick();
            }

            // Outside the world gate: an ending tears its world down while the socket is still up.
            CheckGoal();
        }

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
                    + "Goal completion will NOT be reported automatically — update the mod to match the apworld.");
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

        // Reported checks are marked in the save, so offline checks are only sent from here.
        private static void ResendKnownChecks()
        {
            SendBuffer.Clear();

            foreach (var locationName in CheckTracker.GetReportedLocationNames())
            {
                if (ApLocationIds.TryResolveLocation(locationName, out var id)
                    && Connection.BelongsToSlot(id))
                    SendBuffer.Add(id);
            }

            if (SendBuffer.Count == 0)
                return;

            Plugin.Log.LogInfo($"[{nameof(ApRuntime)}] Resending {SendBuffer.Count} check(s) already validated on this save.");
            Connection.SendChecks(SendBuffer.ToArray());
        }

        private static void FlushPendingChecks()
        {
            SendBuffer.Clear();

            while (PendingLocationNames.TryDequeue(out var locationName))
            {
                if (!ApLocationIds.TryResolveLocation(locationName, out var id))
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(ApRuntime)}] No Archipelago id for '{locationName}', check dropped.");
                    continue;
                }

                // Sending an id this slot does not have is a protocol error.
                if (!Connection.BelongsToSlot(id))
                    continue;

                SendBuffer.Add(id);
            }

            if (SendBuffer.Count == 0)
                return;

            Connection.SendChecks(SendBuffer.ToArray());

            // Only newly flushed checks; the resend on connect is not announced.
            foreach (var id in SendBuffer)
            {
                var name = Connection.LocationName(id);
                if (name == null || !name.EndsWith("Puzzle", StringComparison.Ordinal))
                {
                    ApSounds.CheckSent();
                    break;
                }
            }
        }

        // Owed = received - deposited - spawned this session; only pinned gourds persist.
        private static void RestoreLooseGourds()
        {
            if (_looseGourdsRestored || Connection.HasPendingItems)
                return;

            // An empty queue right after connecting usually means the replay has not landed yet.
            if (Time.unscaledTime - _connectedAt < ReplayGraceSeconds)
                return;

            if (_gourdsSeenThisSession > ApItemCursor.GourdsReceived)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(ApRuntime)}] Gourd ledger rebuilt from the server: {_gourdsSeenThisSession} received "
                    + $"(this save recorded {ApItemCursor.GourdsReceived}).");
                ApItemCursor.SetGourdsReceived(_gourdsSeenThisSession);
            }

            var deposited = CosmeticMonumentFillTracker.GetFilledMonumentCount();
            var owed = ApItemCursor.GourdsReceived - deposited - _gourdsSpawnedThisSession;

            if (!_reconciliationLogged)
            {
                _reconciliationLogged = true;
                Plugin.Log.LogInfo(
                    $"[{nameof(ApRuntime)}] Gourd reconciliation: {ApItemCursor.GourdsReceived} received, "
                    + $"{deposited} deposited, {_gourdsSpawnedThisSession} already spawned -> {owed} to restore.");
            }

            if (owed <= 0)
            {
                _looseGourdsRestored = true;
                if (_looseGourdsRestoredCount > 0)
                    Plugin.Log.LogInfo(
                        $"[{nameof(ApRuntime)}] Restored {_looseGourdsRestoredCount} gourd(s) received but never deposited.");
                return;
            }

            // One at a time on the game's spawn point; computed offsets lost gourds off the map.
            _restoreTimer -= Time.unscaledDeltaTime;
            if (_restoreTimer > 0f)
                return;

            _restoreTimer = Mathf.Max(0f, ModConfig.CosmeticGourdRestoreInterval.Value);

            // A failed spawn (hub not loaded yet) is not counted, so it is retried.
            if (!ItemApplier.ApplyGourdItem(toPlayer: false))
            {
                if (!_looseRestoreBlockedLogged)
                {
                    _looseRestoreBlockedLogged = true;
                    Plugin.Log.LogInfo(
                        $"[{nameof(ApRuntime)}] Cannot spawn the {owed} owed gourd(s) yet (the hub's spawn point is "
                        + "probably not loaded); still trying.");
                }

                return;
            }

            _gourdsSpawnedThisSession++;
            _looseGourdsRestoredCount++;
        }

        private static int Kept(int received)
        {
            var limit = ModConfig.GadgetCopiesKept.Value;
            return limit > 0 ? Math.Min(received, limit) : received;
        }

        private static void RestoreLooseGadgets()
        {
            if (_looseGadgetsRestored || Connection.HasPendingItems)
                return;

            if (Time.unscaledTime - _connectedAt < ReplayGraceSeconds)
                return;

            _gadgetRestoreTimer -= Time.unscaledDeltaTime;
            if (_gadgetRestoreTimer > 0f)
                return;

            GadgetKind? next = null;
            var allSettled = true;

            foreach (GadgetKind kind in Enum.GetValues(typeof(GadgetKind)))
            {
                var spawnedThisSession = _gadgetsSpawnedThisSession.TryGetValue(kind, out var n) ? n : 0;
                var owed = Kept(ApItemCursor.GadgetsReceived(kind)) - spawnedThisSession;
                if (owed <= 0)
                    continue;

                allSettled = false;
                next = kind;
                break;
            }

            if (allSettled)
            {
                _looseGadgetsRestored = true;
                return;
            }

            _gadgetRestoreTimer = Mathf.Max(0f, ModConfig.CosmeticGourdRestoreInterval.Value);

            if (!ItemApplier.ApplyGadgetItem(next.Value, toPlayer: false))
            {
                if (!_looseGadgetsRestoreBlockedLogged)
                {
                    _looseGadgetsRestoreBlockedLogged = true;
                    Plugin.Log.LogInfo(
                        $"[{nameof(ApRuntime)}] Cannot restore the owed {next.Value} yet (the hub's spawn point is "
                        + "probably not loaded); still trying.");
                }

                return;
            }

            _gadgetsSpawnedThisSession[next.Value] =
                (_gadgetsSpawnedThisSession.TryGetValue(next.Value, out var count) ? count : 0) + 1;
        }

        private static void DrainIncomingItems()
        {
            var appliedThisFrame = 0;

            while (appliedThisFrame < MaxItemsPerFrame && Connection.TryDequeueItem(out var item))
            {
                _seenItemCount++;

                // Counted even for skipped items: this tally rebuilds the gourd ledger.
                var isGourd = item.ItemId == ApLocationIds.GourdItemId;
                if (isGourd)
                    _gourdsSeenThisSession++;

                var isGadget = ApLocationIds.TryResolveGadgetItem(item.ItemId, out var gadgetKind);
                // `pack_sanity: vanilla`: a pack that is its own pick-up's item is already there.
                if (isGadget && PackChecks.IsPackItself(item))
                    isGadget = false;

                // Already in this save from an earlier session: the server's replay.
                if (_seenItemCount <= _appliedItemCount)
                    continue;

                // Owed whether or not the prop spawned; the next reconciliation makes good on it.
                if (isGourd)
                    ApItemCursor.CountGourdReceived();
                else if (isGadget)
                    ApItemCursor.CountGadgetReceived(gadgetKind);

                // A reconnect can post many at once, which is why ApNotices collapses repeats.
                ApNotices.Post($"Received: {Palette.Display(GourdNames.Display(item.ItemId, item.ItemName))}");
                if (Traps.TryResolveItem(item.ItemId, out var trapKey) && !TrapEffects.IsBonus(trapKey))
                    ApSounds.DeathLinkAlert();
                else
                    ApSounds.Received();
                SessionJournal.Write(
                    "item", $"{item.ItemName} | from {item.Player?.Name} | at {item.LocationName}", withPosition: false);

                try
                {
                    var materialized = Apply(item);
                    if (isGourd && materialized)
                        _gourdsSpawnedThisSession++;
                    else if (isGourd)
                        // The prop did not appear: re-arm the reconciler so the gourd comes back.
                        _looseGourdsRestored = false;
                    else if (isGadget && materialized)
                        _gadgetsSpawnedThisSession[gadgetKind] =
                            (_gadgetsSpawnedThisSession.TryGetValue(gadgetKind, out var count) ? count : 0) + 1;
                    else if (isGadget)
                        _looseGadgetsRestored = false;
                }
                catch (Exception ex)
                {
                    // Still advance the cursor, or every later item would be stuck behind this one.
                    Plugin.Log.LogError(
                        $"[{nameof(ApRuntime)}] Failed to apply item {item.ItemId}: {ex}");
                }

                appliedThisFrame++;
                _appliedItemCount = _seenItemCount;
                ApItemCursor.Set(_appliedItemCount);
            }
        }

        // Returns whether the item materialized, which is what the gourd reconciliation counts.
        private static bool Apply(Archipelago.MultiClient.Net.Models.ItemInfo item)
        {
            var itemId = item.ItemId;

            if (PackChecks.IsPackItself(item))
                return true;

            if (itemId == ApLocationIds.GourdItemId && !_looseGourdsRestored && _skipGourdSpawns > 0)
            {
                _skipGourdSpawns--;
                return true;
            }

            if (itemId == ApLocationIds.GourdItemId)
                // The startup burst goes to the spawn; only later items go to the player's hands.
                return ItemApplier.ApplyGourdItem(toPlayer: _looseGourdsRestored);

            // Keys before doors: a key id would otherwise match the door range.
            if (MonumentOrder.IsItem(itemId))
                return MonumentOrder.Grant();

            if (ApLocationIds.TryResolveKeyItem(itemId, out var keyName))
                return KeyCustody.Grant(keyName, toPlayer: _looseGourdsRestored);

            if (ApLocationIds.TryResolveBigKeyItem(itemId, out var propName))
                return ItemApplier.ApplyBigKeyItem(propName);

            if (ApLocationIds.TryResolveRadioItem(itemId, out var station))
                return RadioStations.Grant(station);

            if (ApLocationIds.TryResolveArchDoorItem(itemId, out var archDoor))
                return ArchDoors.Grant(archDoor);

            if (PuzzleNeeds.TryResolveItem(itemId, out var need))
                return PuzzleNeeds.Grant(need);

            if (ZoneLocks.IsItem(itemId))
                return ZoneLocks.Grant(itemId);

            if (TunnelDoors.IsItem(itemId))
                return TunnelDoors.Grant(itemId);

            if (AbilityLocks.TryResolveItem(itemId, out var ability))
                return AbilityLocks.Grant(ability);

            if (ApLocationIds.TryResolveGauntletItem(itemId, out var stairway))
                return GauntletStairways.Grant(stairway);

            if (TeleportButtons.TryResolveItem(itemId, out var teleporter))
                return TeleportButtons.Grant(teleporter);

            if (ApLocationIds.TryResolveGadgetItem(itemId, out var gadgetKind))
            {
                var spawned = _gadgetsSpawnedThisSession.TryGetValue(gadgetKind, out var already) ? already : 0;
                if (!_looseGadgetsRestored && spawned >= Kept(int.MaxValue))
                    return true;
                return ItemApplier.ApplyGadgetItem(gadgetKind, toPlayer: _looseGadgetsRestored);
            }

            if (Traps.TryResolveItem(itemId, out var effect))
            {
                var queued = Traps.Enqueue(effect, $"item {item.ItemName}");
                if (queued)
                    Traps.SendTrapLink(effect, item.ItemName);
                return queued;
            }

            Plugin.Log.LogInfo($"[{nameof(ApRuntime)}] Received '{item.ItemName}' ({itemId}): no in-game effect.");
            return false;
        }

        // Counted across all monuments, never per tower, so no gourd distribution can lock a seed.
        private static void SetDepositBudgetOnce()
        {
            if (_depositBudgetSet || Connection.Status != ApConnection.ConnectionStatus.Connected)
                return;

            _depositBudgetSet = true;
            var filled = CosmeticMonumentFillTracker.GetFilledMonumentCount();
            var owed = Math.Max(0, Connection.StoredDeposits - filled);
            _depositsOnServer = Connection.StoredDeposits;
            _skipGourdSpawns = owed > 0 ? CosmeticMonumentFillTracker.PutDepositsBack(owed) : 0;
            Plugin.Log.LogInfo(
                $"[{nameof(ApRuntime)}] Deposits: the server remembers {Connection.StoredDeposits}, this save holds "
                + $"{filled} -> {_skipGourdSpawns} slot(s) marked filled, their gourds come back as the monuments load.");
        }

        private static void StoreDepositsIfRaised()
        {
            if (Connection.Status != ApConnection.ConnectionStatus.Connected)
                return;

            if (_depositCount > _depositsOnServer)
            {
                _depositsOnServer = _depositCount;
                Connection.StoreDeposits(_depositCount);
            }
        }

        private static void ReportDeposits()
        {
            var amounts = Connection.SlotData.DepositLocationAmounts;
            if (amounts.Length == 0)
                return;

            var filled = _depositCount;
            if (filled <= _lastReportedDepositCount)
                return;

            _lastReportedDepositCount = filled;
            SendBuffer.Clear();

            foreach (var amount in amounts)
            {
                if (amount > filled)
                    break;

                var id = ApLocationIds.DepositLocationId(amount);
                if (Connection.BelongsToSlot(id))
                    SendBuffer.Add(id);
            }

            if (SendBuffer.Count > 0)
                Connection.SendChecks(SendBuffer.ToArray());
        }

        // Called when the second ending's gourd comes loose; needs no world, only a socket.
        internal static void ReportGoalIfReached()
        {
            CheckGoal();
        }

        private static void CheckGoal()
        {
            if (_goalSent || !IsGoalReached())
                return;

            // Marked sent only once the socket took it; the next tick or connection retries.
            if (Connection.SendGoal())
                _goalSent = true;
        }

        private static bool IsGoalReached()
        {
            // Called from a Harmony patch whether or not this session ever connected.
            if (Connection.SlotData == null)
                return false;

            if (ApObjectives.Active)
                return ApObjectives.AllDone;

            switch (Connection.SlotData.Goal)
            {
                case "gauntlet":
                    return ApGoalFlags.IsLatched(SavableSystem.GauntletComplete);

                case "ending":
                    return ApGoalFlags.IsLatched(SavableSystem.EndingGate);

                case "deposits":
                    var target = Connection.SlotData.DepositGoalAmount;
                    return target > 0 && _depositCount >= target;

                case "second_ending":
                    return ApSecondEnding.Reached;

                default:
                    // Unreachable: OnJustConnected refuses an unknown goal.
                    return false;
            }
        }
    }
}
