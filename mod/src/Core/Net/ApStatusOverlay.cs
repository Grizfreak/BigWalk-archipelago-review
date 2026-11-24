using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    // IMGUI overlay for the Archipelago status, goal and feed: no prefab or canvas needed.
    internal class ApStatusOverlay : MonoBehaviour
    {
        // Constructor required by Il2CppInterop for any type injected into IL2CPP.
        public ApStatusOverlay(IntPtr ptr) : base(ptr)
        {
        }

        private const int Margin = 12;

        private const float SecondaryScale = 0.8f;

        private static readonly Color WarningColor = new(1f, 0.62f, 0.17f);
        private static readonly Color InfoColor = new(0.72f, 0.9f, 0.72f);
        private static readonly Color ProgressColor = new(0.95f, 0.88f, 0.6f);
        private static readonly Color FeedColor = new(0.86f, 0.86f, 0.86f);
        private static readonly Color DoneColor = new(0.6f, 0.6f, 0.55f);
        private static readonly Color ShadowColor = new(0f, 0f, 0f, 0.75f);

        // Built on first use: touching GUI.skin outside of OnGUI is invalid in Unity.
        private GUIStyle _style;
        private int _styleFontSize;

        private void OnGUI()
        {
            if (!ModConfig.ShowConnectionStatus.Value)
                return;

            // A guest never connects itself, so it shows what its host last sent.
            var mirrored = string.IsNullOrEmpty(ApRuntime.StatusMessage) && ModChannel.HasFreshSnapshot;

            var message = mirrored ? GuestStatus(ModChannel.MirroredStatus) : ApRuntime.StatusMessage;

            // Shown even with nothing else to say: a guest that never hears from its host.
            var warnings = ModChannel.VersionWarnings();
            if (string.IsNullOrEmpty(message) && warnings.Count == 0)
                return;

            var statusIsWarning = mirrored ? ModChannel.MirroredStatusIsWarning : ApRuntime.StatusIsWarning;
            var goal = mirrored ? ModChannel.MirroredGoal : ApRuntime.GoalLine;
            var goalIsWarning = mirrored ? ModChannel.MirroredGoalIsWarning : ApRuntime.GoalLineIsWarning;

            var fontSize = Mathf.Clamp(ModConfig.StatusFontSize.Value, 8, 72);
            if (_style == null || _styleFontSize != fontSize)
            {
                // No TextAnchor: it would need the TextRenderingModule interop just for a default.
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fontSize,
                    wordWrap = false,
                };
                _styleFontSize = fontSize;
            }

            var y = (float)Margin;
            foreach (var warning in warnings)
                y = DrawLine(warning, WarningColor, fontSize, y);
            if (string.IsNullOrEmpty(message))
                return;

            y = DrawLine(message, statusIsWarning ? WarningColor : InfoColor, fontSize, y);

            // While the hint sign is written on: the matching item names (Tab takes the first).
            if (HintBoards.Suggestions.Count > 0)
            {
                y = DrawLine("Hint for (Tab):", InfoColor, fontSize, y);
                foreach (var suggestion in HintBoards.Suggestions)
                    y = DrawLine("  " + suggestion, ProgressColor, fontSize, y);
            }

            var secondary = Mathf.Max(8, Mathf.RoundToInt(fontSize * SecondaryScale));

            if (!string.IsNullOrEmpty(goal))
            {
                var goalLines = goal.Split('\n');
                for (var i = 0; i < goalLines.Length; i++)
                {
                    // A done objective ("[x]") is dimmed and struck through, like on the hub board.
                    var done = i > 0 && goalLines[i].TrimStart().StartsWith("[x]", StringComparison.Ordinal);
                    // Rich text for these lines only: another game's item name may hold a `<`.
                    _style.richText = true;
                    y = DrawLine(goalLines[i], goalIsWarning ? WarningColor : done ? DoneColor : ProgressColor, i == 0 ? fontSize : secondary, y, done);
                    _style.richText = false;
                }
            }

            if (mirrored)
            {
                foreach (var line in ModChannel.MirroredLines)
                    y = DrawLine(line.Text, line.Warning ? WarningColor : FeedColor, secondary, y);

                foreach (var notice in ApNotices.Lines)
                    y = DrawLine(notice.Display, notice.Warning ? WarningColor : FeedColor, secondary, y);

                return;
            }

            var lines = ApNotices.Lines;
            for (var i = 0; i < lines.Count; i++)
            {
                var notice = lines[i];
                y = DrawLine(notice.Display, notice.Warning ? WarningColor : FeedColor, secondary, y);
            }
        }

        // Says whose connection it is: a guest is never connected itself.
        private static string GuestStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
                return status;

            const string prefix = "Archipelago:";
            return status.StartsWith(prefix, StringComparison.Ordinal)
                ? "Archipelago (host):" + status.Substring(prefix.Length)
                : status;
        }

        // Returns the y to draw the next line at.
        private float DrawLine(string text, Color color, int fontSize, float y, bool struck = false)
        {
            _style.fontSize = fontSize;

            var size = _style.CalcSize(new GUIContent(text));
            var rect = new Rect(Margin, y, size.x, size.y);

            // Drawn twice, offset, to stay legible over anything behind it.
            _style.normal.textColor = ShadowColor;
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, _style);

            _style.normal.textColor = color;
            GUI.Label(rect, text, _style);

            if (struck)
            {
                // The strike starts after the "[x]" box and runs over the name only.
                var start = _style.CalcSize(new GUIContent(text.Substring(0, text.IndexOf(']') + 1))).x;
                var old = GUI.color;
                GUI.color = color;
                GUI.DrawTexture(new Rect(rect.x + start + 4f, rect.y + size.y * 0.55f, size.x - start - 4f, 1.5f), Texture2D.whiteTexture);
                GUI.color = old;
            }

            return y + size.y;
        }

    }
}
