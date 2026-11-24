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
    internal sealed class ApConnection
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

        // Monument slots filled, from DataStorage; 0 when absent or the read fails.
        internal int StoredDeposits { get; private set; }

        private const string DepositsKey = "bigwalk_deposits";
        private const int StorageWaitMilliseconds = 4000;

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

        // Only ever goes up and a room is one seed; any failure is a 0 and a log line.
        private static int ReadStoredDeposits(ArchipelagoSession session)
        {
            try
            {
                var element = session.DataStorage[Scope.Slot, DepositsKey];
                element.Initialize(0);

                var read = element.GetAsync();
                if (!read.Wait(StorageWaitMilliseconds))
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(ApConnection)}] The server did not answer the deposits read in {StorageWaitMilliseconds} ms; "
                        + "no deposit will be restored this session.");
                    return 0;
                }

                var token = read.Result;
                var count = token != null && token.Type != Newtonsoft.Json.Linq.JTokenType.Null ? token.ToObject<int>() : 0;
                Plugin.Log.LogInfo($"[{nameof(ApConnection)}] The server remembers {count} deposit(s) for this slot.");
                return Math.Max(0, count);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ApConnection)}] Could not read the deposits from the server ({ex.Message}); none will be restored.");
                return 0;
            }
        }

        // The server keeps the larger value, so a late or repeated write can never lower it.
        internal void StoreDeposits(int count)
        {
            if (_status != ConnectionStatus.Connected || count <= 0)
                return;

            try
            {
                _session.DataStorage[Scope.Slot, DepositsKey] += Operation.Max(count);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Could not store {count} deposit(s) on the server: {ex.Message}");
            }
        }

        private static void OnItemReceived(ReceivedItemsHelper helper, ConcurrentQueue<ItemInfo> queue,
            Dictionary<string, int> received)
        {
            // Managed types only: see the threading note at the top.
            try
            {
                var item = helper.DequeueItem();
                queue.Enqueue(item);
                if (item != null && !string.IsNullOrEmpty(item.ItemName))
                {
                    lock (received)
                        received[item.ItemName] = received.TryGetValue(item.ItemName, out var count) ? count + 1 : 1;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Failed to read an incoming item: {ex.Message}");
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

        internal bool TryDequeueItem(out ItemInfo item)
        {
            return _incomingItems.TryDequeue(out item);
        }

        // ApRuntime waits for this to go quiet before reconciling gourds.
        internal bool HasPendingItems => !_incomingItems.IsEmpty;

        // This slot's number in the multiworld; -1 before a login.
        internal int SlotNumber => _session?.ConnectionInfo?.Slot ?? -1;

        // Snapshotted at login: a slot's locations are fixed at generation.
        internal bool BelongsToSlot(long locationId)
        {
            return _slotLocations.Contains(locationId);
        }

        // The datapackage name for a location; falls back to the number when the lookup fails.
        internal string LocationName(long locationId)
        {
            try
            {
                var name = _session?.Locations.GetLocationNameFromId(locationId);
                return string.IsNullOrEmpty(name) ? locationId.ToString() : name;
            }
            catch (Exception)
            {
                return locationId.ToString();
            }
        }

        // Free hints for these locations of this slot; the server announces each once.
        internal void ScoutAsHints(long[] locationIds)
        {
            if (_status != ConnectionStatus.Connected || locationIds.Length == 0)
                return;

            try
            {
                _session.Locations.ScoutLocationsAsync(HintCreationPolicy.CreateAndAnnounceOnce, locationIds);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Failed to scout {locationIds.Length} location(s) as hints: {ex.Message}");
            }
        }

        internal void SendChecks(long[] locationIds)
        {
            if (_status != ConnectionStatus.Connected || locationIds.Length == 0)
                return;

            try
            {
                _session.Locations.CompleteLocationChecks(locationIds);
            }
            catch (Exception ex)
            {
                // Not lost: reported checks are saved and the whole set is resent on reconnect.
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Failed to send {locationIds.Length} check(s), will resend on reconnect: {ex.Message}");
            }
        }

        // Whether the server took it, so the caller can keep the goal armed on a dead socket.
        internal bool SendGoal()
        {
            if (_status != ConnectionStatus.Connected)
                return false;

            try
            {
                _session.SetGoalAchieved();
                Plugin.Log.LogInfo($"[{nameof(ApConnection)}] Goal reported to the server.");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Failed to report the goal: {ex.Message}");
                return false;
            }
        }

        // A Bounce tagged "DeathLink"; sending needs no tag of ours.
        internal bool SendDeathLink(string cause)
        {
            if (_status != ConnectionStatus.Connected || _deathLink == null)
                return false;

            try
            {
                _deathLink.SendDeathLink(new DeathLink(SlotName, cause));
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Failed to send a DeathLink: {ex.Message}");
                return false;
            }
        }

        // Subscribes to the "DeathLink" tag, for `death_link` on receive or both.
        internal void ListenForDeathLinks(bool listen)
        {
            if (_status != ConnectionStatus.Connected || _deathLink == null)
                return;

            try
            {
                if (listen)
                    _deathLink.EnableDeathLink();
                else
                    _deathLink.DisableDeathLink();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Could not change the DeathLink tag: {ex.Message}");
            }
        }

        // TrapLink: Bounce packets tagged "TrapLink", {"time", "source", "trap_name"}.

        private readonly ConcurrentQueue<(string source, string trap)> _trapLinksReceived = new();
        private bool _trapLinkHooked;

        // Asks the server for the TrapLinks of the multiworld (the tag), or stops.
        internal void ListenForTrapLinks(bool listen)
        {
            if (_status != ConnectionStatus.Connected || _session == null)
                return;

            try
            {
                var tags = new List<string>(_session.ConnectionInfo.Tags ?? Array.Empty<string>());
                tags.Remove("TrapLink");
                if (listen)
                    tags.Add("TrapLink");
                _session.ConnectionInfo.UpdateConnectionOptions(tags.ToArray());

                if (listen && !_trapLinkHooked)
                {
                    _trapLinkHooked = true;
                    var own = SlotName;
                    _session.Socket.PacketReceived += packet =>
                    {
                        // Managed types only, off Unity's thread: queued for the main loop.
                        if (packet is not Archipelago.MultiClient.Net.Packets.BouncedPacket bounced
                            || bounced.Tags == null || !bounced.Tags.Contains("TrapLink") || bounced.Data == null)
                            return;
                        var source = bounced.Data.TryGetValue("source", out var s) ? s?.ToString() : null;
                        var trap = bounced.Data.TryGetValue("trap_name", out var t) ? t?.ToString() : null;
                        if (!string.IsNullOrEmpty(trap) && source != own)
                            _trapLinksReceived.Enqueue((source ?? "?", trap));
                    };
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Could not change the TrapLink tag: {ex.Message}");
            }
        }

        internal bool SendTrapLink(string trapName)
        {
            if (_status != ConnectionStatus.Connected || _session == null)
                return false;

            try
            {
                _session.Socket.SendPacket(new Archipelago.MultiClient.Net.Packets.BouncePacket
                {
                    Tags = new List<string> { "TrapLink" },
                    Data = new Dictionary<string, Newtonsoft.Json.Linq.JToken>
                    {
                        ["time"] = (double)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0,
                        ["source"] = SlotName,
                        ["trap_name"] = trapName,
                    },
                });
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Failed to send a TrapLink: {ex.Message}");
                return false;
            }
        }

        internal bool TryDequeueTrapLink(out string source, out string trap)
        {
            var found = _trapLinksReceived.TryDequeue(out var pair);
            source = pair.source;
            trap = pair.trap;
            return found;
        }

        internal bool TryDequeueDeathLink(out string cause)
        {
            return _deathLinksReceived.TryDequeue(out cause);
        }

        // Until when a server message is taken as the answer to a hint asked at the sign.
        private DateTime _hintAnswerUntil = DateTime.MinValue;

        // Like the Text Client's `!hint`: it costs the slot's hint points.
        internal void AskHint(string text)
        {
            if (_session == null || _status != ConnectionStatus.Connected || string.IsNullOrWhiteSpace(text))
                return;
            try
            {
                _hintAnswerUntil = DateTime.UtcNow.AddSeconds(10);
                _session.Say("!hint " + text.Trim());
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Could not ask for a hint: {ex.Message}");
            }
        }

        // The slot's hint points and what a hint costs (-1, -1 when unknown).
        internal (int points, int cost) HintBudget()
        {
            try
            {
                return _session != null ? (_session.RoomState.HintPoints, _session.RoomState.HintCost) : (-1, -1);
            }
            catch (Exception)
            {
                return (-1, -1);
            }
        }

        // What this slot has received, by item name (a copy, safe on the main thread).
        internal Dictionary<string, int> ReceivedByName()
        {
            var received = _receivedByName;
            lock (received)
                return new Dictionary<string, int>(received);
        }

        // The locations of this slot the server knows as checked (a copy; empty if offline).
        internal HashSet<long> CheckedLocations()
        {
            try
            {
                return _session != null ? new HashSet<long>(_session.Locations.AllLocationsChecked) : new HashSet<long>();
            }
            catch (Exception)
            {
                // The socket's thread was adding one: the next look will have it.
                return new HashSet<long>();
            }
        }

        // A method, not a lambda: the interop build lacks NullableAttribute for these types.
        private void OnScouted(Task<Dictionary<long, ScoutedItemInfo>> task)
        {
            if (task.IsFaulted || task.Result == null)
                return;
            var progression = new HashSet<long>();
            foreach (var pair in task.Result)
            {
                if (pair.Value != null && (pair.Value.Flags & ItemFlags.Advancement) != 0)
                    progression.Add(pair.Key);
            }
            _progressionHere = progression;
            Plugin.Log.LogInfo($"[{nameof(ApConnection)}] Scouted the slot: {progression.Count} location(s) hold a progression item.");
        }

        // How many of this slot's locations are not checked yet.
        internal int UncheckedCount(HashSet<long> checkedIds)
        {
            var count = 0;
            foreach (var id in _slotLocations)
            {
                if (!checkedIds.Contains(id))
                    count++;
            }
            return count;
        }

        // Whether a location of this slot holds a progression item (scouted at login).
        internal bool HoldsProgression(long locationId) => _progressionHere.Contains(locationId);

        // The locations of this slot that open hints point to (anyone's item, not found yet).
        internal HashSet<long> HintedLocations()
        {
            var result = new HashSet<long>();
            var session = _session;
            if (session == null)
                return result;
            try
            {
                var me = session.ConnectionInfo.Slot;
                foreach (var hint in _hints)
                {
                    if (hint != null && !hint.Found && hint.FindingPlayer == me)
                        result.Add(hint.LocationId);
                }
            }
            catch (Exception)
            {
                // Read again at the next count.
            }
            return result;
        }

        // Open hints for this slot's items, newest last, as "item: location (finder)".
        internal List<string> OpenHintLines()
        {
            var lines = new List<string>();
            var session = _session;
            if (session == null)
                return lines;

            try
            {
                var me = session.ConnectionInfo.Slot;
                foreach (var hint in _hints)
                {
                    if (hint == null || hint.Found || hint.ReceivingPlayer != me)
                        continue;
                    var finder = session.Players.GetPlayerInfo(hint.FindingPlayer);
                    var receiver = session.Players.GetPlayerInfo(hint.ReceivingPlayer);
                    var item = session.Items.GetItemName(hint.ItemId, receiver?.Game) ?? hint.ItemId.ToString();
                    var location = session.Locations.GetLocationNameFromId(hint.LocationId, finder?.Game) ?? hint.LocationId.ToString();
                    // The slot it is in, even this one.
                    var where = finder == null ? location : $"{location} ({finder.Name})";
                    lines.Add($"{item}: {where}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Could not read the hints: {ex.Message}");
            }

            return lines;
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
