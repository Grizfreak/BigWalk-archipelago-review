using BigWalkArchipelago.Core;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Pins the nearest cosmetic gourd into the nearest empty monument home, skipping the hold.
    internal static class DebugCosmeticPinForce
    {
        internal static void ForceNearby(float radius)
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugCosmeticPinForce)}] Skipped: not host.");
                return;
            }

            var localPlayer = Core.LocalPlayer.Find();
            if (localPlayer == null)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugCosmeticPinForce)}] Local player not found.");
                return;
            }

            var origin = localPlayer.transform.position;

            var cosmeticProp = FindNearestCosmeticProp(origin, radius);
            if (cosmeticProp == null)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugCosmeticPinForce)}] No cosmetic gourd within {radius}m (spawn one with P first?).");
                return;
            }

            var propHome = FindNearestEmptyPropHome(origin, radius);
            if (propHome == null)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugCosmeticPinForce)}] No empty PropHome within {radius}m.");
                return;
            }

            var rewardGourd = cosmeticProp.GetComponent<RewardGourd>();
            if (rewardGourd != null)
                rewardGourd.ServerSetGourdState(GourdFlag.GourdState.Stashed);

            cosmeticProp.ServerSetPinned(propHome);

            Plugin.Log.LogInfo(
                $"[{nameof(DebugCosmeticPinForce)}] {cosmeticProp.gameObject.name} pinned into {propHome.saveableHomeName}.");
        }

        private static Prop FindNearestCosmeticProp(Vector3 origin, float radius)
        {
            var sqrRadius = radius * radius;
            Prop closest = null;
            var closestSqrDistance = float.MaxValue;

            foreach (var prop in Prop.allProps)
            {
                if (!ReceivedItemSpawner.IsCosmeticClone(prop))
                    continue;

                var sqrDistance = (prop.transform.position - origin).sqrMagnitude;
                if (sqrDistance > sqrRadius || sqrDistance >= closestSqrDistance)
                    continue;

                closestSqrDistance = sqrDistance;
                closest = prop;
            }

            return closest;
        }

        private static PropHome FindNearestEmptyPropHome(Vector3 origin, float radius)
        {
            var sqrRadius = radius * radius;
            PropHome closest = null;
            var closestSqrDistance = float.MaxValue;

            foreach (var home in PropHome.allPropHomes)
            {
                if (home == null || home.pinnedProp != null || !ReceivedItemSpawner.IsMonumentHome(home))
                    continue;

                var sqrDistance = (home.transform.position - origin).sqrMagnitude;
                if (sqrDistance > sqrRadius || sqrDistance >= closestSqrDistance)
                    continue;

                closestSqrDistance = sqrDistance;
                closest = home;
            }

            return closest;
        }
    }
}
