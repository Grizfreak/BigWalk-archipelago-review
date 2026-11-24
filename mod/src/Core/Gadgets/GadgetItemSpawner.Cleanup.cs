using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal static partial class GadgetItemSpawner
    {
        // Our copies of each kind in this world, oldest first, for ModConfig.GadgetCopiesKept.
        private static readonly Dictionary<GadgetKind, List<Prop>> Spawned = new();

        // A copy past the limit replaces the oldest one lying loose. Packs are left alone.
        private static void KeepUnderLimit(GadgetKind kind, Prop added)
        {
            if (!Spawned.TryGetValue(kind, out var list))
                Spawned[kind] = list = new List<Prop>();
            list.RemoveAll(prop => prop == null);
            list.Add(added);

            var limit = ModConfig.GadgetCopiesKept.Value;
            if (limit <= 0 || Packs.Contains(kind))
                return;

            var removed = 0;
            for (var i = 0; i < list.Count - 1 && list.Count > limit;)
            {
                var prop = list[i];
                if (prop.currentHome != null || IsHeldByAnyone(prop))
                {
                    i++;
                    continue;
                }

                list.RemoveAt(i);
                try
                {
                    NetworkServer.Destroy(prop.gameObject);
                    removed++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(GadgetItemSpawner)}] Could not remove an old {kind}: {ex.Message}");
                }
            }

            if (removed > 0)
                Plugin.Log.LogInfo(
                    $"[{nameof(GadgetItemSpawner)}] {removed} old {kind}(s) removed to keep {limit} in the world.");
        }

        private static bool IsHeldByAnyone(Prop prop)
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null)
                return false;

            foreach (var pc in players)
            {
                if (pc != null && pc.hands != null && pc.hands.heldProp == prop)
                    return true;
            }

            return false;
        }

        // Counted so the rebuild does not restock what the sweep left standing.
        private static void CountKept(Dictionary<GadgetKind, int> kept, Prop prop)
        {
            var kind = MatchKind(prop.gameObject.name);
            if (kind == null)
                return;

            kept[kind.Value] = kept.TryGetValue(kind.Value, out var count) ? count + 1 : 1;
        }

        // Only loose props go: anything in a home (worn, stowed) or holding one stays.
        internal static int DestroyLooseCosmeticGadgets(out Dictionary<GadgetKind, int> kept)
        {
            kept = new Dictionary<GadgetKind, int>();
            var destroyed = 0;
            var seen = 0;
            var placed = 0;

            // Inactive included: a picked-up prop can leave the active hierarchy.
            var all = UnityEngine.Object.FindObjectsByType<Prop>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all == null)
                return 0;

            var candidates = new List<Prop>();
            var occupiedHosts = new HashSet<int>();

            foreach (var prop in all)
            {
                if (prop == null || !ReceivedItemSpawner.IsCosmeticClone(prop))
                    continue;

                // A gourd is not swept here, but it can keep its backpack or carton.
                if (prop.GetComponent<RewardGourd>() != null)
                {
                    var gourdHome = prop.currentHome;
                    if (gourdHome != null && gourdHome.parentProp != null)
                        occupiedHosts.Add(gourdHome.parentProp.GetInstanceID());
                    continue;
                }

                seen++;

                var home = prop.currentHome;
                if (home == null)
                {
                    candidates.Add(prop);
                    continue;
                }

                placed++;
                CountKept(kept, prop);

                // Its holder stays too, or its contents would be homed in a destroyed object.
                if (home.parentProp != null)
                    occupiedHosts.Add(home.parentProp.GetInstanceID());
            }

            foreach (var prop in candidates)
            {
                if (occupiedHosts.Contains(prop.GetInstanceID()))
                {
                    placed++;
                    CountKept(kept, prop);
                    continue;
                }

                // Tidying the hands is best effort; removing the prop is not.
                ReceivedItemSpawner.ReleaseFromHands(prop);

                try
                {
                    NetworkServer.Destroy(prop.gameObject);
                    destroyed++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(GadgetItemSpawner)}] Could not remove a cosmetic gadget: {ex.Message}");
                }
            }

            if (placed > 0)
                Plugin.Log.LogInfo(
                    $"[{nameof(GadgetItemSpawner)}] {placed} cosmetic gadget(s) left alone: worn, stowed or holding "
                    + "something that is.");

            seen -= placed;

            if (destroyed != seen)
                Plugin.Log.LogWarning(
                    $"[{nameof(GadgetItemSpawner)}] {seen} loose cosmetic gadget(s) found but only {destroyed} removed; "
                    + "the rebuild will put back fewer than are owed.");

            return destroyed;
        }
    }
}
