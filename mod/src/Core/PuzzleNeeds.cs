using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;

namespace BigWalkArchipelago.Core
{
    // The `lock_puzzle_needs` side: a need is map-wide, its objects hidden until received.
    internal static class PuzzleNeeds
    {
        internal const string Buttons = "buttons";
        internal const string SyncButtons = "sync_buttons";
        internal const string IconPanels = "icon_panels";
        internal const string DrawingPanels = "drawing_panels";
        internal const string PosePanels = "pose_panels";
        internal const string SoundPanels = "sound_panels";
        internal const string PointPanels = "point_panels";
        internal const string Lights = "lights";
        internal const string Speakers = "speakers";
        internal const string Teapots = "teapots";
        internal const string Counter = "counter";
        internal const string CoordinatesComputer = "coordinates_computer";
        internal const string Eggs = "eggs";
        internal const string InkViewer = "ink_viewer";
        internal const string BigHead = "big_head";
        internal const string GolfBall = "golf_ball";
        internal const string TimedTomato = "timed_tomato";

        // The needs PuzzleNeedHider knows the objects of.
        internal static readonly string[] All =
        {
            Buttons, SyncButtons, IconPanels, DrawingPanels, PosePanels, SoundPanels, PointPanels, Lights,
            Speakers, Teapots, Counter, CoordinatesComputer, Eggs, InkViewer, BigHead, GolfBall, TimedTomato,
        };

        private const string KeyPrefix = "ap_need_";

        private static readonly HashSet<string> Locked = new HashSet<string>();

        // In item id order: an item's id is the base plus the offset plus its position here.
        private static string[] _keys = Array.Empty<string>();
        private static long _itemIdBase;
        private static bool _mirrored;

        // Bumped on every change, so the hider can tell without comparing sets.
        internal static int Version { get; private set; }

        internal static bool Enabled { get; private set; }

        // The host reads it from slot_data, a guest is told by its host.
        internal static bool Active => Enabled || _mirrored;

        internal static bool IsLocked(string need) => Locked.Contains(need);

        internal static List<string> LockedNeeds() => Locked.ToList();

        internal static void SetLocked(string need, bool locked)
        {
            var changed = locked ? Locked.Add(need) : Locked.Remove(need);
            if (changed)
                Version++;
        }

        // Host: every need the ledger has not granted is locked.
        internal static void Configure(bool enabled, string[] keysInIdOrder, long itemIdBase)
        {
            Enabled = enabled;
            _keys = keysInIdOrder ?? Array.Empty<string>();
            _itemIdBase = itemIdBase;

            Locked.Clear();
            if (enabled)
            {
                foreach (var key in _keys)
                {
                    if (!All.Contains(key))
                        Plugin.Log.LogWarning($"[{nameof(PuzzleNeeds)}] '{key}' is a need this build does not know; ignored.");
                    else if (!IsGranted(key))
                        Locked.Add(key);
                }
            }

            Version++;
            Plugin.Log.LogInfo(
                $"[{nameof(PuzzleNeeds)}] lock_puzzle_needs {(enabled ? "on" : "off")}; "
                + (Locked.Count == 0 ? "nothing locked." : $"locked until their item arrives: {string.Join(", ", Locked)}."));
        }

        // A world reloaded on the same connection: the ledger decides, not the last hand toggles.
        internal static void Rearm()
        {
            if (!Enabled)
                return;

            Locked.Clear();
            foreach (var key in _keys)
            {
                if (All.Contains(key) && !IsGranted(key))
                    Locked.Add(key);
            }

            Version++;
        }

        // The item id is the apworld's: base, then the need's position.
        internal static bool TryResolveItem(long itemId, out string need)
        {
            need = null;
            var index = itemId - _itemIdBase;
            if (!Enabled || index < 0 || index >= _keys.Length)
                return false;

            need = _keys[index];
            return true;
        }

        internal static bool Grant(string need)
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogWarning($"[{nameof(PuzzleNeeds)}] Ignored: {need} received while not host.");
                return false;
            }

            SaveManager.SetIntValue(KeyPrefix + need, 1);
            SetLocked(need, false);
            Plugin.Log.LogInfo($"[{nameof(PuzzleNeeds)}] {PuzzleNeedHider.Stamp()} {need} granted.");
            SessionJournal.Write("need", $"{need} granted");
            return true;
        }

        private static bool IsGranted(string need) => SaveManager.GetIntValue(KeyPrefix + need, 0, false) != 0;

        // A save rebound to another seed replays every item, so the old grants go.
        internal static void ClearLedger()
        {
            // Only what was granted, so a save that never used the option gets no keys.
            foreach (var key in All)
            {
                if (IsGranted(key))
                    SaveManager.SetIntValue(KeyPrefix + key, 0);
            }
        }

        // The world went away: its slot's list and any hand locks go with it.
        internal static void Forget()
        {
            Enabled = false;
            _keys = Array.Empty<string>();
            _mirrored = false;
            if (Locked.Count == 0)
                return;

            Locked.Clear();
            Version++;
        }

        // Guest: the host's snapshot says which needs are locked right now.
        internal static void ApplyFromHost(bool enabled, List<string> locked)
        {
            if (!enabled && !_mirrored)
                return;

            _mirrored = enabled;
            if (Locked.SetEquals(locked))
                return;

            Locked.Clear();
            foreach (var need in locked)
                Locked.Add(need);

            Version++;
            Plugin.Log.LogInfo(
                $"[{nameof(PuzzleNeeds)}] {PuzzleNeedHider.Stamp()} The host says locked: "
                + $"{(Locked.Count == 0 ? "nothing" : string.Join(", ", Locked))}.");
        }

        // The guest left the host: whatever it was told stops being true.
        internal static void ForgetMirror()
        {
            if (!_mirrored)
                return;

            Forget();
        }
    }
}
