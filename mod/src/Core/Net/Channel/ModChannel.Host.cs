using Il2CppInterop.Runtime.Attributes;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    internal partial class ModChannel
    {
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
    }
}
