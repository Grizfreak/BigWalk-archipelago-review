using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    internal partial class ModChannel
    {
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
    }
}
