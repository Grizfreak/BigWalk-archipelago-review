using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

namespace BigWalkArchipelago.Core.Net
{
    // Library events come on network threads: callbacks touch managed types only, never IL2CPP.
    internal sealed partial class ApConnection
    {
        internal enum ConnectionStatus
        {
            Idle,
            Connecting,
            Connected,
            Failed,
        }

        // Matches required_client_version in the apworld's world.py.
        private static readonly Version ClientVersion = new(0, 6, 0);

        // Per attempt, captured by the handler: a dying session must not push into the new queue.
        private volatile ConcurrentQueue<ItemInfo> _incomingItems = new();

        // Items received by name, for the logic; counted on the socket's thread under its own lock.
        private volatile Dictionary<string, int> _receivedByName = new();

        // The hints the server knows that concern this slot, as it last sent them.
        private volatile Hint[] _hints = Array.Empty<Hint>();

        // Locations of this slot that hold a progression item, scouted once at login.
        private volatile HashSet<long> _progressionHere = new();

        private ArchipelagoSession _session;

        // DeathLinks received as "source: cause", for ApRuntime to drain on the main thread.
        private DeathLinkService _deathLink;
        private readonly ConcurrentQueue<string> _deathLinksReceived = new();
        private volatile ConnectionStatus _status = ConnectionStatus.Idle;

        // Bumped for every attempt, so a stale one can be fenced off.
        private volatile int _generation;

        // Written by the connect task; the Status flip to Connected/Failed is the handoff.
        private HashSet<long> _slotLocations = new();

        // Asks the socket: an abrupt server drop can raise ErrorReceived and never SocketClosed.
        internal ConnectionStatus Status
        {
            get
            {
                if (_status == ConnectionStatus.Connected && !IsSocketAlive())
                {
                    LastError = string.IsNullOrEmpty(LastError) ? "connection dropped" : LastError;
                    _status = ConnectionStatus.Failed;
                }

                return _status;
            }
        }

