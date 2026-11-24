using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    // Raw Mirror messages from the host; an unknown id disconnects a peer, hence the beacon.
    internal partial class ModChannel : MonoBehaviour
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
    }
}
