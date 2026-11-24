using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    // Client main loop, host only. Items are applied on the main thread, never from the socket.
    internal partial class ApRuntime : MonoBehaviour
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

        internal static string LocationName(long locationId) => Connection.LocationName(locationId);

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
    }
}
