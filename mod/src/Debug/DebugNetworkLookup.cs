using System;
using kcp2k;
using Mirror;
using Mirror.Authenticators;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Read-only dump of this process's Mirror setup, for the loopback guest.
    internal static class DebugNetworkLookup
    {
        private const string Tag = "[" + nameof(DebugNetworkLookup) + "]";

        internal static void Dump()
        {
            Plugin.Log.LogInfo($"{Tag} === network dump (process {Environment.ProcessId}) ===");

            // Each section on its own try: a throwing getter must not hide the lines after it.
            Section("process", DumpProcess);
            Section("manager", DumpManager);
            Section("connections", DumpConnections);
            Section("player names", DumpPlayerNames);
            Section("input", DumpInput);
            Section("prefabs", DumpPrefabs);

            Plugin.Log.LogInfo($"{Tag} === end of network dump ===");
        }

        private static void Section(string name, Action dump)
        {
            try
            {
                dump();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} The {name} section threw: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static void DumpProcess()
        {
            Plugin.Log.LogInfo($"{Tag} Loopback role: {LoopbackGuest.RoleName}");
            Plugin.Log.LogInfo($"{Tag} Command line: {string.Join(" ", Environment.GetCommandLineArgs())}");
            Plugin.Log.LogInfo(
                $"{Tag} Application.runInBackground={Application.runInBackground}, isFocused={Application.isFocused}");
            Plugin.Log.LogInfo($"{Tag} SteamManager.Initialized={SteamManager.Initialized}");

            // When this fails, the game sends SystemInfo.deviceName instead.
            var found = HouseSteamManager.TryGetLocalUserIdentifier(out var identifier);
            Plugin.Log.LogInfo(
                $"{Tag} Player identifier this machine would send a host: "
                + (found ? Mask(identifier) : $"<none; the game falls back to the machine name, {Mask(SystemInfo.deviceName)}>"));
        }

        // On a loopback guest `username` reads "127.0.0.1-1", so all the game's names are logged.
        private static void DumpPlayerNames()
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null)
            {
                Plugin.Log.LogInfo($"{Tag} No players.");
                return;
            }

            foreach (var player in players)
            {
                if (player == null)
                    continue;
                var networking = player.GetComponentInChildren<PlayerNetworking>();
                Plugin.Log.LogInfo(
                    $"{Tag} Player object '{player.gameObject.name}': "
                    + (networking == null
                        ? "no PlayerNetworking"
                        : $"username='{networking.username}', moderationName='{networking.moderationName}', "
                          + $"sanitized='{networking.moderationNameSanitized}', platform={networking.platform}, "
                          + $"warning label='{LabelOf(player)}'"));
            }
        }

        private static string LabelOf(PlayerCharacter player)
        {
            foreach (var connection in Mirror.NetworkServer.connections.Values)
            {
                if (connection?.identity != null && connection.identity.gameObject == player.gameObject)
                    return BigWalkArchipelago.Core.Net.ModChannel.PlayerLabel(connection.connectionId);
            }
            return "?";
        }

        private static void DumpManager()
        {
            var manager = NetworkManager.singleton;
            if (manager == null)
            {
                Plugin.Log.LogInfo($"{Tag} NetworkManager.singleton is null.");
                return;
            }

            Plugin.Log.LogInfo(
                $"{Tag} NetworkManager.singleton: {manager.GetIl2CppType().FullName} on '{manager.gameObject.name}', "
                + $"mode={manager.mode}, isNetworkActive={manager.isNetworkActive}, "
                + $"networkAddress='{manager.networkAddress}', maxConnections={manager.maxConnections}, "
                + $"runInBackground={manager.runInBackground}");

            var authenticator = manager.authenticator;
            if (authenticator == null)
            {
                Plugin.Log.LogInfo($"{Tag} Authenticator: <none>");
            }
            else
            {
                // Whether a password is set, never the password itself.
                var house = authenticator.TryCast<HouseAuthenticator>();
                var password = house != null ? $", password set={!string.IsNullOrEmpty(house.password)}" : "";
                Plugin.Log.LogInfo($"{Tag} Authenticator: {authenticator.GetIl2CppType().FullName}{password}");
            }

            Plugin.Log.LogInfo($"{Tag} Transport.active: {Describe(Transport.active)}");
            Plugin.Log.LogInfo($"{Tag} NetworkManager.transport: {Describe(manager.transport)}");

            // Inactive ones too: what the game could switch to, not only what it uses.
            var transports = manager.GetComponentsInChildren<Transport>(true);
            Plugin.Log.LogInfo($"{Tag} {transports.Length} Transport component(s) under '{manager.gameObject.name}':");
            foreach (var transport in transports)
                Plugin.Log.LogInfo($"{Tag}   {Describe(transport)}");
        }

        private static void DumpConnections()
        {
            Plugin.Log.LogInfo(
                $"{Tag} NetworkServer.active={NetworkServer.active}, NetworkClient.active={NetworkClient.active}, "
                + $"NetworkClient.isConnected={NetworkClient.isConnected}");

            if (!NetworkServer.active)
                return;

            Plugin.Log.LogInfo($"{Tag} {NetworkServer.connections.Count} server connection(s):");
            foreach (var connection in NetworkServer.connections.Values)
            {
                if (connection == null)
                    continue;

                // The netId, not the object's name: the name holds the full SteamID.
                var player = connection.identity != null ? $"netId {connection.identity.netId}" : "<no player yet>";
                Plugin.Log.LogInfo(
                    $"{Tag}   #{connection.connectionId} address='{Safe(() => connection.address)}' "
                    + $"authenticated={connection.isAuthenticated} ready={connection.isReady} player={player} "
                    + $"identifier={Safe(() => Identifier(connection))}");
            }
        }

        // The game reads input through Rewired; there is no Unity Input System in the build.
        private static void DumpInput()
        {
            if (!Rewired.ReInput.isReady)
            {
                Plugin.Log.LogInfo($"{Tag} Rewired is not ready.");
                return;
            }

            Plugin.Log.LogInfo(
                $"{Tag} Rewired: ignoreInputWhenAppNotInFocus={Rewired.ReInput.configuration.ignoreInputWhenAppNotInFocus}, "
                + $"joysticks connected={Rewired.ReInput.controllers.joystickCount}, "
                + $"players={Rewired.ReInput.players.playerCount}");

            for (var i = 0; i < Rewired.ReInput.players.playerCount; i++)
            {
                var player = Rewired.ReInput.players.GetPlayer(i);
                if (player == null)
                    continue;
                Plugin.Log.LogInfo(
                    $"{Tag}   player {player.id} '{player.name}' playing={player.isPlaying} "
                    + $"keyboard={player.controllers.hasKeyboard} mouse={player.controllers.hasMouse} "
                    + $"joysticks={player.controllers.joystickCount}");
            }
        }

        // A vanilla client can only build the prefabs in NetworkManager.spawnPrefabs.
        private static void DumpPrefabs()
        {
            var manager = NetworkManager.singleton;
            if (manager == null)
            {
                Plugin.Log.LogInfo($"{Tag} No NetworkManager, no spawnPrefabs.");
                return;
            }

            var player = manager.playerPrefab;
            Plugin.Log.LogInfo($"{Tag} playerPrefab: {(player != null ? player.name : "<none>")}");

            var prefabs = manager.spawnPrefabs;
            var count = prefabs != null ? prefabs.Count : 0;
            Plugin.Log.LogInfo($"{Tag} {count} spawnPrefab(s), what a client without the mod can build:");
            for (var i = 0; i < count; i++)
            {
                var prefab = prefabs[i];
                if (prefab == null)
                {
                    Plugin.Log.LogInfo($"{Tag}   [{i}] <null>");
                    continue;
                }

                var identity = prefab.GetComponent<NetworkIdentity>();
                var kinds = new System.Collections.Generic.List<string>();
                if (prefab.GetComponent<RewardGourd>() != null) kinds.Add("RewardGourd");
                if (prefab.GetComponent<Prop>() != null) kinds.Add("Prop");
                if (prefab.GetComponentInChildren<PropHome>(true) != null) kinds.Add("has a PropHome");
                if (prefab.GetComponent<PlayerCharacter>() != null) kinds.Add("PlayerCharacter");

                Plugin.Log.LogInfo(
                    $"{Tag}   [{i}] '{prefab.name}' assetId={(identity != null ? identity.assetId.ToString() : "<no NetworkIdentity>")}"
                    + (kinds.Count > 0 ? $" ({string.Join(", ", kinds)})" : ""));
            }
        }

        private static bool _prefabsDumped;

        // The list lives on the manager and needs no world, so it is logged at startup.
        internal static void DumpPrefabsOnce()
        {
            if (_prefabsDumped || NetworkManager.singleton == null)
                return;

            _prefabsDumped = true;
            Plugin.Log.LogInfo($"{Tag} === spawnPrefabs (once per launch) ===");
            Section("prefabs", DumpPrefabs);
        }

        // What Corpse.FindMatch is keyed on when a player comes back.
        private static string Identifier(NetworkConnection connection)
        {
            var data = connection.authenticationData;
            if (data == null)
                return "<none>";

            // Since a game patch, the host keeps what it checked, not the request.
            var credentials = data.TryCast<HouseAuthenticator.AuthedCredentials>();
            if (credentials != null)
                return $"{Mask(credentials.playerIdentifier)} ({credentials.platform}, platform id {Mask(credentials.platformUserId)})";

            var request = data.TryCast<HouseAuthenticator.InitialialAuthRequestMessage>();
            return request != null
                ? $"{Mask(request.platformUserId)} ({request.platform})"
                : $"<{data.GetIl2CppType().FullName}>";
        }

        private static string Describe(Transport transport)
        {
            if (transport == null)
                return "<null>";

            var line =
                $"{transport.GetIl2CppType().FullName} on '{transport.gameObject.name}' enabled={transport.enabled} "
                + $"available={Safe(() => transport.Available())} serverActive={Safe(() => transport.ServerActive())} "
                + $"clientConnected={Safe(() => transport.ClientConnected())}";

            var kcp = transport.TryCast<KcpTransport>();
            if (kcp != null)
            {
                line += $" | port={kcp.port} dualMode={kcp.DualMode} noDelay={kcp.NoDelay} interval={kcp.Interval}ms "
                    + $"timeout={kcp.Timeout}ms maxRetransmit={kcp.MaxRetransmit} debugLog={kcp.debugLog}";
            }

            var multiplex = transport.TryCast<MultiplexTransport>();
            if (multiplex != null)
            {
                var inner = multiplex.transports;
                var names = new string[inner != null ? inner.Length : 0];
                for (var i = 0; i < names.Length; i++)
                    names[i] = inner[i] != null ? inner[i].GetIl2CppType().Name : "<null>";
                line += $" | transports=[{string.Join(", ", names)}]";
            }

            return line;
        }

        private static string Safe<T>(Func<T> read)
        {
            try
            {
                return read()?.ToString() ?? "<null>";
            }
            catch (Exception ex)
            {
                return $"<threw {ex.GetType().Name}>";
            }
        }

        // Enough to tell two identifiers apart in a pasted log, not enough to be one.
        private static string Mask(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return "<empty>";
            return identifier.Length <= 4
                ? $"'{identifier}'"
                : $"'...{identifier.Substring(identifier.Length - 4)}' ({identifier.Length} chars)";
        }
    }
}
