using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Items
{
    internal static partial class ReceivedItemSpawner
    {
        // Removes cosmetic gourds lying loose or held; a homed one stays and is counted in `kept`.
        internal static int DestroyLooseCosmeticGourds(out int kept)
        {
            kept = 0;
            var destroyed = 0;
            var seen = 0;

            // Inactive included: a picked-up prop can leave the active hierarchy.
            var all = UnityEngine.Object.FindObjectsByType<RewardGourd>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all == null)
                return 0;

            foreach (var gourd in all)
            {
                if (gourd == null || !IsCosmeticClone(gourd.prop))
                    continue;

                var home = gourd.prop != null ? gourd.prop.currentHome : null;
                if (home != null)
                {
                    if (!IsMonumentHome(home))
                        kept++;
                    continue;
                }

                seen++;

                // Dropping is best effort and must not stop the removal of a held gourd.
                ReleaseFromHands(gourd.prop);

                try
                {
                    NetworkServer.Destroy(gourd.gameObject);
                    destroyed++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(ReceivedItemSpawner)}] Could not remove a cosmetic gourd: {ex.Message}");
                }
            }

            if (destroyed != seen)
                Plugin.Log.LogWarning(
                    $"[{nameof(ReceivedItemSpawner)}] {seen} loose cosmetic gourd(s) found but only {destroyed} removed; the rebuild will over-count.");

            return destroyed;
        }

        // Drop before destroying or hiding. The struct is built from the prop: a default is null.
        internal static void ReleaseFromHands(Prop prop)
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (prop == null || players == null)
                return;

            using (ModActivity.Enter("release from hands"))
            {
                var what = $"'{prop.gameObject.name}' (saveablePropName {prop.saveablePropName}, home {prop.currentHome?.saveableHomeName})";
                Plugin.Log.LogInfo($"[{nameof(ReceivedItemSpawner)}] Releasing {what}.");
                ReleaseFromHandsCore(prop, players);
            }
        }

        private static void ReleaseFromHandsCore(Prop prop, Il2CppSystem.Collections.Generic.List<PlayerCharacter> players)
        {
            foreach (var pc in players)
            {
                if (pc?.hands == null || pc.hands.heldProp != prop)
                    continue;

                try
                {
                    pc.hands.Drop(new PlayerHeldInformation(prop));
                }
                catch (Exception ex)
                {
                    // Last resort: sever the reference by hand.
                    Plugin.Log.LogWarning($"[{nameof(ReceivedItemSpawner)}] Drop failed, clearing the hand directly: {ex.Message}");
                    try { pc.hands.heldProp = null; }
                    catch (Exception inner) { Plugin.Log.LogWarning($"[{nameof(ReceivedItemSpawner)}] ...and that failed too: {inner.Message}"); }
                }
            }

            ClearServerSideHold(prop, players);
        }

        // A remote player's hands ignore a host Drop(), so the host rewrites the held SyncVar.
        private static void ClearServerSideHold(Prop prop, Il2CppSystem.Collections.Generic.List<PlayerCharacter> players)
        {
            if (!NetworkServer.active || prop == null || players == null)
                return;

            var identity = prop.GetComponent<NetworkIdentity>();
            if (identity == null)
                return;

            foreach (var pc in players)
            {
                var networking = pc != null ? pc.playerNetworking : null;
                if (networking == null)
                    continue;

                try
                {
                    var held = networking.playerHeldInformation;
                    if (held == null || held.identity == null || held.identity != identity)
                        continue;

                    held.identity = null;
                    held.heldType = PlayerHeldInformation.HeldType.Nothing;
                    held.hasDropData = false;
                    held.actionNumber = held.actionNumber + 1;
                    networking.NetworkplayerHeldInformation = held;

                    Plugin.Log.LogInfo(
                        $"[{nameof(ReceivedItemSpawner)}] {pc.gameObject.name} was still holding netId {identity.netId} "
                        + "as far as the server knew; cleared, so their own machine lets go too.");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(ReceivedItemSpawner)}] Could not clear a player's held information: {ex.Message}");
                }
            }
        }
    }
}
