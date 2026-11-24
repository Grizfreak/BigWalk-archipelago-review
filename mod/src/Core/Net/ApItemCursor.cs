using System;

namespace BigWalkArchipelago.Core.Net
{
    // Items of the slot's received list already applied to this save; the server resends them all.
    internal static class ApItemCursor
    {
        private const string CountKey = "ap_items_received";
        private const string GourdCountKey = "ap_gourds_received";
        private const string GadgetCountKeyPrefix = "ap_gadgets_received_";
        private const string SeedKey = "ap_seed_name";
        // Read by the hosting screen to warn when a save was renamed away from its slot.
        internal const string SlotKey = "ap_slot_name";

        // A save that has never seen this (seed, slot) pair starts at zero and replays everything.
        internal static int SyncTo(string seedName, string slotName)
        {
            var knownSeed = SaveManager.GetStringValue(SeedKey);
            var knownSlot = SaveManager.GetStringValue(SlotKey);

            if (knownSeed == seedName && knownSlot == slotName)
                return SaveManager.GetIntValue(CountKey, 0, false);

            if (!string.IsNullOrEmpty(knownSeed) || !string.IsNullOrEmpty(knownSlot))
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ApItemCursor)}] This save was bound to seed '{knownSeed}' / slot '{knownSlot}', "
                    + $"now connecting as seed '{seedName}' / slot '{slotName}'. Replaying every received item from scratch.");

                // The checks it reported were for that other room: not to be sent to this one.
                var forgotten = CheckTracker.ClearReported();
                Plugin.Log.LogInfo(
                    $"[{nameof(ApItemCursor)}] {forgotten} check(s) reported to the previous seed forgotten, not resent.");
            }

            SaveManager.SetStringValue(SeedKey, seedName ?? string.Empty);
            SaveManager.SetStringValue(SlotKey, slotName ?? string.Empty);
            SaveManager.SetIntValue(CountKey, 0);
            SaveManager.SetIntValue(GourdCountKey, 0);

            foreach (GadgetKind kind in Enum.GetValues(typeof(GadgetKind)))
                SaveManager.SetIntValue(GadgetCountKeyPrefix + kind, 0);

            return 0;
        }

        internal static void Set(int appliedCount)
        {
            SaveManager.SetIntValue(CountKey, appliedCount);
        }

        // Loose gourds are not saved, so the count is kept and "received minus deposited" rebuilt.
        internal static int GourdsReceived => SaveManager.GetIntValue(GourdCountKey, 0, false);

        internal static void CountGourdReceived()
        {
            SaveManager.SetIntValue(GourdCountKey, GourdsReceived + 1);
        }

        // The server's item history is the authority: repairs a wrong or missing count.
        internal static void SetGourdsReceived(int count)
        {
            SaveManager.SetIntValue(GourdCountKey, count);
        }

        // Gadgets have no deposit sink, so each one received is owed a loose copy: never decreases.
        internal static int GadgetsReceived(GadgetKind kind) =>
            SaveManager.GetIntValue(GadgetCountKeyPrefix + kind, 0, false);

        internal static void CountGadgetReceived(GadgetKind kind)
        {
            SaveManager.SetIntValue(GadgetCountKeyPrefix + kind, GadgetsReceived(kind) + 1);
        }
    }
}
