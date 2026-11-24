using System;
using System.Collections.Generic;
using System.Reflection;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Spawns cosmetic gourds for received items: neutralized clones of a scene gourd.
    internal static class ReceivedItemSpawner
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

        // Picked by netId, the same on every machine. Channels stay under 0xFF, which blooms white.
        private static readonly string[] CosmeticGourdColorsHtml =
        {
            "#C83C3C", // red
            "#D2782D", // orange
            "#D2B42D", // yellow
            "#50A046", // green
            "#3C78C8", // blue
            "#8C50B4", // purple
        };

        private static void ApplyCosmeticColor(RewardGourd rewardGourd)
        {
            // netId is 0 before NetworkServer.Spawn; the spawn path re-applies once it exists.
            var identity = rewardGourd.GetComponent<NetworkIdentity>();
            var netId = identity != null ? identity.netId : 0u;
            var html = CosmeticGourdColorsHtml[(int)(netId % (uint)CosmeticGourdColorsHtml.Length)];
            if (!ColorUtility.TryParseHtmlString(html, out var color))
                return;

            LogColorSettingsOnce(rewardGourd);

            rewardGourd.isVariantChallenge = true;
            rewardGourd.variantChallengeColor = color;
            WriteHelperColor(rewardGourd, color);
            _pendingColorRefresh = rewardGourd;
        }

        // Writes where RewardGourd.Awake writes: Refresh() pushes colorSettings, not the field.
        private static void WriteHelperColor(RewardGourd rewardGourd, Color color)
        {
            var helper = rewardGourd.propertyBlockHelper;
            var settings = helper != null ? helper.colorSettings : null;
            if (settings == null || settings.Length == 0)
                return;

            // An element of an IL2CPP struct array comes back as a copy: changed, then stored back.
            var setting = settings[0];
            setting.color = color;
            settings[0] = setting;

            // Read back once per session: a write through the interop indexer could silently fail.
            if (!_helperWriteChecked && rewardGourd.GetComponent<NetworkIdentity>()?.netId > 0)
            {
                _helperWriteChecked = true;
                var now = settings[0].color;
                Plugin.Log.LogInfo(
                    $"[{nameof(ReceivedItemSpawner)}] Gourd colour written to '{settings[0].propertyName}': asked {color}, "
                    + $"the helper now holds {now} ({(now == color ? "took" : "DID NOT TAKE")}).");
            }
        }

        private static bool _helperWriteChecked;

        // Applied after activation: Awake may push variantChallengeColor; if not, this does.
        private static void RefreshCosmeticColor()
        {
            var target = _pendingColorRefresh;
            _pendingColorRefresh = null;
            if (target == null)
                return;

            try
            {
                target.propertyBlockHelper?.Refresh();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ReceivedItemSpawner)}] Could not refresh the gourd's colour: {ex.Message}");
            }
        }

        // On a client, Mirror's spawn payload runs after the handler and restores the old look.
        internal static void ReapplyCosmeticLook(RewardGourd rewardGourd)
        {
            if (rewardGourd == null)
                return;

            ApplyCosmeticColor(rewardGourd);
            RefreshCosmeticColor();
        }

        // Logged once per session: the shader properties the game drives, for the next fix.
        private static void LogColorSettingsOnce(RewardGourd rewardGourd)
        {
            if (_colorSettingsLogged)
                return;

            _colorSettingsLogged = true;

            var helper = rewardGourd.propertyBlockHelper;
            if (helper == null)
            {
                Plugin.Log.LogInfo($"[{nameof(ReceivedItemSpawner)}] Cloned gourd has no PropertyBlockHelper.");
                return;
            }

            var settings = helper.colorSettings;
            if (settings == null || settings.Length == 0)
            {
                Plugin.Log.LogInfo($"[{nameof(ReceivedItemSpawner)}] PropertyBlockHelper exposes no colour setting.");
                return;
            }

            for (var i = 0; i < settings.Length; i++)
                Plugin.Log.LogInfo(
                    $"[{nameof(ReceivedItemSpawner)}] colorSettings[{i}] '{settings[i].propertyName}' = {settings[i].color}");
        }

        private static RewardGourd _pendingColorRefresh;
        private static bool _colorSettingsLogged;

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

        // Instantiate does not copy a per-instance MaterialPropertyBlock: clones went magenta.
        internal static MaterialPropertyBlock[] CapturePropertyBlocks(GameObject source)
        {
            var renderers = source.GetComponentsInChildren<Renderer>(true);
            var blocks = new MaterialPropertyBlock[renderers.Length];
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;

                var block = new MaterialPropertyBlock();
                renderers[i].GetPropertyBlock(block);
                blocks[i] = block;
            }

            return blocks;
        }

        // By index: valid only on an exact copy of the source, as Instantiate makes.
        internal static void ApplyPropertyBlocks(GameObject target, MaterialPropertyBlock[] blocks)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length && i < blocks.Length; i++)
            {
                if (renderers[i] != null && blocks[i] != null)
                    renderers[i].SetPropertyBlock(blocks[i]);
            }
        }

        // A static prop's baked lightmap index goes stale on a kept template; -1 uses light probes.
        internal static void ClearLightmapReferences(GameObject target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;

                renderer.lightmapIndex = -1;
                renderer.realtimeLightmapIndex = -1;
            }
        }

        // A sub-renderer without a material draws magenta: it borrows a sibling's material.
        internal static void FixMaterialessRenderers(GameObject target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true);

            Material fallback = null;
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    fallback = renderer.sharedMaterial;
                    break;
                }
            }

            if (fallback == null)
                return;

            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.sharedMaterial == null)
                    renderer.sharedMaterial = fallback;
            }
        }

        // A copy points at a PropertyBlockHelper's material, which the game later destroys.
        internal static void OwnMaterials(GameObject target)
        {
            foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                var materials = renderer.sharedMaterials;
                var changed = false;
                for (var i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                        continue;

                    materials[i] = new Material(materials[i]);
                    changed = true;
                }

                if (changed)
                    renderer.sharedMaterials = materials;
            }
        }

        internal static void RefreshPropertyBlockHelpers(GameObject target)
        {
            var helpers = target.GetComponentsInChildren<PropertyBlockHelper>(true);
            foreach (var helper in helpers)
                helper?.Refresh();
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

        // Pending hand-overs: items may go to different players in the same tick.
        private static readonly List<PendingHandover> Pending = new();

        private sealed class PendingHandover
        {
            internal Prop Prop;
            internal PlayerCharacter Recipient;
            internal float NotBefore;
            internal bool DropAsked;
        }

        // The drop has to reach the player's machine first, or the new hold lands in full hands.
        private const float DropSettleSeconds = 0.5f;

        internal static void QueueHandover(Prop prop, PlayerCharacter recipient)
        {
            if (prop == null || recipient == null)
                return;

            if (!ModConfig.PutGourdInHands.Value)
            {
                ItemRecipients.Settled(recipient);
                return;
            }

            Pending.Add(new PendingHandover { Prop = prop, Recipient = recipient, NotBefore = Time.time });
        }

        // Host only, at least one frame after the spawn.
        internal static void DrainPendingHandover()
        {
            using var activity = ModActivity.Enter("handover");
            for (var i = Pending.Count - 1; i >= 0; i--)
            {
                var entry = Pending[i];
                if (Time.time < entry.NotBefore)
                    continue;

                if (entry.Prop == null || entry.Recipient == null || entry.Recipient.hands == null)
                {
                    ItemRecipients.Settled(entry.Recipient);
                    Pending.RemoveAt(i);
                    continue;
                }

                // All hands were full: this player lets go first, once, and gets it after.
                var held = entry.Recipient.hands.heldProp;
                if (held != null && held != entry.Prop && !entry.DropAsked)
                {
                    ReleaseFromHands(held);
                    entry.DropAsked = true;
                    entry.NotBefore = Time.time + DropSettleSeconds;
                    Plugin.Log.LogInfo(
                        $"[{nameof(ReceivedItemSpawner)}] {entry.Recipient.gameObject.name} had full hands; "
                        + "they drop what they held and take the new item next.");
                    continue;
                }

                Pending.RemoveAt(i);
                ItemRecipients.Settled(entry.Recipient);

                var identity = entry.Prop.GetComponent<NetworkIdentity>();
                if (TryPutInHands(entry.Prop, entry.Recipient))
                    Plugin.Log.LogInfo(
                        $"[{nameof(ReceivedItemSpawner)}] Item handed to {entry.Recipient.gameObject.name} "
                        + $"(netId {(identity != null ? identity.netId : 0)}).");
            }
        }

        internal static void ForgetPendingHandovers()
        {
            Pending.Clear();
            ItemRecipients.Forget();
        }

        // Server side only: the hold is a server SyncVar, and a client PickUp only fools itself.
        private static bool TryPutInHands(Prop prop, PlayerCharacter recipient)
        {
            if (prop == null || recipient == null || !NetworkServer.active)
                return false;

            try
            {
                return TryGiveTo(recipient, prop);
            }
            catch (Exception ex)
            {
                // Cosmetic: the item exists and can be picked up, so failing costs nothing.
                Plugin.Log.LogWarning($"[{nameof(ReceivedItemSpawner)}] Could not hand the item over: {ex.Message}");
                return false;
            }
        }

        // PickUp only acts locally; the hold must also be published on the network.
        private static bool TryGiveTo(PlayerCharacter player, Prop prop)
        {
            var hands = player != null ? player.hands : null;
            if (hands == null || hands.isHoldingSomething || !hands.IsSafeToPickUp(prop))
                return false;

            // Local effects, on this machine.
            hands.PickUp(prop);

            // The game's CmdPickUp sets NetworkplayerHeldInformation on the server, so does this.
            try
            {
                // Numbered past the player's last action: OnSetHeld drops a value below its latest.
                var networking = player.playerNetworking;
                var hold = new PlayerHeldInformation(prop);
                hold.actionNumber = networking.playerHeldInformation.actionNumber + 1;
                networking.NetworkplayerHeldInformation = hold;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ReceivedItemSpawner)}] Could not publish the hold to the other players: {ex.Message}");
            }

            if (!HoldIsPublished(player, prop))
                Plugin.Log.LogWarning(
                    $"[{nameof(ReceivedItemSpawner)}] The hold did not take on the network; the other player will not see this gourd in anyone's hands.");

            return true;
        }

        // A late guest can get a hold before its prop; a fresh number makes it take the hold again.
        internal static void RepublishHolds()
        {
            if (!NetworkServer.active)
                return;

            var all = PlayerCharacter.allPlayerCharacters;
            if (all == null)
                return;

            foreach (var player in all)
            {
                try
                {
                    var networking = player != null ? player.playerNetworking : null;
                    if (networking == null)
                        continue;

                    var held = networking.playerHeldInformation;
                    if (held.identity == null)
                        continue;

                    held.actionNumber = held.actionNumber + 1;
                    networking.NetworkplayerHeldInformation = held;
                    Plugin.Log.LogInfo(
                        $"[{nameof(ReceivedItemSpawner)}] Hold of '{player.name}' said again for a guest that joined.");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(ReceivedItemSpawner)}] Could not say a hold again: {ex.Message}");
                }
            }
        }

        // "Published" means the networked field: the only one another machine can read.
        private static bool HoldIsPublished(PlayerCharacter player, Prop prop)
        {
            try
            {
                var networking = player.playerNetworking;
                var identity = prop.GetComponent<NetworkIdentity>();
                if (networking == null || identity == null)
                    return false;

                var held = networking.playerHeldInformation;
                return held.identity != null && held.identity == identity;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ReceivedItemSpawner)}] Could not read the published hold: {ex.Message}");
                return false;
            }
        }

        private static PlayerCharacter FindLocalPlayerCharacter()
        {
            var all = PlayerCharacter.allPlayerCharacters;
            if (all == null)
                return null;

            foreach (var pc in all)
            {
                if (pc != null && pc.isLocalPlayer)
                    return pc;
            }

            return null;
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
