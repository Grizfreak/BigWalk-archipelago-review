using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

namespace BigWalkArchipelago.Core.Net
{
    internal sealed partial class ApConnection
    {
        private static void OnItemReceived(ReceivedItemsHelper helper, ConcurrentQueue<ItemInfo> queue,
            Dictionary<string, int> received)
        {
            // Managed types only: see the threading note at the top.
            try
            {
                var item = helper.DequeueItem();
                queue.Enqueue(item);
                if (item != null && !string.IsNullOrEmpty(item.ItemName))
                {
                    lock (received)
                        received[item.ItemName] = received.TryGetValue(item.ItemName, out var count) ? count + 1 : 1;
                }
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

        // What this slot has received, by item name (a copy, safe on the main thread).
        internal Dictionary<string, int> ReceivedByName()
        {
            var received = _receivedByName;
            lock (received)
                return new Dictionary<string, int>(received);
        }
    }
}
