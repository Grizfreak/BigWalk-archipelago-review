using System;
using System.Collections.Generic;
using System.Reflection;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Items
{
    internal static partial class ReceivedItemSpawner
    {
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
    }
}
