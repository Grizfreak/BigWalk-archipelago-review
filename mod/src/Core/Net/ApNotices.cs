using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    // Short overlay lines for checks sent and items received. Main thread only.
    internal static class ApNotices
    {
        internal const int MaxLines = 6;

        internal sealed class Notice
        {
            internal string Text;
            internal string Display;
            internal int Count;
            internal bool Warning;
            internal float ExpiresAt;
        }

        // Oldest first, which is also the order they are drawn in.
        private static readonly List<Notice> Live = new();

        internal static IReadOnlyList<Notice> Lines => Live;

        // A check this slot sent, posted from the socket's thread: shown for a short while only.
        private const float CheckSeconds = 6f;
        private static readonly System.Collections.Concurrent.ConcurrentQueue<string> FromSocket = new();

        internal static void PostFromSocket(string text) => FromSocket.Enqueue(text);

        internal static void Post(string text, bool warning = false, float seconds = -1f)
        {
            if (string.IsNullOrEmpty(text) || !ModConfig.ShowItemFeed.Value)
                return;

            var expiry = Time.unscaledTime + (seconds > 0f ? seconds : ModConfig.NoticeSeconds.Value);

            // Only the last line collapses, so two different items are never merged into one count.
            if (Live.Count > 0)
            {
                var last = Live[Live.Count - 1];
                if (last.Text == text && last.Warning == warning)
                {
                    last.Count++;
                    last.Display = $"{text} x{last.Count}";
                    last.ExpiresAt = expiry;
                    return;
                }
            }

            Live.Add(new Notice
            {
                Text = text,
                Display = text,
                Count = 1,
                Warning = warning,
                ExpiresAt = expiry,
            });

            while (Live.Count > MaxLines)
                Live.RemoveAt(0);
        }

        // Called from Update, not OnGUI: OnGUI runs several times per frame.
        internal static void Prune()
        {
            while (FromSocket.TryDequeue(out var sent))
                Post(sent, seconds: CheckSeconds);

            var now = Time.unscaledTime;
            while (Live.Count > 0 && Live[0].ExpiresAt <= now)
                Live.RemoveAt(0);
        }

        internal static void Clear()
        {
            Live.Clear();
        }
    }
}
