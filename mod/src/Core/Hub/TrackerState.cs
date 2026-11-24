using UnityEngine;

namespace BigWalkArchipelago.Core.Hub
{
    // What the trackers show, as the server knows it: checked and hinted locations.
    internal static class TrackerState
    {
        private const float RecountSeconds = 3f;

        internal static HashSet<long> Checked { get; private set; } = new();
        internal static HashSet<long> Hinted { get; private set; } = new();
        internal static bool Ready { get; private set; }

        private static float _recountAt;

        internal static void Tick()
        {
            if (Time.unscaledTime < _recountAt)
                return;
            _recountAt = Time.unscaledTime + RecountSeconds;

            var connection = ApRuntime.Current;
            if (connection == null || connection.Status != ApConnection.ConnectionStatus.Connected)
            {
                Ready = false;
                return;
            }

            try
            {
                Checked = connection.CheckedLocations();
                Hinted = connection.HintedLocations();
                Ready = true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(TrackerState)}] {ex.Message}");
            }
        }
    }
}
