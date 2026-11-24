using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace BigWalkArchipelago.Core.Net
{
    internal sealed partial class ApConnection
    {
        // Monument slots filled, from DataStorage; 0 when absent or the read fails.
        internal int StoredDeposits { get; private set; }

        private const string DepositsKey = "bigwalk_deposits";
        private const int StorageWaitMilliseconds = 4000;

        // Only ever goes up and a room is one seed; any failure is a 0 and a log line.
        private static int ReadStoredDeposits(ArchipelagoSession session)
        {
            try
            {
                var element = session.DataStorage[Scope.Slot, DepositsKey];
                element.Initialize(0);

                var read = element.GetAsync();
                if (!read.Wait(StorageWaitMilliseconds))
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(ApConnection)}] The server did not answer the deposits read in {StorageWaitMilliseconds} ms; "
                        + "no deposit will be restored this session.");
                    return 0;
                }

                var token = read.Result;
                var count = token != null && token.Type != Newtonsoft.Json.Linq.JTokenType.Null ? token.ToObject<int>() : 0;
                Plugin.Log.LogInfo($"[{nameof(ApConnection)}] The server remembers {count} deposit(s) for this slot.");
                return Math.Max(0, count);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ApConnection)}] Could not read the deposits from the server ({ex.Message}); none will be restored.");
                return 0;
            }
        }

        // The server keeps the larger value, so a late or repeated write can never lower it.
        internal void StoreDeposits(int count)
        {
            if (_status != ConnectionStatus.Connected || count <= 0)
                return;

            try
            {
                _session.DataStorage[Scope.Slot, DepositsKey] += Operation.Max(count);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Could not store {count} deposit(s) on the server: {ex.Message}");
            }
        }
    }
}
