using System;
using System.Collections.Generic;
using System.Text;
using BigWalkArchipelago.Core.Net;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Hub
{
    // Two hub notice board signs: hints this slot asked for (`hint_board`) and checks in logic.
    internal static class HintBoards
    {
        private const string Tag = "[" + nameof(HintBoards) + "]";

        // The three signs on their hooks.
        private const string HintSignPath = "Hub/NoticeBoardWhiteboards/WallHook (1)/WallHookHome/SignProp center";
        private const string CheckSignPath = "Hub/NoticeBoardWhiteboards/WallHook (2)/WallHookHome/SignProp right";

        private const int MaxHints = 4;
        private const int MaxChecks = 4;
        private const float RecountSeconds = 3f;

        private static PeckEffectTextInput _hintSign;

        // `hint_board` (on by default) and `tracker_board` (off), on the host.
        private static bool _hintOn;

        // Whether guests may ask for hints; on a guest, what the host said and names to suggest.
        private static bool _mirroredMayAsk;
        private static List<string> _mirroredNames = new();

        private static bool GuestsMayAsk => _hintOn && ModConfig.GuestsCanAskHints.Value;

        // For the snapshot: bit 0, the guests may ask.
        internal static byte HostFlags() => (byte)(GuestsMayAsk ? 1 : 0);
        internal static string HostNames() => GuestsMayAsk ? string.Join("\n", HintableNames()) : string.Empty;

        internal static void ApplyFromHost(byte flags, string names)
        {
            _mirroredMayAsk = (flags & 1) != 0;
            _mirroredNames = string.IsNullOrEmpty(names) ? new List<string>() : new List<string>(names.Split('\n'));
        }

        internal static void ForgetMirror()
        {
            _mirroredMayAsk = false;
            _mirroredNames = new List<string>();
        }
        private static PeckEffectTextInput _checkSign;
        private static bool _checkLocked;
        private static float _recountAt;

        private static bool IsGuest => NetworkClient.active && !NetworkServer.active;

        // Locked here: the tracker board, and on a guest the hint board unless guests may ask.
        internal static bool IsLocked(PeckEffectTextInput input) =>
            input != null && (_checkLocked && input == _checkSign
                              || IsGuest && input == _hintSign && IsOurs(_hintSign) && !_mirroredMayAsk);



        // What the mod last wrote on the hint sign: anything else there is a player asking.
        private static string _hintWritten;

        // While a player writes on the hint sign here: the item names that match, for the overlay.
        internal static readonly List<string> Suggestions = new();
        private const int MaxSuggestions = 5;
        private static readonly BepInEx.Unity.IL2CPP.Configuration.KeyboardShortcut CompleteKey = new(KeyCode.Tab);
        private static bool _wasWriting;

        internal static void Tick()
        {
            try
            {
                var names = IsGuest ? _mirroredNames : HintableNames();
                var mine = IsGuest ? _mirroredMayAsk && IsOurs(_hintSign) : _hintOn;
                var writing = mine && _hintSign != null && _hintSign.isInputting && SignTextInput.IsSignInputActive()
                              && names.Count > 0;
                var field = writing && SignTextInput.instance != null ? SignTextInput.instance.inputField : null;
                if (field == null)
                {
                    _wasWriting = false;
                    Suggestions.Clear();
                    return;
                }

                // Opened with the list in it: emptied, so what is typed is the question alone.
                if (!_wasWriting)
                {
                    _wasWriting = true;
                    field.text = string.Empty;
                }

                Suggestions.Clear();
                Suggestions.AddRange(Matches(field.text, names, MaxSuggestions));
                if (Suggestions.Count > 0 && CompleteKey.IsDown())
                {
                    // A Tab the field may have typed in goes with the rest of the text.
                    field.text = Suggestions[0];
                    field.caretPosition = field.text.Length;
                }
            }
            catch (Exception ex)
            {
                Suggestions.Clear();
                Plugin.Log.LogWarning($"{Tag} Suggestions: {ex.Message}");
            }
        }

        // Item names not received yet; worked out when the field opens and as items arrive.
        private static List<string> _hintable = new();
        private static int _hintableAt = -1;

        private static List<string> HintableNames()
        {
            var slot = ApRuntime.SlotData;
            var connection = ApRuntime.Current;
            if (slot == null || connection == null || slot.HintItems.Count == 0)
                return _hintable = new List<string>();

            var received = connection.ReceivedByName();
            var total = 0;
            foreach (var count in received.Values)
                total += count;
            if (total == _hintableAt && _hintable.Count > 0)
                return _hintable;

            _hintableAt = total;
            _hintable = new List<string>();
            foreach (var pair in slot.HintItems)
            {
                if (!received.TryGetValue(pair.Key, out var have) || have < pair.Value)
                    _hintable.Add(pair.Key);
            }
            return _hintable;
        }

        // The names that start with what is typed, then those that hold it, ignoring case.
        private static List<string> Matches(string typed, List<string> names, int max)
        {
            var result = new List<string>();
            var query = (typed ?? string.Empty).Trim();
            if (query.Length == 0)
                return result;
            foreach (var name in names)
            {
                if (result.Count < max && name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    result.Add(name);
            }
            foreach (var name in names)
            {
                if (result.Count < max && !result.Contains(name) && name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    result.Add(name);
            }
            return result;
        }

        // An item's name in its own case when it matches one, else the text as it is.
        private static string Resolve(string asked)
        {
            var names = HintableNames();
            if (names.Count == 0)
                return asked;
            foreach (var name in names)
            {
                if (string.Equals(name, asked, StringComparison.OrdinalIgnoreCase))
                    return name;
            }
            // Never a guess, which would spend points on what was not asked: Tab completes.
            return asked;
        }

        // On every machine: finds the signs and says which are locked; the host writes their text.
        internal static void Sync()
        {
            if (!WorldManager.isReadyForEffects)
            {
                _hintSign = null;
                _checkSign = null;
                return;
            }

            // Unity's null test, not `??=`: a sign from the previous save is destroyed, not null.
            if (_hintSign == null)
            {
                _hintSign = Find(HintSignPath);
                _hintWritten = null;
            }
            if (_checkSign == null)
                _checkSign = Find(CheckSignPath);

            // A guest has no slot: it locks the signs whose text the host wrote.
            if (IsGuest)
            {
                _checkLocked = IsOurs(_checkSign);
                return;
            }

            var connection = ApRuntime.Current;
            var connected = connection != null && connection.Status == ApConnection.ConnectionStatus.Connected
                            && connection.SlotData != null;
            _checkLocked = connected && connection.SlotData.TrackerBoard && connection.SlotData.Logic != null;
            _hintOn = connected && connection.SlotData.HintBoard;
            if (!connected)
                return;

            var asked = _hintOn ? Asked(_hintSign) : null;
            if (asked != null)
            {
                asked = Resolve(asked);
                Plugin.Log.LogInfo($"{Tag} Hint asked at the sign: '{asked}'.");
                ApNotices.Post($"Asking for a hint: {asked}");
                connection.AskHint(asked);
                _recountAt = 0f;
            }

            if (Time.unscaledTime < _recountAt)
                return;
            _recountAt = Time.unscaledTime + RecountSeconds;

            try
            {
                if (_hintOn)
                {
                    _hintWritten = HintText(connection.OpenHintLines(), connection.HintBudget());
                    Write(_hintSign, _hintWritten);
                }
                if (_checkLocked)
                    Write(_checkSign, CheckText(connection.SlotData.Logic, connection));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} {ex.Message}");
            }
        }

        // The mod's texts start with this title, which is how a guest knows a sign is the mod's.
        private const string HintTitle = "HINTS";
        private const string CheckTitle = "LOGIC";

        private static bool IsOurs(PeckEffectTextInput sign)
        {
            var text = sign != null ? sign.NetworknetworkedText : null;
            return text != null && (text.StartsWith(HintTitle, StringComparison.Ordinal) || text.StartsWith(CheckTitle, StringComparison.Ordinal));
        }

        private static void Write(PeckEffectTextInput sign, string text)
        {
            if (sign == null || sign.NetworknetworkedText == text)
                return;
            sign.NetworknetworkedText = text;
        }

        // The last line not written by the mod: a player may write under the list or over it.
        private static string Asked(PeckEffectTextInput sign)
        {
            var text = sign != null ? sign.NetworknetworkedText : null;
            if (string.IsNullOrWhiteSpace(text) || _hintWritten == null || text == _hintWritten)
                return null;

            var ours = new HashSet<string>(_hintWritten.Split('\n'));
            var lines = text.Split('\n');
            for (var i = lines.Length - 1; i >= 0; i--)
            {
                var line = lines[i].Trim();
                if (line.Length > 0 && !ours.Contains(line) && !line.StartsWith(HintTitle, StringComparison.Ordinal))
                    return line;
            }

            // Only the mod's lines, rearranged or cut: nothing asked, the list goes back.
            Write(sign, _hintWritten);
            return null;
        }

        private static string HintText(List<string> lines, (int points, int cost) budget)
        {
            var text = new StringBuilder(HintTitle);
            if (budget.points >= 0 && budget.cost >= 0)
                text.Append($" {budget.points}/{budget.cost}");
            if (lines.Count == 0)
                return text.Append("\nnone asked for").ToString();

            var first = Math.Max(0, lines.Count - MaxHints);
            for (var i = first; i < lines.Count; i++)
                text.Append("\n- ").Append(lines[i]);
            if (first > 0)
                text.Append($"\n(and {first} older)");
            return text.ToString();
        }

        private static string CheckText(ApLogic logic, ApConnection connection)
        {
            var checkedIds = connection.CheckedLocations();
            var hinted = connection.HintedLocations();
            var first = new List<string>();
            var then = new List<string>();
            var rest = new List<string>();
            foreach (var (id, name) in logic.InLogic(connection.ReceivedByName()))
            {
                if (checkedIds.Contains(id))
                    continue;
                if (hinted.Contains(id))
                    first.Add(name);
                else if (connection.HoldsProgression(id))
                    then.Add(name);
                else
                    rest.Add(name);
            }
            var open = new List<string>(first);
            open.AddRange(then);
            open.AddRange(rest);

            var text = new StringBuilder($"{CheckTitle} {open.Count}/{connection.UncheckedCount(checkedIds)}");
            if (open.Count == 0)
                return text.Append("\nnothing right now").ToString();
            for (var i = 0; i < open.Count && i < MaxChecks; i++)
                text.Append("\n- ").Append(open[i]);
            if (open.Count > MaxChecks)
                text.Append($"\n(and {open.Count - MaxChecks} more)");
            return text.ToString();
        }

        private static PeckEffectTextInput Find(string pathEnd)
        {
            foreach (var input in UnityEngine.Object.FindObjectsByType<PeckEffectTextInput>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (input != null && PathOf(input.transform).EndsWith(pathEnd, StringComparison.Ordinal))
                    return input;
            }
            return null;
        }

        private static string PathOf(Transform t)
        {
            var path = t.name;
            for (var p = t.parent; p != null; p = p.parent)
                path = p.name + "/" + path;
            return path;
        }
    }
}
