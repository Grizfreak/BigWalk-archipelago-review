using System;
using System.Collections.Generic;
using System.Reflection;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal static partial class ReceivedItemSpawner
    {
        // Our own assetId for the clones, matched by CosmeticGourdSpawnHandler on every client.
        internal const uint CosmeticAssetId = 0xB16_9A00;

        // Builds without spawning, so each client's spawn handler builds the same clone itself.
        internal static RewardGourd BuildNeutralizedClone(Vector3 position, Quaternion rotation)
        {
            var template = FindTemplate();
            if (template == null)
            {
                Plugin.Log.LogInfo($"[{nameof(ReceivedItemSpawner)}] No RewardGourd loaded to clone, cosmetic spawn skipped.");
                return null;
            }

            // Instantiate does not copy property blocks: captured here, reapplied below.
            var templatePropertyBlocks = CapturePropertyBlocks(template.gameObject);

            // Born under an inactive parent: it must not wake before its ids are cleared.
            var holder = TemplateHolder();
            var clone = UnityEngine.Object.Instantiate(template.gameObject, position, rotation, holder);

            clone.name = $"{template.gameObject.name} {CosmeticNameSuffix}";

            var cloneIdentity = clone.GetComponent<NetworkIdentity>();
            if (cloneIdentity != null)
            {
                cloneIdentity.sceneId = 0;

                // hasSpawned is an interop property, not a field: set through its setter.
                GetPrivatePropertySetter<NetworkIdentity>("hasSpawned")?.Invoke(cloneIdentity, new object[] { false });
            }

            var rewardGourd = clone.GetComponent<RewardGourd>();
            if (rewardGourd == null)
            {
                Plugin.Log.LogWarning($"[{nameof(ReceivedItemSpawner)}] Clone without a RewardGourd, destroying and aborting.");
                UnityEngine.Object.Destroy(clone);
                return null;
            }

            ApplyCosmeticColor(rewardGourd);

            // Neutralized before activation: after ServerSetGourdState it made a false check.
            NeutralizeProgression(rewardGourd.prop);
            DetachExternalPecks(rewardGourd.prop, clone.transform);
            var propsToMakeSavable = rewardGourd.propsToMakeSavable;
            if (propsToMakeSavable != null)
            {
                foreach (var block in propsToMakeSavable)
                {
                    if (block == null || block.props == null)
                        continue;

                    foreach (var prop in block.props)
                        NeutralizeProgression(prop);
                }
            }

            // Reapplied by index, before activation, so not a single frame shows the bare look.
            ApplyPropertyBlocks(clone, templatePropertyBlocks);

            // Wakes the clone: Awake()/OnEnable() run here, on the neutralized object.
            clone.transform.SetParent(null, true);
            clone.SetActive(true);

            // Awake sets SpawnedFromInstantiate; it is reset to false through its setter.
            if (cloneIdentity != null)
                GetPrivatePropertySetter<NetworkIdentity>(nameof(NetworkIdentity.SpawnedFromInstantiate))?.Invoke(cloneIdentity, new object[] { false });

            RefreshCosmeticColor();

            return rewardGourd;
        }

        // Instantiate keeps the puzzle's outside switches: holding the clone solved the real one.
        private static void DetachExternalPecks(Prop prop, Transform root)
        {
            if (prop == null)
                return;

            var detached = new List<string>();
            prop.useHeldSwitch = Detach(prop.useHeldSwitch, root, "useHeldSwitch", detached);
            prop.useHeldUpSwitch = Detach(prop.useHeldUpSwitch, root, "useHeldUpSwitch", detached);
            prop.onDropSwitch = Detach(prop.onDropSwitch, root, "onDropSwitch", detached);
            prop.onEjectSwitch = Detach(prop.onEjectSwitch, root, "onEjectSwitch", detached);
            prop.onUseAsKey = Detach(prop.onUseAsKey, root, "onUseAsKey", detached);
            prop.onPickUpSwitch = Detach(prop.onPickUpSwitch, root, "onPickUpSwitch", detached);

            if (detached.Count > 0)
                Plugin.Log.LogInfo(
                    $"[{nameof(ReceivedItemSpawner)}] Clone let go of the puzzle's own switches: {string.Join(", ", detached)}.");
        }

        private static PeckSwitch Detach(PeckSwitch peck, Transform root, string field, List<string> detached)
        {
            if (peck == null || peck.transform.IsChildOf(root))
                return peck;

            detached.Add($"{field} -> '{peck.gameObject.name}'");
            return null;
        }

        private static Transform _templateHolder;

        // An inactive, never-destroyed parent for clones in the making.
        private static Transform TemplateHolder()
        {
            if (_templateHolder == null)
            {
                var holder = new GameObject("AP clone holder");
                holder.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(holder);
                _templateHolder = holder.transform;
            }

            return _templateHolder;
        }

        private static RewardGourd CreateNeutralizedClone(Vector3 position, Quaternion rotation)
        {
            var rewardGourd = BuildNeutralizedClone(position, rotation);
            if (rewardGourd == null)
                return null;

            // The assetId overload: NetworkIdentity.assetId has an internal setter.
            NetworkServer.Spawn(rewardGourd.gameObject, CosmeticAssetId);

            // Now that it has a netId, its real colour.
            ReapplyCosmeticLook(rewardGourd);

            // The netId, to line this up with the other player's log.
            var identity = rewardGourd.GetComponent<NetworkIdentity>();
            Plugin.Log.LogInfo(
                $"[{nameof(ReceivedItemSpawner)}] Cosmetic gourd spawned on the network (netId {(identity != null ? identity.netId : 0)}).");

            return rewardGourd;
        }

        internal static bool IsCosmeticClone(Prop prop)
        {
            return prop != null
                && prop.saveablePropName == SaveablePropName.notSavable
                && prop.gameObject.name.Contains(CosmeticNameSuffix, StringComparison.Ordinal);
        }

        // PropHome also serves carried slots like belts; monument slots all start with "monoument".
        internal static bool IsMonumentHome(PropHome home)
        {
            return home != null
                && home.saveableHomeName.ToString().StartsWith("monoument", StringComparison.OrdinalIgnoreCase);
        }

        // Internal: GadgetItemSpawner reuses this reflection.
        internal static MethodInfo GetPrivatePropertySetter<T>(string propertyName)
        {
            return typeof(T)
                .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetSetMethod(nonPublic: true);
        }

        internal static void NeutralizeProgression(Prop prop)
        {
            if (prop == null)
                return;

            prop.saveablePropName = SaveablePropName.notSavable;
            prop.propSaveType = PropSaveType.Never;

            // Without this, Prop.Start() teleports the clone to the template's original slot.
            prop.startHome = null;
        }

        // Cached: a scene-wide scan per spawn stuttered during the session-start restore.
        private static RewardGourd _cachedTemplate;

        private static RewardGourd FindTemplate()
        {
            if (_cachedTemplate != null && _cachedTemplate.prop != null)
                return _cachedTemplate;

            var all = UnityEngine.Object.FindObjectsByType<RewardGourd>(FindObjectsSortMode.None);
            if (all == null || all.Length == 0)
                return null;

            // Only a puzzle gourd the registry knows: other gourds have a look of their own.
            foreach (var candidate in all)
            {
                if (IsPlainPuzzleGourd(candidate) && candidate.gourdState == GourdFlag.GourdState.Loose)
                {
                    _cachedTemplate = candidate;
                    LogTemplate(candidate);
                    return candidate;
                }
            }

            foreach (var candidate in all)
            {
                if (IsPlainPuzzleGourd(candidate))
                {
                    _cachedTemplate = candidate;
                    LogTemplate(candidate);
                    return candidate;
                }
            }

            return null;
        }

        // A clone carries its template's components, and one may still answer to its puzzle.
        private static void LogTemplate(RewardGourd template)
        {
            var name = $"'{template.gameObject.name}' ({template.prop.saveablePropName})";
            Plugin.Log.LogInfo($"[{nameof(ReceivedItemSpawner)}] Cosmetic gourds are cloned from {name}.");
        }

        private static bool IsPlainPuzzleGourd(RewardGourd candidate)
        {
            return candidate != null && candidate.prop != null
                && !candidate.gameObject.name.Contains(CosmeticNameSuffix, StringComparison.Ordinal)
                && GourdRegistry.TryGetLocationId(candidate.prop.saveablePropName, out _);
        }
    }
}
