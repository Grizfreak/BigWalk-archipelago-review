using System.Collections.Concurrent;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;

namespace BigWalkArchipelago.Core.Net
{
    internal sealed partial class ApConnection
    {
        private static void OnItemReceived(ReceivedItemsHelper helper, ConcurrentQueue<ItemInfo> queue)
        {
            // Managed types only: see the threading note at the top.
            try
            {
                queue.Enqueue(helper.DequeueItem());
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Failed to read an incoming item: {ex.Message}");
            }
        }

        internal bool TryDequeueItem(out ItemInfo item)
        {
            return _incomingItems.TryDequeue(out item);
        }

        // ApRuntime waits for this to go quiet before reconciling gourds.
        internal bool HasPendingItems => !_incomingItems.IsEmpty;
    }
}
