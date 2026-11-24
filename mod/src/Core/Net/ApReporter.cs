namespace BigWalkArchipelago.Core.Net
{
    // Logs each check like LocalLogReporter and queues it for the server.
    internal sealed class ApReporter : ICheckReporter
    {
        private readonly ICheckReporter _log = new LocalLogReporter();

        public void ReportCheck(string locationId)
        {
            _log.ReportCheck(locationId);

            // A check reported while the mod is acting is a bug, never a solve.
            var activity = ModActivity.Current;
            if (activity != null)
                Plugin.Log.LogWarning(
                    $"[{nameof(ApReporter)}] '{locationId}' was reported while the MOD was acting ({activity}); "
                    + $"this is not a genuine solve. Stack: {new System.Diagnostics.StackTrace(1, false)}");

            SessionJournal.Write("check", locationId);

            // Queued, not sent: this runs inside a Harmony patch, and ApRuntime owns the socket.
            ApRuntime.QueueLocation(locationId);
        }
    }
}
