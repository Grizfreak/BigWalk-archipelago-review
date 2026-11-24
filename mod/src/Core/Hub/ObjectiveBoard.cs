using Mirror;
using TMPro;
using UnityEngine;

namespace BigWalkArchipelago.Core.Hub
{
    // The seed's objectives on a hub board; also decides which endings `lock_endings` keeps shut.
    internal static class ObjectiveBoard
    {
        private const string Tag = "[" + nameof(ObjectiveBoard) + "]";

        // The hub's resync sign: the board goes above it, on the same wall.
        private static readonly Vector3 SignPoint = new Vector3(-212.09f, 33.53f, -526.02f);
        private static readonly Vector3 Normal = new Vector3(0.66f, 0f, 0.75f).normalized;
        // Wide enough that lines do not wrap, shifted onto the red pillar right of the door.
        private const float BoardWidth = 3.0f;
        private const float ShiftAlongWall = 0.5f;
        private static readonly Vector3 AlongWall = new Vector3(-Normal.z, 0f, Normal.x);
        private const float BoardDepth = 0.1f;
        private static Vector3 _size;
        private static int _lines;
        // The board grows upward from above the sign, so it never covers the sign or its button.
        private const float BottomGap = 0.6f;

        private static GameObject _board;
        private static TextMeshPro _text;
        private static string _shown;

        // On a guest: what the host last said.
        private static string _mirroredText;
        private static byte _mirroredLocks;

        private static bool IsGuest => NetworkClient.active && !NetworkServer.active;

        // The text of the board, as the host knows it ("" when the seed lists no objectives).
        internal static string HostText()
        {
            var monuments = MonumentOrder.BoardText();
            if (!ApObjectives.Active)
                return monuments;

            var text = $"<b>OBJECTIVES  {ApObjectives.DoneCount}/{ApObjectives.Keys.Count}</b>";
            foreach (var key in ApObjectives.Keys)
            {
                // Done: ticked, dark and struck through; the ending in bold.
                var done = ApObjectives.IsDone(key);
                var label = ApObjectives.Label(key);
                if (done)
                    label = $"<color=#2a2824><s>{label}</s></color>";
                if (ApObjectives.IsEnding(key))
                    label = $"<b>{label}</b>  <size=70%>(ending)</size>";
                var line = $"{(done ? "[x]" : "[  ]")} {label}";
                text += "\n" + line;
            }

            return monuments.Length > 0 ? text + "\n\n" + monuments : text;
        }

        // Bit 0: Big Goodbye's door shut, bit 1: Big Game's dispenser, bit 2: Big Wall's bell.
        internal static byte HostLocks() =>
            (byte)((IsShut("big_goodbye") ? 1 : 0) | (IsShut("big_game") ? 2 : 0) | (IsShut("big_wall") ? 4 : 0));

        private static bool IsShut(string key) =>
            ApObjectives.Active && (ApObjectives.DebugKeys != null || ApRuntime.SlotData != null && ApRuntime.SlotData.LockEndings)
            && Array.IndexOf(ApObjectives.Keys.ToArray(), key) >= 0 && !ApObjectives.OthersDone(key);

        // Asked on every game state change, so it stays cheap. A guest follows what the host said.
        internal static bool HoldsTheLastStairway =>
            IsGuest
                ? _mirroredLocks != 0
                : ApObjectives.Active && (ApObjectives.DebugKeys != null || ApRuntime.SlotData != null && ApRuntime.SlotData.LockEndings);

        private static float _saidAt = -100f;

        // A line on the screen when an ending's entry refuses, at most one every few seconds.
        internal static void SayEndingIsShut(string key)
        {
            if (Time.unscaledTime - _saidAt < 6f)
                return;

            _saidAt = Time.unscaledTime;
            ApNotices.Post(
                key == "big_goodbye" ? "Big Goodbye: the Gauntlet's last door waits for the other objectives"
                : key == "big_wall" ? "Big Wall: the bell waits for the other objectives"
                : "Big Game: the printer waits for the other objectives",
                warning: true);
        }

        internal static bool Locked(string key)
        {
            if (IsGuest)
            {
                var bit = key == "big_goodbye" ? 1 : key == "big_game" ? 2 : key == "big_wall" ? 4 : 0;
                return bit != 0 && (_mirroredLocks & bit) != 0;
            }

            return IsShut(key);
        }

