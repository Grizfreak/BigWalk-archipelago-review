using System;
using System.Collections.Generic;
using System.Reflection;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Spawns cosmetic gourds for received items: neutralized clones of a scene gourd.
    internal static partial class ReceivedItemSpawner
    {
        internal const string CosmeticNameSuffix = "(AP cosmetic)";

        // A little above the ground, so it drops rather than starting half-buried.
        private const float PlayerSpawnDistance = 1.3f;
        private const float PlayerSpawnLift = 0.6f;

        // `toPlayer`: a mid-game gourd goes to a player; session-start stock goes to the hub.
        internal static GameObject SpawnCosmeticPickup(bool toPlayer)
        {
            using var activity = ModActivity.Enter("cosmetic gourd spawn");
            if (!NetworkServer.active)
                return null;

            try
            {
                var recipient = toPlayer ? ItemRecipients.Choose() : null;
                var resolved = ResolveSpawnPosition(toPlayer, recipient);
                if (resolved == null)
                {
                    ItemRecipients.Settled(recipient);
                    Plugin.Log.LogInfo($"[{nameof(ReceivedItemSpawner)}] Nowhere to put a gourd yet (no player and no InventorySpawn loaded), cosmetic spawn skipped.");
                    return null;
                }

                var position = resolved.Value;
                var rewardGourd = CreateNeutralizedClone(position, Quaternion.identity);
                if (rewardGourd == null)
                {
                    ItemRecipients.Settled(recipient);
                    return null;
                }

                rewardGourd.ServerSetGourdState(GourdFlag.GourdState.Loose);

                // Without this the clone stays fixed, without gravity, as in its original clamp.
                rewardGourd.prop?.SetLoose();

                // A frame later: spawn and hold in one frame can give a guest a null ref.
                QueueHandover(rewardGourd.prop, recipient);

                Plugin.Log.LogInfo(
                    $"[{nameof(ReceivedItemSpawner)}] Cosmetic gourd spawned at {rewardGourd.transform.position}.");
                return rewardGourd.gameObject;
            }
            catch (Exception ex)
            {
                // Cosmetic only: the item was already applied, so this must change nothing.
                Plugin.Log.LogWarning($"[{nameof(ReceivedItemSpawner)}] Cosmetic spawn failed, ignored: {ex}");
                return null;
            }
        }

        // Restores a gourd deposited in a previous session, pinned straight into its home.
        internal static GameObject SpawnCosmeticPickupPinnedTo(PropHome propHome)
        {
            using var activity = ModActivity.Enter("cosmetic gourd restore");
            if (!NetworkServer.active || propHome == null)
                return null;

            try
            {
                var rewardGourd = CreateNeutralizedClone(propHome.transform.position, propHome.transform.rotation);
                if (rewardGourd == null || rewardGourd.prop == null)
                    return null;

                // Stashed, not Loose: GourdStatePatch only reacts to Loose, so no false check.
                rewardGourd.ServerSetGourdState(GourdFlag.GourdState.Stashed);
                rewardGourd.prop.ServerSetPinned(propHome);

                return rewardGourd.gameObject;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ReceivedItemSpawner)}] Failed to restore a cosmetic gourd in {propHome.saveableHomeName}, ignored: {ex}");
                return null;
            }
        }

        // The nearest InventorySpawn: several are loaded at once, not one for the hub.
        private static InventorySpawn FindSpawnPoint()
        {
            var all = UnityEngine.Object.FindObjectsByType<InventorySpawn>(FindObjectsSortMode.None);
            if (all == null || all.Length == 0)
                return null;

            var origin = FindLocalPlayerPosition();
            if (origin == null || all.Length == 1)
                return all[0];

            InventorySpawn closest = null;
            var closestSqrDistance = float.MaxValue;
            foreach (var candidate in all)
            {
                if (candidate == null)
                    continue;

                var sqrDistance = (candidate.transform.position - origin.Value).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closest = candidate;
                }
            }

            return closest ?? all[0];
        }

        private static Vector3? FindLocalPlayerPosition()
        {
            var player = FindLocalPlayerCharacter();
            return player != null ? player.transform.position : null;
        }

        // Where the next resync puts its restock (a tower's gather button); lapses by itself.
        private static Vector3? _gatherAt;
        private static float _gatherUntil;
        private const float GatherLifetime = 20f;
        private const float GatherScatter = 0.7f;

        internal static void GatherAt(Vector3 where)
        {
            _gatherAt = where;
            _gatherUntil = Time.unscaledTime + GatherLifetime;
        }

        internal static Vector3? ResolveSpawnPosition(bool toPlayer, PlayerCharacter near = null)
        {
            if (!toPlayer && _gatherAt != null)
            {
                if (Time.unscaledTime < _gatherUntil)
                {
                    // Spread over a little ground so a dozen gourds do not land on one another.
                    return _gatherAt.Value + new Vector3(
                        UnityEngine.Random.Range(-GatherScatter, GatherScatter),
                        UnityEngine.Random.Range(0f, 0.5f),
                        UnityEngine.Random.Range(-GatherScatter, GatherScatter));
                }

                _gatherAt = null;
            }

            if (toPlayer && ModConfig.SpawnGourdAtPlayer.Value)
            {
                var player = near != null ? near : FindLocalPlayerCharacter();
                if (player != null)
                {
                    var t = player.transform;
                    return t.position + t.forward * PlayerSpawnDistance + Vector3.up * PlayerSpawnLift;
                }
            }

            var spawnPoint = FindSpawnPoint();
            return spawnPoint != null ? spawnPoint.GetNextSpawnPosition() : null;
        }
    }
}
