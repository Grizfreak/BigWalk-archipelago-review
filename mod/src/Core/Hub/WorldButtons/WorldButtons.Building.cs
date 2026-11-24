using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Core.Hub
{
    internal partial class WorldButtons
    {
        // Everything drawn but the post, which is not the button.
        private static void Measure(Button button)
        {
            var any = false;
            var bounds = new Bounds(button.Position + Vector3.up * 1f, Vector3.zero);
            foreach (var renderer in button.Root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer.gameObject.name == "post")
                    continue;

                if (!any)
                    bounds = renderer.bounds;
                else
                    bounds.Encapsulate(renderer.bounds);
                any = true;
            }

            button.Bounds = bounds;
            button.Centre = any ? bounds.center : button.Position + Vector3.up * 1f;
            // Some slack around the drawn box, so a button is easy to aim at.
            button.Radius = Mathf.Max(0.45f, bounds.extents.magnitude + 0.2f);
        }

        // Only the game button's meshes and materials: no network reference or press logic.
        private static GameObject Build(Vector3 position, Quaternion rotation, int index, Button button)
        {
            var root = new GameObject("AP world button");
            root.transform.SetPositionAndRotation(position, rotation);

            if (TryBuildNative(root, index, button))
            {
                button.Native = true;
                return root;
            }

            if (TryBuildFromGameButton(root))
            {
                AddPost(root);
                return root;
            }

            return BuildFromPrimitives(root);
        }

        private const float PostHeight = 1.0f;

        private static void AddPost(GameObject root)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "post";
            post.transform.SetParent(root.transform, false);
            post.transform.localPosition = new Vector3(0f, PostHeight * 0.5f - 0.1f, 0f);
            post.transform.localScale = new Vector3(0.14f, PostHeight * 0.5f - 0.05f, 0.14f);
            Paint(post, new Color(0.12f, 0.2f, 0.17f));
        }

        private static bool TryBuildFromGameButton(GameObject root)
        {
            var template = FindPushButton(root.transform.position);
            if (template == null)
                return false;

            var made = 0;
            foreach (var source in template.GetComponentsInChildren<MeshRenderer>(true))
            {
                var filter = source != null ? source.GetComponent<MeshFilter>() : null;
                if (filter == null || filter.sharedMesh == null)
                    continue;

                var part = new GameObject(source.gameObject.name);
                part.transform.SetParent(root.transform, false);
                part.transform.localPosition = template.transform.InverseTransformPoint(source.transform.position)
                                               + Vector3.up * PostHeight;
                part.transform.localRotation = Quaternion.Inverse(template.transform.rotation) * source.transform.rotation;
                part.transform.localScale = source.transform.lossyScale;

                part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var renderer = part.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = source.sharedMaterials;

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_TintColor", new Color(0.23f, 0.81f, 0.39f));
                block.SetColor("_Color", new Color(0.23f, 0.81f, 0.39f));
                block.SetColor("_BaseColor", new Color(0.23f, 0.81f, 0.39f));
                renderer.SetPropertyBlock(block);
                made++;
            }

            return made > 0;
        }

        // No colliders, so nothing catches on them.
        private static GameObject BuildFromPrimitives(GameObject root)
        {

            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "post";
            post.transform.SetParent(root.transform, false);
            post.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            post.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);

            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = "cap";
            cap.transform.SetParent(root.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            cap.transform.localScale = new Vector3(0.28f, 0.06f, 0.28f);

            Paint(post, new Color(0.16f, 0.28f, 0.24f));
            Paint(cap, new Color(0.23f, 0.81f, 0.39f));
            return root;
        }
    }
}
