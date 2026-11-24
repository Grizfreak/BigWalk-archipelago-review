using System;
using System.Collections.Generic;
using System.Linq;
using BigWalkArchipelago.Core.Net;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // The Silent Gauntlet's extent: boxes around the renderers under `SilentGauntlet <n>Player`.
    internal static class Gauntlet
    {
        private const string RootPrefix = "SilentGauntlet";
        private const float Margin = 6f;
        private const float MaxBoxSize = 200f;

        // The Gauntlet's parts are spread over the island, so it gets a box per part.
        private static readonly System.Collections.Generic.List<Bounds> _boxes = new();
        private static bool _looked;

        internal static bool Contains(Vector3 point)
        {
            if (!_looked)
                Find();
            foreach (var box in _boxes)
            {
                if (box.Contains(point))
                    return true;
            }

            return false;
        }

        internal static void Forget()
        {
            _boxes.Clear();
            _looked = false;
        }

        // A particle system's bounds sit at the origin until it has played: not part of it.
        private static bool IsParticle(Renderer renderer) =>
            renderer.GetIl2CppType().Name == "ParticleSystemRenderer";

        private static bool Measure(Transform branch, out Bounds bounds)
        {
            bounds = default;
            var any = false;
            foreach (var renderer in branch.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || IsParticle(renderer))
                    continue;
                if (!any)
                    bounds = renderer.bounds;
                else
                    bounds.Encapsulate(renderer.bounds);
                any = true;
            }

            return any;
        }

        private static float Widest(Bounds bounds) =>
            Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));

        private static void Collect(Transform branch)
        {
            if (!Measure(branch, out var bounds))
                return;
            if (Widest(bounds) <= MaxBoxSize)
            {
                bounds.Expand(Margin * 2f);
                _boxes.Add(bounds);
                Plugin.Log.LogInfo($"[{nameof(Gauntlet)}]   box '{branch.name}': {bounds.size.x:F0} x {bounds.size.y:F0} x {bounds.size.z:F0} m around {bounds.center}.");
                return;
            }

            Plugin.Log.LogInfo($"[{nameof(Gauntlet)}]   '{branch.name}' is {bounds.size.x:F0} x {bounds.size.y:F0} x {bounds.size.z:F0} m: split into its children.");
            if (branch.childCount == 0)
            {
                Plugin.Log.LogInfo($"[{nameof(Gauntlet)}]   dropped '{branch.name}', a single renderer too wide.");
                return;
            }

            // Its own renderers (not a child's) count one by one.
            foreach (var renderer in branch.GetComponents<Renderer>())
            {
                if (IsParticle(renderer))
                    continue;
                var own = renderer.bounds;
                if (Widest(own) <= MaxBoxSize)
                {
                    own.Expand(Margin * 2f);
                    _boxes.Add(own);
                }
                else
                    Plugin.Log.LogInfo($"[{nameof(Gauntlet)}]   dropped the renderer of '{branch.name}', too wide.");
            }

            for (var i = 0; i < branch.childCount; i++)
                Collect(branch.GetChild(i));
        }

        private static void Find()
        {
            if (!WorldManager.isReadyForEffects)
                return;

            _looked = true;
            try
            {
                Transform root = null;
                foreach (var state in UnityEngine.Object.FindObjectsByType<TrackedPeckState>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    for (var t = state != null ? state.transform : null; t != null; t = t.parent)
                    {
                        if (t.name.StartsWith(RootPrefix, StringComparison.Ordinal))
                        {
                            root = t;
                            break;
                        }
                    }

                    if (root != null)
                        break;
                }

                if (root == null)
                {
                    Plugin.Log.LogInfo($"[{nameof(Gauntlet)}] No '{RootPrefix}' object in this world.");
                    return;
                }

                _boxes.Clear();
                Plugin.Log.LogInfo($"[{nameof(Gauntlet)}] '{root.name}':");
                Collect(root);
                Plugin.Log.LogInfo($"[{nameof(Gauntlet)}] {_boxes.Count} box(es) in all.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(Gauntlet)}] Could not measure the Gauntlet: {ex.Message}");
            }
        }
    }
}
