using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    internal static partial class Traps
    {
        // Effects that last, so a player who joins while one runs gets it for the time left.
        private static readonly string[] Lasting = { TrapEffects.Speed, TrapEffects.Jump, TrapEffects.Sleep, TrapEffects.Slide, TrapEffects.Silence };
        private static readonly Dictionary<string, float> ActiveUntil = new();

        // From the ModChannel, when a guest says hello.
        internal static void OnGuestJoined(NetworkConnection connection)
        {
            foreach (var pair in ActiveUntil)
            {
                var left = Mathf.CeilToInt(pair.Value - Time.unscaledTime);
                if (left < 1)
                    continue;

                Plugin.Log.LogInfo($"{Tag} Guest #{connection.connectionId} joins during {TrapEffects.DisplayName(pair.Key)}: {left}s of it.");
                ModChannel.SendEffect(connection, pair.Key, left, null, 0f);
            }
        }

        private sealed class Walker
        {
            internal PlayerCharacter Player;
            internal NetworkConnectionToClient Connection;
            internal bool Local;
            internal Vector3 Position;
            internal float Yaw;
        }

        // Every player with the connection it plays through; the host's own is connection 0.
        private static List<Walker> Walkers()
        {
            var all = AllWalkers();
            if (_onlyPlayer != null)
                all.RemoveAll(walker => walker.Player != _onlyPlayer);
            return all;
        }

        private static List<Walker> AllWalkers()
        {
            var walkers = new List<Walker>();
            foreach (var connection in NetworkServer.connections.Values)
            {
                var identity = connection?.identity;
                var player = identity != null ? identity.GetComponent<PlayerCharacter>() : null;
                if (player == null)
                    continue;

                walkers.Add(new Walker
                {
                    Player = player,
                    Connection = connection,
                    Local = connection.connectionId == NetworkConnection.LocalConnectionId,
                    Position = player.transform.position,
                    Yaw = player.transform.eulerAngles.y,
                });
            }

            return walkers;
        }

        private static void Send(Walker walker, string key, Vector3? destination, float yaw)
        {
            if (destination != null && (key == TrapEffects.Trip || key == TrapEffects.Meeting))
                GiveTomato(walker);

            if (walker.Local)
                TrapEffects.Play(key, _duration, destination, yaw);
            else
                ModChannel.SendEffect(walker.Connection, key, _duration, destination, yaw);
        }

        // Guests say whether a puzzle's mask is on their screen (ModChannel, KindStatus).
        private static readonly Dictionary<int, bool> GuestMasked = new();

        internal static void NoteGuestMasked(int connectionId, bool masked)
        {
            if (GuestMasked.TryGetValue(connectionId, out var before) && before == masked)
                return;

            GuestMasked[connectionId] = masked;
            Plugin.Log.LogInfo($"{Tag} Guest #{connectionId}: screen mask {(masked ? "on" : "off")}.");
        }

        private static bool IsMasked(Walker walker)
        {
            return walker.Local
                ? TrapEffects.LocalMasked
                : GuestMasked.TryGetValue(walker.Connection.connectionId, out var masked) && masked;
        }

        // Whether Big Trip and Big Meeting leave this player where they are.
        private static bool IsSpared(Walker walker, out string why)
        {
            why = null;
            if (IsMasked(walker))
                why = "a puzzle's mask is on their screen";
            else if (_spareGauntlet && Gauntlet.Contains(walker.Position))
                why = "in the Silent Gauntlet";
            return why != null;
        }
    }
}
