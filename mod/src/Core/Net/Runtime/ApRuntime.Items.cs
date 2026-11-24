using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    internal partial class ApRuntime : MonoBehaviour
    {
        private static void DrainIncomingItems()
        {
            var appliedThisFrame = 0;

            while (appliedThisFrame < MaxItemsPerFrame && Connection.TryDequeueItem(out var item))
            {
                _seenItemCount++;

                // Counted even for skipped items: this tally rebuilds the gourd ledger.
                var isGourd = item.ItemId == ApLocationIds.GourdItemId;
                if (isGourd)
                    _gourdsSeenThisSession++;

                var isGadget = ApLocationIds.TryResolveGadgetItem(item.ItemId, out var gadgetKind);
                // `pack_sanity: vanilla`: a pack that is its own pick-up's item is already there.
                if (isGadget && PackChecks.IsPackItself(item))
                    isGadget = false;

                // Already in this save from an earlier session: the server's replay.
                if (_seenItemCount <= _appliedItemCount)
                    continue;

                // Owed whether or not the prop spawned; the next reconciliation makes good on it.
                if (isGourd)
                    ApItemCursor.CountGourdReceived();
                else if (isGadget)
                    ApItemCursor.CountGadgetReceived(gadgetKind);

                // A reconnect can post many at once, which is why ApNotices collapses repeats.
                ApNotices.Post($"Received: {Palette.Display(GourdNames.Display(item.ItemId, item.ItemName))}");
                if (Traps.TryResolveItem(item.ItemId, out var trapKey) && !TrapEffects.IsBonus(trapKey))
                    ApSounds.DeathLinkAlert();
                else
                    ApSounds.Received();
                SessionJournal.Write(
                    "item", $"{item.ItemName} | from {item.Player?.Name} | at {item.LocationName}", withPosition: false);

                try
                {
                    var materialized = Apply(item);
                    if (isGourd && materialized)
                        _gourdsSpawnedThisSession++;
                    else if (isGourd)
                        // The prop did not appear: re-arm the reconciler so the gourd comes back.
                        _looseGourdsRestored = false;
                    else if (isGadget && materialized)
                        _gadgetsSpawnedThisSession[gadgetKind] =
                            (_gadgetsSpawnedThisSession.TryGetValue(gadgetKind, out var count) ? count : 0) + 1;
                    else if (isGadget)
                        _looseGadgetsRestored = false;
                }
                catch (Exception ex)
                {
                    // Still advance the cursor, or every later item would be stuck behind this one.
                    Plugin.Log.LogError(
                        $"[{nameof(ApRuntime)}] Failed to apply item {item.ItemId}: {ex}");
                }

                appliedThisFrame++;
                _appliedItemCount = _seenItemCount;
                ApItemCursor.Set(_appliedItemCount);
            }
        }

        // Returns whether the item materialized, which is what the gourd reconciliation counts.
        private static bool Apply(Archipelago.MultiClient.Net.Models.ItemInfo item)
        {
            var itemId = item.ItemId;

            if (PackChecks.IsPackItself(item))
                return true;

            if (itemId == ApLocationIds.GourdItemId && !_looseGourdsRestored && _skipGourdSpawns > 0)
            {
                _skipGourdSpawns--;
                return true;
            }

            if (itemId == ApLocationIds.GourdItemId)
                // The startup burst goes to the spawn; only later items go to the player's hands.
                return ItemApplier.ApplyGourdItem(toPlayer: _looseGourdsRestored);

            // Keys before doors: a key id would otherwise match the door range.
            if (MonumentOrder.IsItem(itemId))
                return MonumentOrder.Grant();

            if (ApLocationIds.TryResolveKeyItem(itemId, out var keyName))
                return KeyCustody.Grant(keyName, toPlayer: _looseGourdsRestored);

            if (ApLocationIds.TryResolveBigKeyItem(itemId, out var propName))
                return ItemApplier.ApplyBigKeyItem(propName);

            if (ApLocationIds.TryResolveRadioItem(itemId, out var station))
                return RadioStations.Grant(station);

            if (ApLocationIds.TryResolveArchDoorItem(itemId, out var archDoor))
                return ArchDoors.Grant(archDoor);

            if (PuzzleNeeds.TryResolveItem(itemId, out var need))
                return PuzzleNeeds.Grant(need);

            if (ZoneLocks.IsItem(itemId))
                return ZoneLocks.Grant(itemId);

            if (TunnelDoors.IsItem(itemId))
                return TunnelDoors.Grant(itemId);

            if (AbilityLocks.TryResolveItem(itemId, out var ability))
                return AbilityLocks.Grant(ability);

            if (ApLocationIds.TryResolveGauntletItem(itemId, out var stairway))
                return GauntletStairways.Grant(stairway);

            if (TeleportButtons.TryResolveItem(itemId, out var teleporter))
                return TeleportButtons.Grant(teleporter);

            if (ApLocationIds.TryResolveGadgetItem(itemId, out var gadgetKind))
            {
                var spawned = _gadgetsSpawnedThisSession.TryGetValue(gadgetKind, out var already) ? already : 0;
                if (!_looseGadgetsRestored && spawned >= Kept(int.MaxValue))
                    return true;
                return ItemApplier.ApplyGadgetItem(gadgetKind, toPlayer: _looseGadgetsRestored);
            }

            if (Traps.TryResolveItem(itemId, out var effect))
            {
                var queued = Traps.Enqueue(effect, $"item {item.ItemName}");
                if (queued)
                    Traps.SendTrapLink(effect, item.ItemName);
                return queued;
            }

            Plugin.Log.LogInfo($"[{nameof(ApRuntime)}] Received '{item.ItemName}' ({itemId}): no in-game effect.");
            return false;
        }
    }
}
