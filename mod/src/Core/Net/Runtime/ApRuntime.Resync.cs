using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    internal partial class ApRuntime : MonoBehaviour
    {
        // Host only, one at a time on the whole map, so two restocks never pull the same items.
        internal static bool ResyncAll(Vector3? gatherAt = null, bool gadgets = true)
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogInfo($"[{nameof(ApRuntime)}] Resync ignored: only the host can do this.");
                ApNotices.Post("Only the host can resync", warning: true);
                return false;
            }

            if (ResyncRunning)
            {
                ApNotices.Post("A resync is already running", warning: true);
                return false;
            }

            if (gatherAt != null)
                ReceivedItemSpawner.GatherAt(gatherAt.Value);

            if (!ResyncGourds())
                return false;

            ResyncKeys();
            if (gadgets)
                ResyncGadgets();

            _resyncSince = Time.unscaledTime;
            _resyncGadgets = gadgets;
            return true;
        }

        private static float _resyncSince = -1f;
        private static bool _resyncGadgets;
        private const float ResyncShortest = 3f;
        private const float ResyncLongest = 60f;

        // Times out after a minute so a restock that cannot finish never locks the stations.
        internal static bool ResyncRunning
        {
            get
            {
                if (_resyncSince < 0f)
                    return false;

                var age = Time.unscaledTime - _resyncSince;
                var restocking = !_looseGourdsRestored || (_resyncGadgets && !_looseGadgetsRestored);
                if (age < ResyncShortest || (restocking && age < ResyncLongest))
                    return true;

                _resyncSince = -1f;
                return false;
            }
        }

        internal static void ResyncKeys()
        {
            var moved = KeyCustody.ResyncToSpawn();
            if (moved <= 0)
                return;

            Plugin.Log.LogInfo($"[{nameof(ApRuntime)}] {moved} big key(s) sent back to the spawn point.");
            ApNotices.Post($"Resync: {moved} key(s) sent back to the spawn point");
        }

        internal static bool ResyncGourds()
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogInfo($"[{nameof(ApRuntime)}] Gourd resync ignored: only the host can do this.");
                return false;
            }

            // The rebuild only runs on a live connection; sweeping now would give nothing back.
            if (Connection.Status != ApConnection.ConnectionStatus.Connected)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ApRuntime)}] Gourd resync refused: not connected to Archipelago, so the gourds could not be put back. Try again once reconnected.");
                ApNotices.Post("Resync refused: not connected to Archipelago", warning: true);
                return false;
            }

            var removed = ReceivedItemSpawner.DestroyLooseCosmeticGourds(out var keptGourds);

            _looseGourdsRestored = false;
            _looseRestoreBlockedLogged = false;
            _reconciliationLogged = false;
            _looseGourdsRestoredCount = 0;

            // Gourds in backpacks, cartons and belts still exist; zero would restock them twice.
            _gourdsSpawnedThisSession = keptGourds;
            _restoreTimer = 0f;

            Plugin.Log.LogInfo(
                $"[{nameof(ApRuntime)}] Gourd resync: {removed} loose gourd(s) cleared and {keptGourds} left where "
                + "they were stowed, restocking the hub from the ledger.");
            ApNotices.Post($"Resync: {removed} gourd(s) cleared, restocking");
            return true;
        }

        // Sweep and tally reset go together: one without the other duplicates or loses gadgets.
        internal static void ResyncGadgets()
        {
            if (!NetworkServer.active || Connection.Status != ApConnection.ConnectionStatus.Connected)
                return;

            var removed = GadgetItemSpawner.DestroyLooseCosmeticGadgets(out var keptGadgets);

            _gadgetsSpawnedThisSession.Clear();
            foreach (var pair in keptGadgets)
                _gadgetsSpawnedThisSession[pair.Key] = pair.Value;
            _looseGadgetsRestored = false;
            _looseGadgetsRestoreBlockedLogged = false;
            _gadgetRestoreTimer = 0f;

            Plugin.Log.LogInfo(
                $"[{nameof(ApRuntime)}] Gadget resync: {removed} loose gadget(s) cleared, restocking from the ledger.");
            ApNotices.Post($"Resync: {removed} item(s) cleared, restocking from the ledger");
        }
    }
}
