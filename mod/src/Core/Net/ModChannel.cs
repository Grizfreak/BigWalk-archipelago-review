using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime.Attributes;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    // Raw Mirror messages from the host; an unknown id disconnects a peer, hence the beacon.
    internal class ModChannel : MonoBehaviour
    {
        // Required by Il2CppInterop for any injected type.
        public ModChannel(IntPtr ptr) : base(ptr)
        {
        }

        // Clear of Big TV's 0xB17B, and checked against other handlers before being claimed.
        internal const ushort MessageId = 0xB1A9;

        // In the mod's own asset id block, clear of the gourd and gadget ids.
        internal const uint BeaconAssetId = 0xB16_9BFE;

        // A mismatch is logged once and ignored rather than half-read.
        private const byte ProtocolVersion = 26;

        private const byte KindHello = 1;
        private const byte KindSnapshot = 2;

        // Debug only: sent only to a loopback guest on this PC that said hello.
        private const byte KindSummon = 3;

        // The press reaches the host's button, but the teleport must happen on the guest's machine.
        private const byte KindPress = 4;

        private const byte KindNotice = 5;

        private const byte KindEffect = 6;

        // The only kind a guest sends besides hello.
        private const byte KindStatus = 7;

        private const byte KindMap = 8;

        private const byte KindBlob = 9;

        private const float SnapshotIntervalSeconds = 1f;

        private const float SnapshotStaleSeconds = 5f;

        private static readonly HashSet<int> Announced = new();
        private static GameObject _beacon;
        private static bool _serverHandlerRegistered;
        private float _nextSnapshot;

        private static bool _clientHandlerRegistered;
        private static bool _beaconHandlerRegistered;
        private static bool _hostHasMod;
        private static bool _helloSent;
        private static bool _versionMismatchLogged;

        internal sealed class MirroredLine
        {
            internal string Text;
            internal bool Warning;
        }

        private static readonly byte[] _heardCounts = new byte[ApSounds.Counts.Length];

        internal static string MirroredStatus { get; private set; }
        internal static bool MirroredStatusIsWarning { get; private set; }
        internal static string MirroredGoal { get; private set; }
        internal static bool MirroredGoalIsWarning { get; private set; }
        internal static readonly List<MirroredLine> MirroredLines = new();
        private static float _receivedAt = -1f;

        // A guest and host on different builds ignore each other; the overlay says so.
        private const float SilentGuestSeconds = 60f;
        private const float UnansweredHelloSeconds = 25f;

        private static readonly Dictionary<int, byte> MismatchedGuests = new();
        private static readonly Dictionary<int, float> FirstSeen = new();
        private static readonly HashSet<int> SilentGuests = new();

        private static bool _hostIsAnotherBuild;
        private static float _helloAt = -1f;

        internal static List<string> VersionWarnings()
        {
            var lines = new List<string>();
            if (NetworkServer.active)
            {
                foreach (var id in MismatchedGuests.Keys)
                    lines.Add($"{PlayerLabel(id)} runs another build of the mod: they see none of this session's overlay, traps or colors. Everyone needs the same build.");
                foreach (var id in SilentGuests)
                    lines.Add($"{PlayerLabel(id)} has not said hello after {SilentGuestSeconds:0}s: no mod, or an old build. Everyone needs the same build.");
                return lines;
            }

            var unanswered = _hostHasMod && _helloSent && _receivedAt < 0f && _helloAt >= 0f
                             && Time.unscaledTime - _helloAt > UnansweredHelloSeconds;
            if (_hostIsAnotherBuild || unanswered)
                lines.Add("The host runs another build of the mod: update yours so both match, or you will see none of its overlay, traps or colors.");
            return lines;
        }

        // The player's display name, else the connection number; never the platform id.
        internal static string PlayerLabel(int connectionId)
        {
            try
            {
                if (NetworkServer.connections.TryGetValue(connectionId, out var connection) && connection?.identity != null)
                {
                    var networking = connection.identity.GetComponentInChildren<PlayerNetworking>();
                    var name = networking != null ? networking.moderationNameSanitized : null;
                    if (string.IsNullOrWhiteSpace(name) && networking != null)
                        name = networking.moderationName;
                    if (!string.IsNullOrWhiteSpace(name))
                        return name.Length > 24 ? name.Substring(0, 24) : name.Trim();
                }
            }
            catch (Exception)
            {
                // The label is a courtesy: the number does.
            }

            return $"Player #{connectionId}";
        }

        private static float _nextGuestCheck;

        private static void TrackGuests()
        {
            if (Time.unscaledTime < _nextGuestCheck)
                return;
            _nextGuestCheck = Time.unscaledTime + 1f;

            var live = new HashSet<int>();
            var local = NetworkServer.localConnection != null ? NetworkServer.localConnection.connectionId : 0;
            foreach (var connection in NetworkServer.connections.Values)
            {
                if (connection == null || connection.connectionId == local)
                    continue;
                live.Add(connection.connectionId);

                // Only a connection with a player in the world counts: a loading one is not silent.
                if (connection.identity == null || Announced.Contains(connection.connectionId)
                    || MismatchedGuests.ContainsKey(connection.connectionId))
                    continue;
                if (!FirstSeen.TryGetValue(connection.connectionId, out var since))
                    FirstSeen[connection.connectionId] = Time.unscaledTime;
                else if (Time.unscaledTime - since >= SilentGuestSeconds && SilentGuests.Add(connection.connectionId))
                    Plugin.Log.LogWarning($"[{nameof(ModChannel)}] {PlayerLabel(connection.connectionId)} (connection #{connection.connectionId}) has not said hello after {SilentGuestSeconds:0}s: no mod, or an old build.");
            }

            foreach (var id in new List<int>(MismatchedGuests.Keys))
            {
                if (!live.Contains(id))
                    MismatchedGuests.Remove(id);
            }
            foreach (var id in new List<int>(FirstSeen.Keys))
            {
                if (!live.Contains(id))
                    FirstSeen.Remove(id);
            }
            SilentGuests.RemoveWhere(id => !live.Contains(id) || Announced.Contains(id));
        }

        internal static bool HasFreshSnapshot =>
            !NetworkServer.active
            && _receivedAt >= 0f
            && Time.unscaledTime - _receivedAt < SnapshotStaleSeconds;

        private void Update()
        {
            try
            {
                if (NetworkServer.active)
                    TickHost();
                else
                    ResetHost();

                if (NetworkClient.active && !NetworkServer.active)
                    TickGuest();
                else
                    ResetGuest();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ModChannel)}] Tick failed, ignored: {ex.Message}");
            }
        }

        [HideFromIl2Cpp]
        private void TickHost()
        {
            if (!_serverHandlerRegistered || !NetworkServer.handlers.ContainsKey(MessageId))
                RegisterServerHandler();

            if (WorldManager.isReadyForEffects && _beacon == null)
                SpawnBeacon();

            PruneAnnounced();
            TrackGuests();
            TickRepublish();
            PumpBlobs();

            if (Time.unscaledTime < _nextSnapshot || Announced.Count == 0)
                return;

            _nextSnapshot = Time.unscaledTime + SnapshotIntervalSeconds;
            SendSnapshotToAnnounced();
        }

        private static void PumpBlobs()
        {
            var guests = new List<NetworkConnection>();
            foreach (var connection in NetworkServer.connections.Values)
            {
                if (connection != null && Announced.Contains(connection.connectionId))
                    guests.Add(connection);
            }
            TileSounds.PumpHost(guests);
            CustomTiles.PumpHost(guests);
        }

        internal static void SendBlobChunk(NetworkConnection connection, byte channel, string key, ulong hash, byte[] compressed, int offset, int count)
        {
            try
            {
                var chunk = new byte[count];
                Buffer.BlockCopy(compressed, offset, chunk, 0, count);
                var writer = new NetworkWriter();
                NetworkWriterExtensions.WriteUShort(writer, MessageId);
                writer.WriteByte(KindBlob);
                writer.WriteByte(ProtocolVersion);
                writer.WriteByte(channel);
                NetworkWriterExtensions.WriteString(writer, key);
                NetworkWriterExtensions.WriteULong(writer, hash);
                NetworkWriterExtensions.WriteInt(writer, compressed.Length);
                NetworkWriterExtensions.WriteInt(writer, offset);
                NetworkWriterExtensions.WriteInt(writer, count);
                writer.WriteBytes((Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<byte>)chunk, 0, count);
                connection.Send(writer.ToArraySegment(), 0);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ModChannel)}] Could not send '{key}' to guest #{connection.connectionId}: {ex.Message}");
            }
        }

        private static void ResetHost()
        {
            Announced.Clear();
            MismatchedGuests.Clear();
            FirstSeen.Clear();
            SilentGuests.Clear();
            _beacon = null;
            _serverHandlerRegistered = false;
        }

        private static void RegisterServerHandler()
        {
            var handlers = NetworkServer.handlers;
            if (handlers == null)
                return;

            if (handlers.ContainsKey(MessageId))
            {
                // Ours from a previous session may be replaced; anyone else's is not ours to take.
                if (_serverHandlerRegistered)
                    handlers.Remove(MessageId);
                else
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(ModChannel)}] Message id 0x{MessageId:X4} is already taken on the server; "
                        + "guests will not be told anything this session.");
                    _serverHandlerRegistered = true;
                    return;
                }
            }

            NetworkMessageDelegate handler = (Action<NetworkConnection, NetworkReader, int>)OnServerMessage;
            handlers[MessageId] = handler;
            _serverHandlerRegistered = true;
            Plugin.Log.LogInfo($"[{nameof(ModChannel)}] Listening for guests on 0x{MessageId:X4}.");
        }

        private static void SpawnBeacon()
        {
            try
            {
                var beacon = new GameObject("Big Walk Archipelago beacon (AP)");
                var identity = beacon.AddComponent<NetworkIdentity>();

                // Past interest management, or a far guest would never learn the host runs the mod.
                identity.visible = Visibility.ForceShown;

                NetworkServer.Spawn(beacon, BeaconAssetId);
                _beacon = beacon;
                Plugin.Log.LogInfo(
                    $"[{nameof(ModChannel)}] Beacon spawned (netId {identity.netId}); guests running the mod will say hello.");
            }
            catch (Exception ex)
            {
                // Not retried: a beacon that cannot spawn will not spawn a frame later either.
                _beacon = new GameObject("Big Walk Archipelago beacon (failed)");
                Plugin.Log.LogWarning($"[{nameof(ModChannel)}] Could not spawn the beacon: {ex.Message}");
            }
        }

        private static void PruneAnnounced()
        {
            if (Announced.Count == 0)
                return;

            var live = new HashSet<int>();
            foreach (var connection in NetworkServer.connections.Values)
            {
                if (connection != null)
                    live.Add(connection.connectionId);
            }

            Announced.RemoveWhere(id => !live.Contains(id));
        }

        // Sent again twice after hello: the guest's world is usually still filling in.
        private static readonly float[] RepublishDelays = { 3f, 8f };
        private static readonly List<float> RepublishAt = new();

        private static void TickRepublish()
        {
            for (var i = RepublishAt.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime < RepublishAt[i])
                    continue;

                RepublishAt.RemoveAt(i);
                ReceivedItemSpawner.RepublishHolds();
            }
        }

        // Runs inside Mirror's dispatch: a throw disconnects the sender, so nothing may escape.
        private static void OnServerMessage(NetworkConnection connection, NetworkReader reader, int channelId)
        {
            try
            {
                var kind = reader.ReadByte();
                if (kind == KindStatus)
                {
                    if (reader.ReadByte() == ProtocolVersion && Announced.Contains(connection.connectionId))
                        Traps.NoteGuestMasked(connection.connectionId, NetworkReaderExtensions.ReadBool(reader));
                    return;
                }

                if (kind != KindHello)
                    return;

                var version = reader.ReadByte();
                if (version != ProtocolVersion)
                {
                    MismatchedGuests[connection.connectionId] = version;
                    Plugin.Log.LogWarning(
                        $"[{nameof(ModChannel)}] Guest #{connection.connectionId} runs protocol v{version}, this host "
                        + $"v{ProtocolVersion}; not mirroring to it. Both players need the same build of the mod.");
                    return;
                }

                MismatchedGuests.Remove(connection.connectionId);
                SilentGuests.Remove(connection.connectionId);
                if (Announced.Add(connection.connectionId))
                {
                    Plugin.Log.LogInfo(
                        $"[{nameof(ModChannel)}] Guest #{connection.connectionId} runs the mod; mirroring the overlay "
                        + "and the radio to it.");
                    SendSnapshot(connection);
                    Traps.OnGuestJoined(connection);
                    foreach (var delay in RepublishDelays)
                        RepublishAt.Add(Time.unscaledTime + delay);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ModChannel)}] Unreadable message from a guest, ignored: {ex.Message}");
            }
        }

        internal static void SendSnapshotNow()
        {
            if (NetworkServer.active)
                SendSnapshotToAnnounced();
        }

        private static void SendSnapshotToAnnounced()
        {
            foreach (var connection in NetworkServer.connections.Values)
            {
                if (connection != null && Announced.Contains(connection.connectionId))
                    SendSnapshot(connection);
            }
        }

        private static void SendSnapshot(NetworkConnection connection)
        {
            try
            {
                var writer = new NetworkWriter();
                NetworkWriterExtensions.WriteUShort(writer, MessageId);
                writer.WriteByte(KindSnapshot);
                writer.WriteByte(ProtocolVersion);

                NetworkWriterExtensions.WriteString(writer, ApRuntime.StatusMessage ?? string.Empty);
                NetworkWriterExtensions.WriteBool(writer, ApRuntime.StatusIsWarning);
                NetworkWriterExtensions.WriteString(writer, ApRuntime.GoalLine ?? string.Empty);
                NetworkWriterExtensions.WriteBool(writer, ApRuntime.GoalLineIsWarning);

                var lines = ApNotices.Lines;
                var count = Math.Min(lines.Count, byte.MaxValue);
                writer.WriteByte((byte)count);
                for (var i = 0; i < count; i++)
                {
                    NetworkWriterExtensions.WriteString(writer, lines[i].Display ?? string.Empty);
                    NetworkWriterExtensions.WriteBool(writer, lines[i].Warning);
                }

                NetworkWriterExtensions.WriteBool(writer, RadioStations.ItemsInPlay);
                var granted = new List<SavableSystem>();
                foreach (var system in RadioStations.All)
                {
                    if (RadioStations.IsGranted(system))
                        granted.Add(system);
                }

                writer.WriteByte((byte)granted.Count);
                foreach (var system in granted)
                    writer.WriteByte((byte)(int)system);

                NetworkWriterExtensions.WriteBool(writer, PuzzleNeeds.Enabled);
                var lockedNeeds = PuzzleNeeds.LockedNeeds();
                writer.WriteByte((byte)lockedNeeds.Count);
                foreach (var need in lockedNeeds)
                    NetworkWriterExtensions.WriteString(writer, need);

                NetworkWriterExtensions.WriteBool(writer, GauntletStairways.Enabled);
                NetworkWriterExtensions.WriteBool(writer, GauntletStairways.PartsHidden);
                NetworkWriterExtensions.WriteUShort(writer, (ushort)TeleportButtons.HostMask());
                NetworkWriterExtensions.WriteUShort(writer, (ushort)CabinFeverWaits.Seconds(0));
                NetworkWriterExtensions.WriteUShort(writer, (ushort)CabinFeverWaits.Seconds(1));
                writer.WriteByte((byte)((CabinFeverWaits.Help(0) ? 1 : 0) | (CabinFeverWaits.Help(1) ? 2 : 0)
                                        | (TileThiefHelper.HostMode << 2)
                                        | (CabinFeverWaits.HideTimer(0) ? 16 : 0) | (CabinFeverWaits.HideTimer(1) ? 32 : 0)));
                NetworkWriterExtensions.WriteUShort(writer, ResyncStations.HostState());
                NetworkWriterExtensions.WriteString(writer, GourdNames.Current);
                writer.WriteByte(PackChecks.HostMode);
                writer.WriteByte(PackChecks.HostObjectMode);
                NetworkWriterExtensions.WriteBool(writer, FastGolf.HostLightweight);
                var kinds = new List<string>(PackChecks.HostCountedKinds);
                writer.WriteByte((byte)kinds.Count);
                foreach (var kind in kinds)
                    NetworkWriterExtensions.WriteString(writer, kind);
                var collected = new List<string>(PackChecks.HostCollected);
                writer.WriteByte((byte)Math.Min(collected.Count, byte.MaxValue));
                for (var i = 0; i < collected.Count && i < byte.MaxValue; i++)
                    NetworkWriterExtensions.WriteString(writer, collected[i]);
                var colours = Palette.HostColours;
                writer.WriteByte((byte)Math.Min(colours.Length, byte.MaxValue));
                for (var i = 0; i < colours.Length && i < byte.MaxValue; i++)
                    writer.WriteByte((byte)colours[i]);
                NetworkWriterExtensions.WriteString(writer, ObjectiveBoard.HostText());
                writer.WriteByte(ObjectiveBoard.HostLocks());
                writer.WriteByte(MonumentOrder.HostMask());
                for (var kind = 0; kind < ApSounds.Counts.Length; kind++)
                    writer.WriteByte(ApSounds.Counts[kind]);
                NetworkWriterExtensions.WriteBool(writer, AbilityLocks.Enabled);
                NetworkWriterExtensions.WriteUShort(writer, AbilityLocks.HostMask());
                writer.WriteByte(HintBoards.HostFlags());
                NetworkWriterExtensions.WriteString(writer, HintBoards.HostNames());
                var zoneLocked = new List<string>(ZoneLocks.HostLocked);
                writer.WriteByte((byte)Math.Min(zoneLocked.Count, byte.MaxValue));
                for (var i = 0; i < zoneLocked.Count && i < byte.MaxValue; i++)
                    NetworkWriterExtensions.WriteString(writer, zoneLocked[i]);
                var pegTiles = CustomTiles.HostTiles;
                writer.WriteByte((byte)Math.Min(pegTiles.Length, byte.MaxValue));
                for (var i = 0; i < pegTiles.Length && i < byte.MaxValue; i++)
                    NetworkWriterExtensions.WriteString(writer, pegTiles[i]);
                var tileSounds = TileSounds.HostManifest();
                writer.WriteByte((byte)tileSounds.Count);
                foreach (var pair in tileSounds)
                {
                    writer.WriteByte((byte)pair.Key);
                    NetworkWriterExtensions.WriteULong(writer, pair.Value);
                }
                var drawings = CustomTiles.HostManifest();
                writer.WriteByte((byte)Math.Min(drawings.Count, byte.MaxValue));
                var written = 0;
                foreach (var pair in drawings)
                {
                    if (written++ == byte.MaxValue)
                        break;
                    NetworkWriterExtensions.WriteString(writer, pair.Key);
                    NetworkWriterExtensions.WriteULong(writer, pair.Value);
                }

                connection.Send(writer.ToArraySegment(), 0);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ModChannel)}] Could not send a snapshot to guest #{connection.connectionId}: {ex.Message}");
            }
        }

        internal static void SendPress(NetworkConnection connection, int slot)
        {
            if (connection == null || !Announced.Contains(connection.connectionId))
                return;

            try
            {
                var writer = new NetworkWriter();
                NetworkWriterExtensions.WriteUShort(writer, MessageId);
                writer.WriteByte(KindPress);
                writer.WriteByte(ProtocolVersion);
                writer.WriteByte((byte)slot);
                connection.Send(writer.ToArraySegment(), 0);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ModChannel)}] Could not send a button press to guest #{connection.connectionId}: {ex.Message}");
            }
        }

        internal static void SendEffect(NetworkConnection connection, string key, int seconds, Vector3? destination, float yaw)
        {
            if (connection == null || !Announced.Contains(connection.connectionId))
                return;

            try
            {
                var writer = new NetworkWriter();
                NetworkWriterExtensions.WriteUShort(writer, MessageId);
                writer.WriteByte(KindEffect);
                writer.WriteByte(ProtocolVersion);
                NetworkWriterExtensions.WriteString(writer, key);
                NetworkWriterExtensions.WriteUShort(writer, (ushort)Math.Clamp(seconds, 0, ushort.MaxValue));
                NetworkWriterExtensions.WriteBool(writer, destination != null);
                var point = destination ?? Vector3.zero;
                NetworkWriterExtensions.WriteFloat(writer, point.x);
                NetworkWriterExtensions.WriteFloat(writer, point.y);
                NetworkWriterExtensions.WriteFloat(writer, point.z);
                NetworkWriterExtensions.WriteFloat(writer, yaw);
                connection.Send(writer.ToArraySegment(), 0);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ModChannel)}] Could not send {key} to guest #{connection.connectionId}: {ex.Message}");
            }
        }

        internal static bool SendStatus(bool masked)
        {
            if (!_helloSent || !_hostHasMod)
                return false;

            try
            {
                var connection = NetworkClient.connection;
                if (connection == null)
                    return false;

                var writer = new NetworkWriter();
                NetworkWriterExtensions.WriteUShort(writer, MessageId);
                writer.WriteByte(KindStatus);
                writer.WriteByte(ProtocolVersion);
                NetworkWriterExtensions.WriteBool(writer, masked);
                connection.Send(writer.ToArraySegment(), 0);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ModChannel)}] Could not tell the host about the mask: {ex.Message}");
                return false;
            }
        }

        internal static void SendMap(string text)
        {
            foreach (var connection in NetworkServer.connections.Values)
            {
                if (connection == null || !Announced.Contains(connection.connectionId))
                    continue;
                try
                {
                    var writer = new NetworkWriter();
                    NetworkWriterExtensions.WriteUShort(writer, MessageId);
                    writer.WriteByte(KindMap);
                    writer.WriteByte(ProtocolVersion);
                    NetworkWriterExtensions.WriteString(writer, text);
                    connection.Send(writer.ToArraySegment(), 0);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(ModChannel)}] Could not send the map to guest #{connection.connectionId}: {ex.Message}");
                }
            }
        }

        internal static void SendNotice(NetworkConnection connection, string text, bool warning)
        {
            if (connection == null || !Announced.Contains(connection.connectionId))
                return;

            try
            {
                var writer = new NetworkWriter();
                NetworkWriterExtensions.WriteUShort(writer, MessageId);
                writer.WriteByte(KindNotice);
                writer.WriteByte(ProtocolVersion);
                NetworkWriterExtensions.WriteString(writer, text);
                NetworkWriterExtensions.WriteBool(writer, warning);
                connection.Send(writer.ToArraySegment(), 0);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ModChannel)}] Could not send a notice to guest #{connection.connectionId}: {ex.Message}");
            }
        }

        internal static int SendSummon(Vector3 position, Quaternion rotation)
        {
            var sent = 0;
            foreach (var connection in NetworkServer.connections.Values)
            {
                if (connection == null || !Announced.Contains(connection.connectionId) || !IsFromThisMachine(connection))
                    continue;

                try
                {
                    // Several guests each a step further back, so they do not land in one another.
                    var spot = position + rotation * Vector3.back * (1.5f * sent);
                    var writer = new NetworkWriter();
                    NetworkWriterExtensions.WriteUShort(writer, MessageId);
                    writer.WriteByte(KindSummon);
                    writer.WriteByte(ProtocolVersion);
                    NetworkWriterExtensions.WriteFloat(writer, spot.x);
                    NetworkWriterExtensions.WriteFloat(writer, spot.y);
                    NetworkWriterExtensions.WriteFloat(writer, spot.z);
                    NetworkWriterExtensions.WriteFloat(writer, rotation.eulerAngles.y);
                    connection.Send(writer.ToArraySegment(), 0);
                    sent++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(ModChannel)}] Could not send the summon to guest #{connection.connectionId}: {ex.Message}");
                }
            }

            return sent;
        }

        // Connection 0 is the host's own client; others carry the remote address.
        private static bool IsFromThisMachine(NetworkConnectionToClient connection)
        {
            if (connection.connectionId == NetworkConnection.LocalConnectionId)
                return false;
            if (!System.Net.IPAddress.TryParse(connection.address, out var address))
                return false;
            if (address.IsIPv4MappedToIPv6)
                address = address.MapToIPv4();
            return System.Net.IPAddress.IsLoopback(address);
        }

        [HideFromIl2Cpp]
        private static void TickGuest()
        {
            if (!_beaconHandlerRegistered || !NetworkClient.spawnHandlers.ContainsKey(BeaconAssetId))
                RegisterBeaconHandler();

            if (!_clientHandlerRegistered || !NetworkClient.handlers.ContainsKey(MessageId))
                RegisterClientHandler();

            if (!WorldManager.isReadyForEffects)
                RadioStations.ForgetMirroredWorld();

            if (_hostHasMod && !_helloSent && _clientHandlerRegistered)
                SendHello();
        }

        private static void ResetGuest()
        {
            if (_hostHasMod || _receivedAt >= 0f)
            {
                RadioStations.ForgetMirror();
                PuzzleNeeds.ForgetMirror();
                ZoneLocks.ForgetMirror();
                AbilityLocks.ForgetMirror();
                FastGolf.ForgetMirror();
                GauntletStairways.ForgetMirror();
                TeleportButtons.ForgetMirror();
                CabinFeverWaits.ForgetMirror();
                TileThiefHelper.ForgetMirror();
                ResyncStations.ForgetMirror();
                ObjectiveBoard.ForgetMirror();
                HintBoards.ForgetMirror();
                MapTracker.ForgetMirror();
                MonumentOrder.ForgetMirror();
                PackChecks.ForgetMirror();
                Palette.ForgetMirror();
                GourdNames.ForgetMirror();
                CustomTiles.ForgetMirror();
                TileSounds.ForgetMirror();
            }

            _clientHandlerRegistered = false;
            _beaconHandlerRegistered = false;
            _hostHasMod = false;
            _helloSent = false;
            _versionMismatchLogged = false;
            _hostIsAnotherBuild = false;
            _helloAt = -1f;
            _receivedAt = -1f;
            MirroredStatus = null;
            MirroredGoal = null;
            MirroredLines.Clear();
        }

        private static void RegisterBeaconHandler()
        {
            NetworkClient.RegisterSpawnHandler(BeaconAssetId, (SpawnDelegate)BuildBeacon, (UnSpawnDelegate)DestroyBeacon);
            _beaconHandlerRegistered = true;
        }

        private static void RegisterClientHandler()
        {
            var handlers = NetworkClient.handlers;
            if (handlers == null)
                return;

            if (handlers.ContainsKey(MessageId) && !_clientHandlerRegistered)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ModChannel)}] Message id 0x{MessageId:X4} is already taken on this client; "
                    + "the host's overlay and radio will not reach this player.");
                _clientHandlerRegistered = true;
                return;
            }

            NetworkMessageDelegate handler = (Action<NetworkConnection, NetworkReader, int>)OnClientMessage;
            handlers[MessageId] = handler;
            _clientHandlerRegistered = true;
        }

        // Same rule as every spawn handler in this mod: never null.
        private static GameObject BuildBeacon(Vector3 position, uint assetId)
        {
            var beacon = new GameObject("Big Walk Archipelago beacon (AP)");
            beacon.AddComponent<NetworkIdentity>();
            NoteHostHasMod("its beacon arrived");
            return beacon;
        }

        private static void DestroyBeacon(GameObject beacon)
        {
            if (beacon != null)
                UnityEngine.Object.Destroy(beacon);
        }

        // Any asset id of ours can only come from a host running this mod.
        internal static void NoteHostHasMod(string evidence)
        {
            if (_hostHasMod || NetworkServer.active)
                return;

            _hostHasMod = true;
            Plugin.Log.LogInfo($"[{nameof(ModChannel)}] The host runs the mod ({evidence}); saying hello.");
        }

        private static void SendHello()
        {
            try
            {
                var connection = NetworkClient.connection;
                if (connection == null)
                    return;

                var writer = new NetworkWriter();
                NetworkWriterExtensions.WriteUShort(writer, MessageId);
                writer.WriteByte(KindHello);
                writer.WriteByte(ProtocolVersion);
                connection.Send(writer.ToArraySegment(), 0);
                _helloSent = true;
                _helloAt = Time.unscaledTime;
            }
            catch (Exception ex)
            {
                _helloSent = true;
                Plugin.Log.LogWarning($"[{nameof(ModChannel)}] Could not say hello to the host: {ex.Message}");
            }
        }

        // Same rule as OnServerMessage: nothing may escape it.
        private static void OnClientMessage(NetworkConnection connection, NetworkReader reader, int channelId)
        {
            try
            {
                var kind = reader.ReadByte();
                if (kind == KindSummon)
                {
                    OnSummon(reader);
                    return;
                }

                if (kind == KindPress)
                {
                    if (reader.ReadByte() == ProtocolVersion)
                        WorldButtons.RunPress(reader.ReadByte());
                    return;
                }

                if (kind == KindEffect)
                {
                    if (reader.ReadByte() != ProtocolVersion)
                        return;

                    var key = NetworkReaderExtensions.ReadString(reader);
                    var seconds = NetworkReaderExtensions.ReadUShort(reader);
                    var hasDestination = NetworkReaderExtensions.ReadBool(reader);
                    var point = new Vector3(
                        NetworkReaderExtensions.ReadFloat(reader),
                        NetworkReaderExtensions.ReadFloat(reader),
                        NetworkReaderExtensions.ReadFloat(reader));
                    var yaw = NetworkReaderExtensions.ReadFloat(reader);
                    TrapEffects.Play(key, seconds, hasDestination ? point : null, yaw);
                    return;
                }

                if (kind == KindMap)
                {
                    if (reader.ReadByte() == ProtocolVersion)
                        MapTracker.ApplyFromHost(NetworkReaderExtensions.ReadString(reader));
                    return;
                }

                if (kind == KindBlob)
                {
                    if (reader.ReadByte() != ProtocolVersion)
                        return;
                    var channel = reader.ReadByte();
                    var key = NetworkReaderExtensions.ReadString(reader);
                    var hash = NetworkReaderExtensions.ReadULong(reader);
                    var total = NetworkReaderExtensions.ReadInt(reader);
                    var offset = NetworkReaderExtensions.ReadInt(reader);
                    var length = NetworkReaderExtensions.ReadInt(reader);
                    // Bounded before reading: no chunk is larger than the host cuts them.
                    if (length <= 0 || length > BlobChannel.ChunkBytes || key == null || key.Length > BlobChannel.MaxKeyLength)
                        return;
                    var raw = NetworkReaderExtensions.ReadBytes(reader, length);
                    var data = new byte[length];
                    for (var i = 0; i < length; i++)
                        data[i] = raw[i];
                    if (channel == 0)
                        TileSounds.ReceiveChunk(key, hash, total, offset, data);
                    else if (channel == 1)
                        CustomTiles.ReceiveChunk(key, hash, total, offset, data);
                    return;
                }

                if (kind == KindNotice)
                {
                    if (reader.ReadByte() == ProtocolVersion)
                    {
                        var text = NetworkReaderExtensions.ReadString(reader);
                        ApNotices.Post(text, warning: NetworkReaderExtensions.ReadBool(reader));
                    }

                    return;
                }

                if (kind != KindSnapshot)
                    return;

                var version = reader.ReadByte();
                if (version != ProtocolVersion)
                {
                    _hostIsAnotherBuild = true;
                    if (!_versionMismatchLogged)
                    {
                        _versionMismatchLogged = true;
                        Plugin.Log.LogWarning(
                            $"[{nameof(ModChannel)}] The host speaks protocol v{version}, this build v{ProtocolVersion}; "
                            + "ignoring it. Both players need the same build of the mod.");
                    }
                    return;
                }

                var status = NetworkReaderExtensions.ReadString(reader);
                var statusWarning = NetworkReaderExtensions.ReadBool(reader);
                var goal = NetworkReaderExtensions.ReadString(reader);
                var goalWarning = NetworkReaderExtensions.ReadBool(reader);

                var lines = new List<MirroredLine>();
                var count = reader.ReadByte();
                for (var i = 0; i < count; i++)
                {
                    lines.Add(new MirroredLine
                    {
                        Text = NetworkReaderExtensions.ReadString(reader),
                        Warning = NetworkReaderExtensions.ReadBool(reader),
                    });
                }

                var radioItemsInPlay = NetworkReaderExtensions.ReadBool(reader);
                var granted = new List<SavableSystem>();
                var grantedCount = reader.ReadByte();
                for (var i = 0; i < grantedCount; i++)
                    granted.Add((SavableSystem)reader.ReadByte());

                var needsEnabled = NetworkReaderExtensions.ReadBool(reader);
                var lockedNeeds = new List<string>();
                var lockedCount = reader.ReadByte();
                for (var i = 0; i < lockedCount; i++)
                    lockedNeeds.Add(NetworkReaderExtensions.ReadString(reader));

                var gauntletLocked = NetworkReaderExtensions.ReadBool(reader);
                var gauntletPartsLocked = NetworkReaderExtensions.ReadBool(reader);
                var teleportMask = NetworkReaderExtensions.ReadUShort(reader);
                var cabinFever = NetworkReaderExtensions.ReadUShort(reader);
                var cabinFeverLong = NetworkReaderExtensions.ReadUShort(reader);
                var cabinFeverHelp = reader.ReadByte();
                var resyncStations = NetworkReaderExtensions.ReadUShort(reader);
                var gourdName = NetworkReaderExtensions.ReadString(reader);
                var packMode = reader.ReadByte();
                var objectMode = reader.ReadByte();
                var golfLightweight = NetworkReaderExtensions.ReadBool(reader);
                var objectKinds = new List<string>();
                var objectKindCount = reader.ReadByte();
                for (var i = 0; i < objectKindCount; i++)
                    objectKinds.Add(NetworkReaderExtensions.ReadString(reader));
                var collectedPacks = new List<string>();
                var collectedCount = reader.ReadByte();
                for (var i = 0; i < collectedCount; i++)
                    collectedPacks.Add(NetworkReaderExtensions.ReadString(reader));
                var colours = new List<byte>();
                var colourCount = reader.ReadByte();
                for (var i = 0; i < colourCount; i++)
                    colours.Add(reader.ReadByte());
                var boardText = NetworkReaderExtensions.ReadString(reader);
                var endingLocks = reader.ReadByte();
                var monumentMask = reader.ReadByte();
                var soundCounts = new byte[ApSounds.Counts.Length];
                for (var sound = 0; sound < soundCounts.Length; sound++)
                    soundCounts[sound] = reader.ReadByte();
                var abilitiesEnabled = NetworkReaderExtensions.ReadBool(reader);
                var abilityMask = NetworkReaderExtensions.ReadUShort(reader);
                var hintFlags = reader.ReadByte();
                var hintNames = NetworkReaderExtensions.ReadString(reader);
                var zoneLocked = new List<string>();
                var zoneLockedCount = reader.ReadByte();
                for (var i = 0; i < zoneLockedCount; i++)
                    zoneLocked.Add(NetworkReaderExtensions.ReadString(reader));
                var pegTiles = new List<string>();
                var pegTileCount = reader.ReadByte();
                for (var i = 0; i < pegTileCount; i++)
                    pegTiles.Add(NetworkReaderExtensions.ReadString(reader));
                var tileSounds = new Dictionary<int, ulong>();
                var tileSoundCount = reader.ReadByte();
                for (var i = 0; i < tileSoundCount; i++)
                {
                    var soundKind = reader.ReadByte();
                    var hash = NetworkReaderExtensions.ReadULong(reader);
                    if (soundKind < 9)
                        tileSounds[soundKind] = hash;
                }
                var drawings = new Dictionary<string, ulong>();
                var drawingCount = reader.ReadByte();
                for (var i = 0; i < drawingCount; i++)
                {
                    var key = NetworkReaderExtensions.ReadString(reader);
                    var hash = NetworkReaderExtensions.ReadULong(reader);
                    if (!string.IsNullOrEmpty(key) && key.Length <= BlobChannel.MaxKeyLength)
                        drawings[key] = hash;
                }

                var first = _receivedAt < 0f;

                MirroredStatus = string.IsNullOrEmpty(status) ? null : status;
                MirroredStatusIsWarning = statusWarning;
                MirroredGoal = string.IsNullOrEmpty(goal) ? null : goal;
                MirroredGoalIsWarning = goalWarning;

                // A guest hears what the host hears, except on the first snapshot.
                if (!first)
                {
                    for (var sound = 0; sound < soundCounts.Length; sound++)
                    {
                        if (soundCounts[sound] != _heardCounts[sound])
                            ApSounds.PlayForGuest(sound);
                    }
                }

                System.Array.Copy(soundCounts, _heardCounts, soundCounts.Length);

                MirroredLines.Clear();
                MirroredLines.AddRange(lines);
                _receivedAt = Time.unscaledTime;

                if (first)
                    Plugin.Log.LogInfo(
                        $"[{nameof(ModChannel)}] First word from the host: '{MirroredStatus}'. Showing the overlay here too.");

                RadioStations.ApplyFromHost(radioItemsInPlay, granted);
                PuzzleNeeds.ApplyFromHost(needsEnabled, lockedNeeds);
                GauntletStairways.ApplyFromHost(gauntletLocked, gauntletPartsLocked);
                TeleportButtons.ApplyFromHost(teleportMask);
                CabinFeverWaits.ApplyFromHost(cabinFever, cabinFeverLong, (cabinFeverHelp & 1) != 0, (cabinFeverHelp & 2) != 0,
                    (cabinFeverHelp & 16) != 0, (cabinFeverHelp & 32) != 0);
                TileThiefHelper.ApplyFromHost((cabinFeverHelp >> 2) & 3);
                ResyncStations.ApplyFromHost(resyncStations);
                PackChecks.ApplyFromHost(packMode, objectMode, objectKinds, collectedPacks);
                FastGolf.ApplyFromHost(golfLightweight);
                Palette.ApplyFromHost(colours);
                GourdNames.ApplyFromHost(gourdName);
                ObjectiveBoard.ApplyFromHost(boardText, endingLocks);
                MonumentOrder.ApplyFromHost(monumentMask);
                AbilityLocks.ApplyFromHost(abilitiesEnabled, abilityMask);
                HintBoards.ApplyFromHost(hintFlags, hintNames);
                ZoneLocks.ApplyFromHost(zoneLocked);
                CustomTiles.ApplyFromHost(pegTiles);
                CustomTiles.ApplyDrawingsFromHost(drawings);
                TileSounds.ApplyManifestFromHost(tileSounds);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ModChannel)}] Unreadable snapshot from the host, ignored: {ex.Message}");
            }
        }

        // Called from OnClientMessage's try, so nothing escapes it either.
        private static void OnSummon(NetworkReader reader)
        {
            if (reader.ReadByte() != ProtocolVersion)
                return;

            var position = new Vector3(
                NetworkReaderExtensions.ReadFloat(reader),
                NetworkReaderExtensions.ReadFloat(reader),
                NetworkReaderExtensions.ReadFloat(reader));
            var yaw = NetworkReaderExtensions.ReadFloat(reader);

#if DEBUG
            BigWalkArchipelago.Debug.LoopbackGuest.OnSummoned(position, Quaternion.Euler(0f, yaw, 0f));
#endif
        }
    }
}
