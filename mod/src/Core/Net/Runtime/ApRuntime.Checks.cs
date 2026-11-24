using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    internal partial class ApRuntime : MonoBehaviour
    {
        internal static void QueueLocation(string locationName)
        {
            // A non-host never connects, so its queue would only grow.
            if (string.IsNullOrEmpty(locationName) || !NetworkServer.active)
                return;

            // A check made with Archipelago off belongs to no seed.
            if (!ModConfig.ArchipelagoEnabled.Value)
                return;

            PendingLocationNames.Enqueue(locationName);
        }

        // Checks belong to the save going away; the ap_reported_* ledger resends them later.
        private static void ReleasePendingChecksWithTheWorld()
        {
            if (Connection.Status == ApConnection.ConnectionStatus.Connected)
                FlushPendingChecks();

            var dropped = 0;
            while (PendingLocationNames.TryDequeue(out _))
                dropped++;

            if (dropped > 0)
                Plugin.Log.LogInfo(
                    $"[{nameof(ApRuntime)}] {dropped} check(s) still waiting for the server went with the world that made "
                    + "them; its save resends them on its next connection.");
        }

        // Reported checks are marked in the save, so offline checks are only sent from here.
        private static void ResendKnownChecks()
        {
            SendBuffer.Clear();

            foreach (var locationName in CheckTracker.GetReportedLocationNames())
            {
                if (ApLocationIds.TryResolveLocation(locationName, out var id)
                    && Connection.BelongsToSlot(id))
                    SendBuffer.Add(id);
            }

            if (SendBuffer.Count == 0)
                return;

            Plugin.Log.LogInfo($"[{nameof(ApRuntime)}] Resending {SendBuffer.Count} check(s) already validated on this save.");
            Connection.SendChecks(SendBuffer.ToArray());
        }

        private static void FlushPendingChecks()
        {
            SendBuffer.Clear();

            while (PendingLocationNames.TryDequeue(out var locationName))
            {
                if (!ApLocationIds.TryResolveLocation(locationName, out var id))
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(ApRuntime)}] No Archipelago id for '{locationName}', check dropped.");
                    continue;
                }

                // Sending an id this slot does not have is a protocol error.
                if (!Connection.BelongsToSlot(id))
                    continue;

                SendBuffer.Add(id);
            }

            if (SendBuffer.Count == 0)
                return;

            Connection.SendChecks(SendBuffer.ToArray());

            // Only newly flushed checks; the resend on connect is not announced.
            foreach (var id in SendBuffer)
            {
                var name = Connection.LocationName(id);
                if (name == null || !name.EndsWith("Puzzle", StringComparison.Ordinal))
                {
                    ApSounds.CheckSent();
                    break;
                }
            }
        }
    }
}
