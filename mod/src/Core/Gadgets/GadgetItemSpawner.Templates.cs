using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal static partial class GadgetItemSpawner
    {
        // Templates are never spawned over the network, so they get a suffix of their own.
        private const string TemplateNameSuffix = "(AP template)";
        private const string PlaceholderNameSuffix = "(AP placeholder)";

        // In savablePropGuid order, so indices line up on every machine.
        private static readonly Dictionary<GadgetKind, List<Prop>> _templates = new();

        // Every machine, once per world load: each clones its own templates from its own scene.
        internal static void CaptureTemplates()
        {
            foreach (var kind in PrefabNames.Keys)
                CaptureTemplate(kind);
        }

        private static void CaptureTemplate(GadgetKind kind)
        {
            var prefabName = PrefabNames[kind];

            // Inactive ones count: on a guest the hidden props are never switched on.
            var all = UnityEngine.Object.FindObjectsByType<Prop>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all == null)
                return;

            var candidates = new List<Prop>();
            foreach (var candidate in all)
            {
                if (candidate == null || candidate.gameObject == null)
                    continue;

                if (!MatchesKind(kind, candidate.gameObject.name) || IsOurs(candidate.gameObject.name))
                    continue;

                candidates.Add(candidate);
            }

            if (candidates.Count == 0)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(GadgetItemSpawner)}] No {kind} ({prefabName}) found in the scene to capture as a template; a received one will have nothing to clone from.");
                return;
            }

            // Sorted by guid: FindObjectsByType has no order and host and clients must index alike.
            candidates.Sort(CompareByGuid);
            if (candidates.Count > MaxInstancesPerKind)
                candidates.RemoveRange(MaxInstancesPerKind, candidates.Count - MaxInstancesPerKind);

            var templates = new List<Prop>();
            foreach (var candidate in candidates)
            {
                var template = BuildTemplate(candidate, prefabName);
                if (template != null)
                    templates.Add(template);
            }

            // Re-armed on every world load, so the previous world's templates would leak.
            if (_templates.TryGetValue(kind, out var previous) && previous != null)
            {
                foreach (var old in previous)
                {
                    if (old != null && old.gameObject != null)
                        UnityEngine.Object.Destroy(old.gameObject);
                }
            }

            _templates[kind] = templates;
            Plugin.Log.LogInfo(
                $"[{nameof(GadgetItemSpawner)}] {kind}: {templates.Count} template(s) captured, one per vanilla "
                + $"'{prefabName}'{(AlternatePrefabNames.ContainsKey(kind) ? " or its alternates" : string.Empty)}.");
        }

        private static int CompareByGuid(Prop a, Prop b)
        {
            var byGuid = string.CompareOrdinal(a.savablePropGuid ?? string.Empty, b.savablePropGuid ?? string.Empty);
            return byGuid != 0 ? byGuid : string.CompareOrdinal(a.gameObject.name, b.gameObject.name);
        }

        private static Prop BuildTemplate(Prop candidate, string prefabName)
        {
            // Instantiate drops the MaterialPropertyBlock; without it the template turns pink.
            var propertyBlocks = ReceivedItemSpawner.CapturePropertyBlocks(candidate.gameObject);

            // Inactive before Instantiate, so the clone's Awake and OnEnable never run.
            var templateWasActive = candidate.gameObject.activeSelf;
            GameObject clone;
            try
            {
                candidate.gameObject.SetActive(false);
                clone = UnityEngine.Object.Instantiate(candidate.gameObject);
            }
            finally
            {
                if (templateWasActive)
                    candidate.gameObject.SetActive(true);
            }

            clone.name = $"{prefabName} {TemplateNameSuffix}";
            ReceivedItemSpawner.ApplyPropertyBlocks(clone, propertyBlocks);
            ReceivedItemSpawner.ClearLightmapReferences(clone);
            ReceivedItemSpawner.FixMaterialessRenderers(clone);
            ReceivedItemSpawner.RefreshPropertyBlockHelpers(clone);
            clone.SetActive(false);

            var identity = clone.GetComponent<NetworkIdentity>();
            if (identity != null)
                identity.sceneId = 0;

            return clone.GetComponent<Prop>();
        }

        private static int VanillaCount(GadgetKind kind)
        {
            return _templates.TryGetValue(kind, out var list) && list != null ? list.Count : 0;
        }

        // None for a kind the island keeps: its vanilla tickets are always taken.
        private static int VanillaUsable(GadgetKind kind)
        {
            return LendsNoTickets(kind) ? 0 : VanillaCount(kind);
        }

        // An extra slot walks the templates in turn, so received lamps get the island's colours.
        private static Prop ResolveTemplate(GadgetKind kind, int index, out bool extra)
        {
            var count = VanillaCount(kind);
            var usable = VanillaUsable(kind);
            extra = count > 0 && index >= usable;
            return GetTemplate(kind, extra ? (index - usable) % count : index);
        }

        private static Prop GetTemplate(GadgetKind kind, int index = 0)
        {
            if (!_templates.TryGetValue(kind, out var list) || list == null || list.Count == 0)
                return null;

            if (index < 0 || index >= list.Count)
            {
                // Not expected, every machine loads the same scene; wrong tickets beat no clone.
                Plugin.Log.LogWarning(
                    $"[{nameof(GadgetItemSpawner)}] Asked for {kind} instance {index}, but only {list.Count} were captured here; using the first.");
                index = 0;
            }

            var template = list[index];
            return template != null ? template : null;
        }

        private static float _nextLateCapture;

        private const float LateCaptureCooldownSeconds = 1f;

        // Captures every kind at once: a spawn wave comes in one burst, so one scan serves it.
        private static void TryCaptureLate(GadgetKind kind)
        {
            if (Time.time < _nextLateCapture)
                return;

            _nextLateCapture = Time.time + LateCaptureCooldownSeconds;

            Plugin.Log.LogInfo(
                $"[{nameof(GadgetItemSpawner)}] An incoming {kind} arrived before the world-ready sweep; capturing "
                + "the templates now.");
            CaptureTemplates();
        }
    }
}