        private bool IsSocketAlive()
        {
            try
            {
                return _session?.Socket != null && _session.Socket.Connected;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }
        // Null until a login has succeeded: Update and patch code must check it.
        internal ApSlotData SlotData { get; private set; }
        internal string SeedName { get; private set; } = string.Empty;
        internal string SlotName { get; private set; } = string.Empty;

        // Hosting another save leaves the socket open; ApRuntime checks this against the save.
        internal string SaveUuid { get; private set; } = string.Empty;
        internal string LastError { get; private set; } = string.Empty;

        internal void BeginConnect(ApEndpoint endpoint)
        {
            if (_status == ConnectionStatus.Connecting || _status == ConnectionStatus.Connected)
                return;

            _status = ConnectionStatus.Connecting;
            SlotName = endpoint.SlotName;
            SaveUuid = endpoint.Uuid;
            LastError = string.Empty;

            var generation = ++_generation;
            Task.Run(() => ConnectBlocking(endpoint, generation));
        }

        // TryConnectAndLogin may never return on a dead port: fenced off by bumping the generation.
        internal void AbandonAttempt(string reason)
        {
            if (_status != ConnectionStatus.Connecting)
                return;

            _generation++;
            LastError = reason;
            _status = ConnectionStatus.Failed;
        }

        private void ConnectBlocking(ApEndpoint endpoint, int generation)
        {
            try
            {
                var session = ArchipelagoSessionFactory.CreateSession(endpoint.Host, endpoint.Port);

                var queue = new ConcurrentQueue<ItemInfo>();
                _incomingItems = queue;
                var received = new Dictionary<string, int>();
                _receivedByName = received;
                _hints = Array.Empty<Hint>();
                _progressionHere = new HashSet<long>();

                // Before login: the server sends the whole backlog once connected.
                session.Items.ItemReceived += (ReceivedItemsHelper helper) => OnItemReceived(helper, queue, received);
                session.Socket.SocketClosed += OnSocketClosed;
                session.Socket.ErrorReceived += OnErrorReceived;

                var result = session.TryConnectAndLogin(
                    "Big Walk",
                    endpoint.SlotName,
                    ItemsHandlingFlags.AllItems,
                    ClientVersion,
                    Array.Empty<string>(),
                    endpoint.Uuid,
                    endpoint.Password,
                    true);

                // An attempt abandoned as too slow must not declare itself connected.
                if (generation != _generation)
                    return;

                if (result is not LoginSuccessful success)
                {
                    LastError = result is LoginFailure failure && failure.Errors is { Length: > 0 }
                        ? string.Join(" / ", failure.Errors)
                        : "unknown login failure";
                    _status = ConnectionStatus.Failed;
                    return;
                }

                var storedDeposits = ReadStoredDeposits(session);

                var deathLink = session.CreateDeathLinkService();
                var slot = endpoint.SlotName;
                deathLink.OnDeathLinkReceived += received =>
                {
                    // Managed types only. One of this slot's own comes back from the server too.
                    if (received == null || received.Source == slot)
                        return;
                    _deathLinksReceived.Enqueue(
                        string.IsNullOrEmpty(received.Cause) ? received.Source : $"{received.Source}: {received.Cause}");
                };

                // A check this slot sent; queued here, shown from the main thread.
                session.MessageLog.OnMessageReceived += message =>
                {
                    try
                    {
                        // The answer to a hint asked at the sign: the hint, or a refusal.
                        if (DateTime.UtcNow < _hintAnswerUntil
                            && (message is Archipelago.MultiClient.Net.MessageLog.Messages.HintItemSendLogMessage
                                || message is Archipelago.MultiClient.Net.MessageLog.Messages.CommandResultLogMessage
                                || message.GetType() == typeof(Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage)))
                        {
                            ApNotices.PostFromSocket($"Hint: {message}");
                        }

                        // A hint is an ItemSendLogMessage too: it is no check.
                        if (message is Archipelago.MultiClient.Net.MessageLog.Messages.ItemSendLogMessage sent
                            && message is not Archipelago.MultiClient.Net.MessageLog.Messages.HintItemSendLogMessage
                            && sent.IsSenderTheActivePlayer && sent.Item != null)
                        {
                            // Always who it went to, this slot included.
                            ApNotices.PostFromSocket($"Check: {sent.Item.LocationName} -> {sent.Item.ItemName} ({sent.Receiver?.Name})");
                        }
                    }
                    catch (Exception)
                    {
                        // An undescribable message is no reason to touch the connection.
                    }
                };

                // The hints of this slot, now and as they change.
                try
                {
                    session.DataStorage.TrackHints(hints => _hints = hints ?? Array.Empty<Hint>(), true);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Could not follow the hints: {ex.Message}");
                }

                // Every location of this slot, quietly: only progression is kept.
                try
                {
                    var ids = new List<long>(session.Locations.AllLocations).ToArray();
                    session.Locations.ScoutLocationsAsync(HintCreationPolicy.None, ids).ContinueWith(OnScouted);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Could not scout the slot's locations: {ex.Message}");
                }

                _session = session;
                _deathLink = deathLink;
                StoredDeposits = storedDeposits;
                SlotData = ApSlotData.From(success.SlotData);
                SeedName = session.RoomState?.Seed ?? string.Empty;
                _slotLocations = new HashSet<long>(session.Locations.AllLocations);

                _status = ConnectionStatus.Connected;
            }
            catch (Exception ex)
            {
                if (generation != _generation)
                    return;

                LastError = ex.Message;
                _status = ConnectionStatus.Failed;
            }
        }

        private void OnSocketClosed(string reason)
        {
            // Never demote Failed, and never touch the game from here: ApRuntime reacts next frame.
            LastError = string.IsNullOrEmpty(reason) ? "connection closed" : reason;
            _status = ConnectionStatus.Failed;
        }

        private void OnErrorReceived(Exception exception, string message)
        {
            // Recorded, not acted on: Status asks the socket; the message names the real cause.
            LastError = string.IsNullOrEmpty(message) ? exception?.Message ?? "socket error" : message;
            Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Socket error: {message} ({exception?.Message}).");
        }

        internal void Reset()
        {
            _session = null;
            _deathLink = null;
            _trapLinkHooked = false;
            _status = ConnectionStatus.Idle;
        }
    }
}
