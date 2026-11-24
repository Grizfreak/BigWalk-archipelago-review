using Il2CppInterop.Runtime.Attributes;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    internal partial class ModChannel
    {
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
    }
}
