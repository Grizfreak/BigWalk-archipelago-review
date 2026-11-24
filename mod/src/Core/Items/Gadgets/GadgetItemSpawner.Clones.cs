using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Items
{
    internal static partial class GadgetItemSpawner
    {
        // Points the tomato's tick and pop at the clone's own state before the clone wakes.
        private static void WireTomatoEffects(GameObject clone)
        {
            var own = clone.GetComponent<TrackedPeckState>();
            if (own == null)
                return;

            var notes = new List<string>();
            foreach (var audio in clone.GetComponentsInChildren<PeckEffectAudio>(true))
            {
                var reference = audio.systemReference;
                notes.Add($"audio listened to {(reference.peckSystem == null ? "nothing" : reference.peckSystem == own ? "the clone" : reference.peckSystem.gameObject.name)}");
                reference.peckSystem = own;
                audio.systemReference = reference;

                // Played at the tomato: a guest gets the state change without the context's prop.
                var where = audio.transformReference;
                where.referenceType = PeckTransformReference.ReferenceType.Custom;
                where.customTransform = clone.transform;
                audio.transformReference = where;
            }

            foreach (var particle in clone.GetComponentsInChildren<PeckEffectParticleNetworked>(true))
            {
                var reference = particle.systemReference;
                notes.Add($"pop listened to {(reference.peckSystem == null ? "nothing" : reference.peckSystem == own ? "the clone" : reference.peckSystem.gameObject.name)}");
                reference.peckSystem = own;
                particle.systemReference = reference;
            }

            Plugin.Log.LogInfo($"[{nameof(GadgetItemSpawner)}] Tomato clone: {string.Join(", ", notes)}; all on the clone now.");
        }

        // Builds the clone without spawning it, so GadgetSpawnHandler can do the same on a client.
        internal static Prop BuildNeutralizedClone(GadgetKind kind, Vector3 position, Quaternion rotation, int index = 0)
        {
            var template = ResolveTemplate(kind, index, out var extra);
            if (template == null)
            {
                // A guest gets Mirror's spawn wave before the world is ready, so capture now.
                TryCaptureLate(kind);
                template = ResolveTemplate(kind, index, out extra);
            }

            if (template == null)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(GadgetItemSpawner)}] No {kind} template captured yet, cosmetic spawn skipped.");
                return null;
            }

            // Instantiate does not carry a MaterialPropertyBlock over.
            var templatePropertyBlocks = ReceivedItemSpawner.CapturePropertyBlocks(template.gameObject);

            var clone = UnityEngine.Object.Instantiate(template.gameObject, position, rotation);
            clone.name = $"{PrefabNames[kind]} {ReceivedItemSpawner.CosmeticNameSuffix}";
            ReceivedItemSpawner.ApplyPropertyBlocks(clone, templatePropertyBlocks);

            // Before activation: tickets register in OnEnable, and a refused one never asks again.
            if (extra)
                AssignExtraTickets(clone, kind, index);

            var cloneIdentity = clone.GetComponent<NetworkIdentity>();
            if (cloneIdentity != null)
            {
                cloneIdentity.sceneId = 0;
                ReceivedItemSpawner.GetPrivatePropertySetter<NetworkIdentity>("hasSpawned")
                    ?.Invoke(cloneIdentity, new object[] { false });
            }

            var prop = clone.GetComponent<Prop>();
            if (prop == null)
            {
                Plugin.Log.LogWarning($"[{nameof(GadgetItemSpawner)}] Clone without a Prop, destroying and aborting.");
                UnityEngine.Object.Destroy(clone);
                return null;
            }

            ReceivedItemSpawner.NeutralizeProgression(prop);
            StartLooseNotHung(clone);

            // Big Mask's helmet cannot be taken off by anyone.
            if (kind == GadgetKind.Blindfold)
                prop.blockRemovingFromHomes = true;

            // The tomato timer is local (PeckEffectTimer), so every machine's clone is set here.
            if (kind == GadgetKind.Pomodoro)
            {
                foreach (var timer in clone.GetComponentsInChildren<PeckEffectTimer>(true))
                    timer.duration = TomatoSeconds;
                WireTomatoEffects(clone);
            }

            clone.SetActive(true);

            if (cloneIdentity != null)
                ReceivedItemSpawner.GetPrivatePropertySetter<NetworkIdentity>(nameof(NetworkIdentity.SpawnedFromInstantiate))
                    ?.Invoke(cloneIdentity, new object[] { false });

            // After activation: something re-enables the materialless renderer on wake.
            ReceivedItemSpawner.ClearLightmapReferences(clone);
            ReceivedItemSpawner.FixMaterialessRenderers(clone);
            ReceivedItemSpawner.RefreshPropertyBlockHelpers(clone);
            SettleOffTheHanger(clone);

            // After the helpers' refresh, which would put a buoy's tint back.
            if (kind == GadgetKind.Lamp || FlareGuns.Contains(kind))
                ColourPainter.PaintClone(kind, clone, kind == GadgetKind.RainbowFlareGun);

            return prop;
        }

        // A clone copies a template still on its hanger, so its states are reset to lying loose.
        private static readonly Dictionary<string, int> LooseStates = new()
        {
            { "placed on hanger", 0 },
            { "backpack not worn", 1 },
            { "backpackWornSystem", 0 },
        };

        // Save identity removed, or the clone reads and writes the vanilla prop's saved states.
        private static void StartLooseNotHung(GameObject clone)
        {
            foreach (var state in clone.GetComponentsInChildren<TrackedPeckState>(true))
            {
                if (state == null)
                    continue;

                state.saveIdentity = null;

                if (state.label == null || !LooseStates.TryGetValue(state.label, out var value))
                    continue;

                try
                {
                    var context = state.currentPeckContext;
                    context.state = value;
                    state.currentPeckContext = context;

                    var predicted = state.peckContextForPrediction;
                    predicted.state = value;
                    state.peckContextForPrediction = predicted;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(GadgetItemSpawner)}] Could not set '{state.label}' on a cloned {clone.name}: {ex.Message}");
                }
            }
        }

        // The server holds the borrowed ticket's last state, so the host publishes the loose ones.
        private static void PublishLooseStates(GameObject clone)
        {
            if (!NetworkServer.active)
                return;

            foreach (var state in clone.GetComponentsInChildren<TrackedPeckState>(true))
            {
                if (state == null || state.label == null || !LooseStates.TryGetValue(state.label, out var value))
                    continue;

                try
                {
                    state.SetState(value);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(GadgetItemSpawner)}] Could not publish '{state.label}' for {clone.name}: {ex.Message}");
                }
            }
        }

        private static void SettleOffTheHanger(GameObject clone)
        {
            foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true))
            {
                var hung = renderer != null ? renderer.gameObject : null;
                if (hung == null || !hung.name.EndsWith("_hung", StringComparison.Ordinal) || hung.transform.parent == null)
                    continue;

                var closedName = hung.name.Substring(0, hung.name.Length - "_hung".Length) + "_closed";
                var closed = hung.transform.parent.Find(closedName);
                if (closed == null)
                    continue;

                hung.SetActive(false);
                closed.gameObject.SetActive(true);
            }
        }

        // Never null: an unresolved netId breaks that player's network state for the session.
        internal static Prop BuildStandInClone(GadgetKind wanted, Vector3 position, Quaternion rotation)
        {
            foreach (var kind in StandInOrder())
            {
                if (kind == wanted || GetTemplate(kind) == null)
                    continue;

                var prop = BuildNeutralizedClone(kind, position, rotation);
                if (prop == null)
                    continue;

                Plugin.Log.LogWarning(
                    $"[{nameof(GadgetItemSpawner)}] Nothing to build an incoming {wanted} from; stood a {kind} in "
                    + "its place so the spawn resolves. It will look wrong for this player.");
                return prop;
            }

            return null;
        }

        private static IEnumerable<GadgetKind> StandInOrder()
        {
            // The Lamp first: the one kind the hiding pass spares.
            yield return GadgetKind.Lamp;

            foreach (var kind in PrefabNames.Keys)
                yield return kind;
        }

        // Just a NetworkIdentity, so Mirror has a netId to resolve.
        internal static GameObject BuildResolvablePlaceholder(GadgetKind wanted, Vector3 position)
        {
            try
            {
                var placeholder = new GameObject($"{wanted} {PlaceholderNameSuffix}");
                placeholder.transform.position = position;
                placeholder.AddComponent<NetworkIdentity>();

                Plugin.Log.LogWarning(
                    $"[{nameof(GadgetItemSpawner)}] Nothing at all to build an incoming {wanted} from, not even a "
                    + "stand-in; spawned an empty placeholder so this client's network state stays readable.");
                return placeholder;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(GadgetItemSpawner)}] Could not even place a placeholder for an incoming {wanted}: {ex.Message}");
                return null;
            }
        }
    }
}