        internal static void ApplyFromHost(string text, byte locks)
        {
            _mirroredText = string.IsNullOrEmpty(text) ? null : text;
            _mirroredLocks = locks;
        }

        internal static void ForgetMirror()
        {
            _mirroredText = null;
            _mirroredLocks = 0;
        }

        private static readonly System.Collections.Generic.HashSet<string> Announced = new();
        private static bool _announcedOnce;
        private static string _signature = string.Empty;

        // Announces a newly done objective on the host; what was done before the first look is not.
        private static void Announce()
        {
            if (IsGuest || !ApObjectives.Active)
            {
                Announced.Clear();
                _announcedOnce = false;
                return;
            }

            // A new list is looked at in silence: what it already holds as done is not news.
            var signature = string.Join(",", ApObjectives.Keys);
            if (signature != _signature)
            {
                _signature = signature;
                _announcedOnce = false;
                Announced.Clear();
            }

            foreach (var key in ApObjectives.Keys)
            {
                // An objective forgotten (the debug key) is announced again when done again.
                if (!ApObjectives.IsDone(key))
                {
                    Announced.Remove(key);
                    continue;
                }

                if (!Announced.Add(key) || !_announcedOnce)
                    continue;

                Plugin.Log.LogInfo($"[{nameof(ObjectiveBoard)}] Objective done: {key}.");
                ApNotices.Post($"Objective done: {ApObjectives.Label(key)}");
                ApSounds.Confetti();
            }

            _announcedOnce = true;
        }

        internal static void Say(string text) => ApNotices.Post(text);

        internal static void Sync()
        {
            Announce();
            GoodbyePrinterHoldPatch.ReplayIfUnlocked();
            PrinterHold.Tick();
            var text = IsGuest ? _mirroredText : HostText();
            if (string.IsNullOrEmpty(text) || (!IsGuest && ApObjectives.DebugKeys == null && MonumentOrder.DebugMode == null && !ModConfig.ArchipelagoEnabled.Value))
            {
                Take();
                return;
            }

            // Taller with more lines, so the text stays readable.
            var lines = text.Split('\n').Length;
            if (_board != null && lines != _lines)
                Take();
            if (_board == null)
                Make(lines);
            if (_text != null && text != _shown)
            {
                _text.text = text;
                _shown = text;
            }
        }

        internal static void Take()
        {
            if (_board != null)
                UnityEngine.Object.Destroy(_board);
            _board = null;
            _text = null;
            _shown = null;
        }

        private static void Make(int lines)
        {
            _lines = lines;
            _size = new Vector3(BoardWidth, Mathf.Clamp(0.17f * lines + 0.3f, 0.9f, 2.8f), BoardDepth);
            var face = SignPoint + AlongWall * ShiftAlongWall + Vector3.up * (BottomGap + _size.y / 2f) + Normal * 0.02f;
            _board = WorldButtons.MakeSlab("AP objectives board", face - Normal * (_size.z / 2f),
                Quaternion.LookRotation(Normal, Vector3.up), _size, new Color(0.62f, 0.58f, 0.5f));

            // Faces the reader, as ResyncStations' signs do: a TextMeshPro reads from its back.
            var holder = new GameObject("AP objectives text");
            holder.transform.SetParent(_board.transform, false);
            holder.transform.position = face + Normal * 0.01f;
            holder.transform.rotation = Quaternion.LookRotation(-Normal, Vector3.up);
            holder.transform.localScale = new Vector3(1f / _size.x, 1f / _size.y, 1f / _size.z);

            try
            {
                _text = holder.AddComponent<TextMeshPro>();
                var font = ResyncStations.SignFont();
                if (font != null)
                    _text.font = font;

                _text.rectTransform.sizeDelta = new Vector2(_size.x - 0.2f, _size.y - 0.12f);
                _text.alignment = TextAlignmentOptions.TopLeft;
                _text.enableAutoSizing = true;
                _text.fontSizeMin = 0.05f;
                _text.fontSizeMax = 1.5f;
                _text.color = new Color(0.12f, 0.12f, 0.14f);
                _shown = null;
                Plugin.Log.LogInfo($"{Tag} The board is up in the hub.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} No text on the board: {ex.Message}");
            }
        }
    }
}
