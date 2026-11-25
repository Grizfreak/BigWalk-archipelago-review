using System.Text;
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

        // Whether guests may ask for hints; on a guest, what the host said.
        private static bool _mirroredMayAsk;

        private static bool GuestsMayAsk => _hintOn && ModConfig.GuestsCanAskHints.Value;

        // For the snapshot: bit 0, the guests may ask.
        internal static byte HostFlags() => (byte)(GuestsMayAsk ? 1 : 0);

        internal static void ApplyFromHost(byte flags)
        {
            _mirroredMayAsk = (flags & 1) != 0;
        }

        internal static void ForgetMirror()
        {
            _mirroredMayAsk = false;
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

        private static bool _wasWriting;

        // The sign opens with the list in it: emptied, so what is typed is the question alone.
        internal static void Tick()
        {
            try
            {
                var mine = IsGuest ? _mirroredMayAsk && IsOurs(_hintSign) : _hintOn;
                var writing = mine && _hintSign != null && _hintSign.isInputting && SignTextInput.IsSignInputActive();
                var field = writing && SignTextInput.instance != null ? SignTextInput.instance.inputField : null;
                if (field != null && !_wasWriting)
                    field.text = string.Empty;
                _wasWriting = field != null;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Hint sign: {ex.Message}");
            }
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
                if (input != null && SceneObjects.PathOf(input.transform).EndsWith(pathEnd, StringComparison.Ordinal))
                    return input;
            }
            return null;
        }
    }
}
