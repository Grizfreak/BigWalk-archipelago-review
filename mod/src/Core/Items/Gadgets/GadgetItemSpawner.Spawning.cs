using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Items
{
    internal static partial class GadgetItemSpawner
    {
        // Host entry point; toPlayer is false for the session-start replay, stocked at the hub.
        internal static GameObject SpawnCosmeticPickup(GadgetKind kind, bool toPlayer)
        {
            using var activity = ModActivity.Enter("cosmetic gadget spawn");
            if (!NetworkServer.active)
                return null;

            try
            {
                var recipient = toPlayer ? ItemRecipients.Choose() : null;
                var resolved = ReceivedItemSpawner.ResolveSpawnPosition(toPlayer, recipient);
                if (resolved == null)
                {
                    ItemRecipients.Settled(recipient);
                    Plugin.Log.LogInfo(
                        $"[{nameof(GadgetItemSpawner)}] Nowhere to put a {kind} yet (no player and no InventorySpawn loaded), cosmetic spawn skipped.");
                    return null;
                }

                EnsureVanillaHidden();
                if (GetTemplate(kind) == null)
                    TryCaptureLate(kind);

                var index = ChooseInstance(kind);
                var prop = BuildNeutralizedClone(kind, resolved.Value, Quaternion.identity, index);
                if (prop == null)
                {
                    ItemRecipients.Settled(recipient);
                    return null;
                }

                NetworkServer.Spawn(prop.gameObject, AssetIdFor(kind, index));

                // Otherwise the clone stays fixed in its original resting pose.
                prop.SetLoose();
                PublishLooseStates(prop.gameObject);

                ReceivedItemSpawner.QueueHandover(prop, recipient);
                KeepUnderLimit(kind, prop);

                var identity = prop.GetComponent<NetworkIdentity>();
                ColourPainter.PaintCopy(prop.gameObject, identity != null ? identity.netId : 0);
                Plugin.Log.LogInfo(
                    $"[{nameof(GadgetItemSpawner)}] Cosmetic {kind} #{index} spawned at {prop.transform.position} (netId {(identity != null ? identity.netId : 0)}).");
                return prop.gameObject;
            }
            catch (Exception ex)
            {
                // Must never make the item reception fail.
                Plugin.Log.LogWarning($"[{nameof(GadgetItemSpawner)}] Cosmetic spawn failed, ignored: {ex}");
                return null;
            }
        }

        // Host: one of `kind` set down upright at a place (Big Break's chair). Null on failure.
        internal static Prop SpawnAt(GadgetKind kind, Vector3 position, Quaternion rotation)
        {
            if (!NetworkServer.active)
                return null;

            try
            {
                if (GetTemplate(kind) == null)
                    TryCaptureLate(kind);

                var index = ChooseInstance(kind);
                var prop = BuildNeutralizedClone(kind, position, rotation, index);
                if (prop == null)
                    return null;

                // Lowest point on the spot, not the pivot; meshes answer while inactive.
                var lowest = float.MaxValue;
                foreach (var filter in prop.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = filter != null ? filter.sharedMesh : null;
                    if (mesh == null)
                        continue;
                    var bounds = mesh.bounds;
                    for (var i = 0; i < 8; i++)
                    {
                        var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                        lowest = Mathf.Min(lowest, filter.transform.TransformPoint(corner).y);
                    }
                }
                if (lowest < float.MaxValue)
                    prop.transform.position += Vector3.up * (position.y - lowest + 0.02f);

                NetworkServer.Spawn(prop.gameObject, AssetIdFor(kind, index));
                prop.SetLoose();
                PublishLooseStates(prop.gameObject);
                return prop;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(GadgetItemSpawner)}] Could not set down a {kind}: {ex.Message}");
                return null;
            }
        }

        internal static Prop SpawnFor(GadgetKind kind, PlayerCharacter player, bool toHands = true)
        {
            if (!NetworkServer.active || player == null)
                return null;

            try
            {
                if (GetTemplate(kind) == null)
                    TryCaptureLate(kind);

                var t = player.transform;
                var position = t.position + t.forward * 1.2f + Vector3.up * 0.8f;
                var index = ChooseInstance(kind);
                var prop = BuildNeutralizedClone(kind, position, Quaternion.identity, index);
                if (prop == null)
                    return null;

                NetworkServer.Spawn(prop.gameObject, AssetIdFor(kind, index));
                prop.SetLoose();
                PublishLooseStates(prop.gameObject);

                if (toHands && player.hands != null && !player.hands.isHoldingSomething)
                    ReceivedItemSpawner.QueueHandover(prop, player);

                return prop;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(GadgetItemSpawner)}] Could not give {player.gameObject.name} a {kind}: {ex.Message}");
                return null;
            }
        }
    }
}
