using UnityEngine;

namespace BigWalkArchipelago.Core.Hub
{
    // Turns the Map Room's big map into a tracker; the host sends each check's state to guests.
    internal static partial class MapTracker
    {
        private const string Tag = "[" + nameof(MapTracker) + "]";
        private const float TickSeconds = 1f;

        private static readonly Color Grey = new(0.55f, 0.55f, 0.55f, 1f);
        private const float GreyAmount = 0.8f;

        private static GourdMap _map;
        private static float _tickAt;

        private static readonly Dictionary<int, Color[]> Original = new();
        private static readonly Dictionary<int, PropertyBlockHelper> Greyed = new();
        private static readonly Dictionary<int, GourdFlag> HiddenFlags = new();
        private static int _loggedBuilt;

        private sealed class Marker
        {
            internal GameObject Root;
            internal readonly List<(Renderer renderer, MaterialPropertyBlock own, MaterialPropertyBlock grey)> Parts = new();
            internal bool Greyed;
            internal Renderer Stick;
            internal bool Hinted;
        }

        // Hinted checks get a glowing gold stick.
        private static readonly Color Gold = new(1.6f, 1.2f, 0.25f, 1f);
        private static readonly Color Glow = new(2.2f, 2.2f, 2f, 1f);
        private static readonly HashSet<Renderer> Glowing = new();
        private static readonly Dictionary<int, GameObject> FlagSticks = new();

        // Built in the keys' order, so a spot's spread is the same on every machine.
        private static readonly Dictionary<string, Marker> Markers = new();
        private static readonly HashSet<string> Built = new();

        private const byte Visible = 1, GreyBit = 2, HintBit = 4, OpenBit = 16;

        private static readonly List<(string key, long id)> HostKeys = new();
        private static bool _hostKeysBuilt;
        private static Dictionary<string, byte> _states = new();
        private static Dictionary<string, byte> _mirrored;
        private static string _sent;
        private static float _sentAt;
        private const float ResendSeconds = 10f;

        private static bool IsGuest => Mirror.NetworkClient.active && !Mirror.NetworkServer.active;

        // Map units are metres; a gourd's flag stands about 0.2 above the relief.
        private const float ModelSize = 0.13f;
        private const float StickHeight = 0.2f;
        private const float Neighbour = 0.22f;
        private const float StackStep = 0.03f;
        private const float SpreadStep = 0.09f;
        private static readonly Color StickColour = new(0.25f, 0.35f, 0.75f);
        private static readonly Color GreyTint = new(0.35f, 0.35f, 0.35f, 1f);

        private static bool On => IsGuest
            ? _mirrored != null
            : ApRuntime.SlotData != null && ApRuntime.SlotData.MapTracker && TrackerState.Ready;

        internal static void ApplyFromHost(string text)
        {
            var states = new Dictionary<string, byte>();
            foreach (var line in (text ?? string.Empty).Split('\n'))
            {
                var tab = line.LastIndexOf('\t');
                if (tab > 0 && byte.TryParse(line.Substring(tab + 1), out var state))
                    states[line.Substring(0, tab)] = state;
            }
            _mirrored = states.Count > 0 ? states : null;
        }

        internal static void ForgetMirror() => _mirrored = null;

        internal static void Tick()
        {
            PulseHints();
            PulseRainbow();
            if (Time.unscaledTime < _tickAt)
                return;
            _tickAt = Time.unscaledTime + TickSeconds;

            try
            {
                if (!WorldManager.isReadyForEffects)
                {
                    _map = null;
                    Original.Clear();
                    Greyed.Clear();
                    HiddenFlags.Clear();
                    Markers.Clear();
                    Built.Clear();
                    Glowing.Clear();
                    Rainbow.Clear();
                    FlagSticks.Clear();
                    _placed.Clear();
                    _scene = null;
                    return;
                }

                if (!IsGuest)
                    TrackerState.Tick();
                if (!On)
                {
                    RestoreAll();
                    return;
                }

                if (_map == null)
                    _map = FindMap();
                if (_map == null)
                    return;

                if (!IsGuest)
                {
                    _states = HostStates();
                    SendToGuests();
                }
                var states = IsGuest ? _mirrored : _states;

                var keys = new List<string>(states.Keys);
                keys.Sort(StringComparer.Ordinal);
                foreach (var key in keys)
                {
                    if (!key.StartsWith("puzzle:", StringComparison.Ordinal) && Built.Add(key))
                        Build(key);
                }
                if (Built.Count != _loggedBuilt)
                {
                    _loggedBuilt = Built.Count;
                    Plugin.Log.LogInfo($"{Tag} {Markers.Count} marker(s) on the map for {Built.Count} key(s){(IsGuest ? " (from the host)" : string.Empty)}.");
                }
                foreach (var pair in Markers)
                    ApplyMarker(pair.Key, pair.Value, states.TryGetValue(pair.Key, out var state) ? state : (byte)0);
                ApplyFlags(states);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} {ex.Message}");
            }
        }

        private static GourdMap FindMap()
        {
            foreach (var map in UnityEngine.Object.FindObjectsByType<GourdMap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (map != null)
                    return map;
            }
            return null;
        }
    }
}
