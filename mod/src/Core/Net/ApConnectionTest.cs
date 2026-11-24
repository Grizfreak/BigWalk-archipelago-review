using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;

namespace BigWalkArchipelago.Core.Net
{
    // A throwaway login from the hosting screen, closed at once; runs on a Task, Status is polled.
    internal static class ApConnectionTest
    {
        internal enum TestStatus
        {
            Idle,
            Testing,
            Ok,
            Failed,
        }

        // Past this, the probe has failed and the player can host anyway.
        private const double TimeoutSeconds = 15;

        private static volatile TestStatus _status = TestStatus.Idle;
        private static DateTime _startedAt;

        internal static string LastError { get; private set; } = string.Empty;

        // The games of the room that answered: a wrong port often reaches someone else's live room.
        internal static string[] LastRoomGames { get; private set; }

        internal static TestStatus Status
        {
            get
            {
                if (_status == TestStatus.Testing && (DateTime.UtcNow - _startedAt).TotalSeconds > TimeoutSeconds)
                {
                    LastError = $"no answer after {TimeoutSeconds:0}s";
                    _status = TestStatus.Failed;
                }

                return _status;
            }
        }

        internal static void Reset()
        {
            _status = TestStatus.Idle;
            LastError = string.Empty;
        }

        internal static void Start(ApEndpoint endpoint)
        {
            if (_status == TestStatus.Testing)
                return;

            _status = TestStatus.Testing;
            _startedAt = DateTime.UtcNow;
            LastError = string.Empty;

            Task.Run(() => Probe(endpoint));
        }

        private static void Probe(ApEndpoint endpoint)
        {
            try
            {
                var session = ArchipelagoSessionFactory.CreateSession(endpoint.Host, endpoint.Port);
                LastRoomGames = null;
                session.Socket.PacketReceived += packet =>
                {
                    if (packet is Archipelago.MultiClient.Net.Packets.RoomInfoPacket info)
                        LastRoomGames = info.Games;
                };

                // NoItems and no slot_data: this throwaway login must not consume the item stream.
                var result = session.TryConnectAndLogin(
                    "Big Walk",
                    endpoint.SlotName,
                    ItemsHandlingFlags.NoItems,
                    new Version(0, 6, 0),
                    Array.Empty<string>(),
                    endpoint.Uuid + "-test",
                    endpoint.Password,
                    false);

                try
                {
                    session.Socket.DisconnectAsync();
                }
                catch (Exception ex)
                {
                    // Best effort; the server drops it on its own soon enough.
                    Plugin.Log.LogInfo($"[{nameof(ApConnectionTest)}] Probe cleanup: {ex.Message}");
                }

                if (result.Successful)
                {
                    _status = TestStatus.Ok;
                    return;
                }

                LastError = result is LoginFailure failure && failure.Errors is { Length: > 0 }
                    ? string.Join(" / ", failure.Errors)
                    : "login refused";
                _status = TestStatus.Failed;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                _status = TestStatus.Failed;
            }
        }
    }
}
