using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    internal partial class ApRuntime : MonoBehaviour
    {
        // Owed = received - deposited - spawned this session; only pinned gourds persist.
        private static void RestoreLooseGourds()
        {
            if (_looseGourdsRestored || Connection.HasPendingItems)
                return;

            // An empty queue right after connecting usually means the replay has not landed yet.
            if (Time.unscaledTime - _connectedAt < ReplayGraceSeconds)
                return;

            if (_gourdsSeenThisSession > ApItemCursor.GourdsReceived)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(ApRuntime)}] Gourd ledger rebuilt from the server: {_gourdsSeenThisSession} received "
                    + $"(this save recorded {ApItemCursor.GourdsReceived}).");
                ApItemCursor.SetGourdsReceived(_gourdsSeenThisSession);
            }

            var deposited = CosmeticMonumentFillTracker.GetFilledMonumentCount();
            var owed = ApItemCursor.GourdsReceived - deposited - _gourdsSpawnedThisSession;

            if (!_reconciliationLogged)
            {
                _reconciliationLogged = true;
                Plugin.Log.LogInfo(
                    $"[{nameof(ApRuntime)}] Gourd reconciliation: {ApItemCursor.GourdsReceived} received, "
                    + $"{deposited} deposited, {_gourdsSpawnedThisSession} already spawned -> {owed} to restore.");
            }

            if (owed <= 0)
            {
                _looseGourdsRestored = true;
                if (_looseGourdsRestoredCount > 0)
                    Plugin.Log.LogInfo(
                        $"[{nameof(ApRuntime)}] Restored {_looseGourdsRestoredCount} gourd(s) received but never deposited.");
                return;
            }

            // One at a time on the game's spawn point; computed offsets lost gourds off the map.
            _restoreTimer -= Time.unscaledDeltaTime;
            if (_restoreTimer > 0f)
                return;

            _restoreTimer = RestoreIntervalSeconds;

            // A failed spawn (hub not loaded yet) is not counted, so it is retried.
            if (!ItemApplier.ApplyGourdItem(toPlayer: false))
            {
                if (!_looseRestoreBlockedLogged)
                {
                    _looseRestoreBlockedLogged = true;
                    Plugin.Log.LogInfo(
                        $"[{nameof(ApRuntime)}] Cannot spawn the {owed} owed gourd(s) yet (the hub's spawn point is "
                        + "probably not loaded); still trying.");
                }

                return;
            }

            _gourdsSpawnedThisSession++;
            _looseGourdsRestoredCount++;
        }

        private static int Kept(int received)
        {
            var limit = ModConfig.GadgetCopiesKept.Value;
            return limit > 0 ? Math.Min(received, limit) : received;
        }

        private static void RestoreLooseGadgets()
        {
            if (_looseGadgetsRestored || Connection.HasPendingItems)
                return;

            if (Time.unscaledTime - _connectedAt < ReplayGraceSeconds)
                return;

            _gadgetRestoreTimer -= Time.unscaledDeltaTime;
            if (_gadgetRestoreTimer > 0f)
                return;

            GadgetKind? next = null;
            var allSettled = true;

            foreach (GadgetKind kind in Enum.GetValues(typeof(GadgetKind)))
            {
                var spawnedThisSession = _gadgetsSpawnedThisSession.TryGetValue(kind, out var n) ? n : 0;
                var owed = Kept(ApItemCursor.GadgetsReceived(kind)) - spawnedThisSession;
                if (owed <= 0)
                    continue;

                allSettled = false;
                next = kind;
                break;
            }

            if (allSettled)
            {
                _looseGadgetsRestored = true;
                return;
            }

            _gadgetRestoreTimer = RestoreIntervalSeconds;

            if (!ItemApplier.ApplyGadgetItem(next.Value, toPlayer: false))
            {
                if (!_looseGadgetsRestoreBlockedLogged)
                {
                    _looseGadgetsRestoreBlockedLogged = true;
                    Plugin.Log.LogInfo(
                        $"[{nameof(ApRuntime)}] Cannot restore the owed {next.Value} yet (the hub's spawn point is "
                        + "probably not loaded); still trying.");
                }

                return;
            }

            _gadgetsSpawnedThisSession[next.Value] =
                (_gadgetsSpawnedThisSession.TryGetValue(next.Value, out var count) ? count : 0) + 1;
        }
    }
}
