using UnityEngine;

namespace BigWalkArchipelago.Core.Hub
{
    internal partial class WorldButtons
    {
        private const float IconGap = 0.06f;
        private const float IconScale = 0.6f;
        private const float IconSink = 0.10f;

        // A picture rebuilt from the Black Tower's EndingGateIndicator meshes, no components.
        private static void AddIcon(Button button, string tower)
        {
            try
            {
                var container = FindIndicatorContainer();
                if (container == null)
                {
                    Plugin.Log.LogInfo($"[{nameof(WorldButtons)}] No tower-icon panel is loaded to copy; '{button.Label}' has no icon.");
                    return;
                }

                var icon = new GameObject("AP world button icon");
                icon.transform.SetParent(button.Root.transform, false);
                icon.transform.localPosition = Vector3.zero;
                icon.transform.localRotation = Quaternion.identity;
                icon.transform.localScale = Vector3.one * IconScale;

                var made = 0;
                // All on the light bezel, where black is not lost.
                var sphere = tower is "gauntlet" or "drawbridge" or "dome";
                var beige = tower == "hub";
                var silhouette = beige ? "white" : tower;
                var bezel = "EndingGateIndicator_base_white";
                var names = sphere
                    ? new[] { bezel, "EndingGateIndicator_light" }
                    : new[] { bezel, "EndingGateIndicator_light", "EndingGateIndicator_tower_" + silhouette };
                Renderer bezelCopy = null;
                foreach (var name in names)
                {
                    var source = container.Find(name);
                    var filter = source != null ? source.GetComponent<MeshFilter>() : null;
                    var renderer = source != null ? source.GetComponent<MeshRenderer>() : null;
                    if (filter == null || filter.sharedMesh == null || renderer == null)
                        continue;

                    var part = new GameObject(name);
                    part.transform.SetParent(icon.transform, false);
                    part.transform.localPosition = container.InverseTransformPoint(source.position);
                    part.transform.localRotation = Quaternion.Inverse(container.rotation) * source.rotation;
                    part.transform.localScale = source.localScale;
                    part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                    var copy = part.AddComponent<MeshRenderer>();
                    copy.sharedMaterials = renderer.sharedMaterials;

                    // The game sets the silhouettes' colour at run time, so each copy gets its own.
                    if (name == bezel)
                        bezelCopy = copy;

                    if (!beige && tower != "black" && name.EndsWith("_tower_" + silhouette, StringComparison.Ordinal))
                    {
                        var block = new MaterialPropertyBlock();
                        copy.GetPropertyBlock(block);
                        var colour = IconColour(tower);
                        block.SetColor("_TintColor", colour);
                        block.SetColor("_Color", colour);
                        block.SetColor("_BaseColor", colour);
                        copy.SetPropertyBlock(block);
                    }

                    made++;
                }

                if (tower == "drawbridge" && bezelCopy != null)
                    AddDrawbridge(icon, bezelCopy.bounds, button.Root.transform.forward);
                else if (sphere && bezelCopy != null)
                    AddSphere(icon, bezelCopy.bounds, button.Root.transform.forward, PlateColour(tower));

                // Lifted by its measured size: the icon is bigger than the button.
                var drawn = false;
                var iconBounds = new Bounds(icon.transform.position, Vector3.zero);
                foreach (var renderer in icon.GetComponentsInChildren<Renderer>(true))
                {
                    if (!drawn)
                        iconBounds = renderer.bounds;
                    else
                        iconBounds.Encapsulate(renderer.bounds);
                    drawn = true;
                }

                if (drawn)
                    icon.transform.position += Vector3.up * (button.Bounds.max.y - iconBounds.min.y + IconGap);

                // Into the wall: the root faces out of it, so back is along its forward.
                icon.transform.position -= button.Root.transform.forward * IconSink;

                Plugin.Log.LogInfo($"[{nameof(WorldButtons)}] Icon '{tower}' for '{button.Label}': {made} of 3 part(s) copied.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(WorldButtons)}] No icon for '{button.Label}': {ex.Message}");
            }
        }

        // The panel has no drawbridge: its red bridge on two piers, in cubes.
        private static void AddDrawbridge(GameObject icon, Bounds bezel, Vector3 outward)
        {
            var width = Mathf.Max(bezel.size.x, bezel.size.z);
            var toIcon = 1f / Mathf.Max(0.0001f, icon.transform.lossyScale.x);
            var upright = Quaternion.LookRotation(outward, Vector3.up);
            var middle = bezel.center + outward * (width * 0.16f);
            var red = new Color(0.72f, 0.14f, 0.12f);
            var stone = new Color(0.38f, 0.31f, 0.20f);

            void Block(float x, float y, float sizeX, float sizeY, Color colour)
            {
                var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = "drawbridge part";
                block.transform.SetParent(icon.transform, false);
                block.transform.localScale = new Vector3(sizeX, sizeY, 0.20f) * (width * toIcon);
                var at = middle + upright * new Vector3(x, y, 0f) * width;
                block.transform.SetPositionAndRotation(at, upright);
                Paint(block, colour);
            }

            Block(-0.12f, -0.15f, 0.09f, 0.20f, stone);
            Block(0.12f, -0.15f, 0.09f, 0.20f, stone);
            Block(0f, -0.03f, 0.50f, 0.05f, red);
            Block(0f, 0.06f, 0.50f, 0.02f, red);
            Block(-0.24f, 0.015f, 0.02f, 0.09f, red);
            Block(0f, 0.015f, 0.02f, 0.09f, red);
            Block(0.24f, 0.015f, 0.02f, 0.09f, red);
        }

        private static void AddSphere(GameObject icon, Bounds bezel, Vector3 outward, Color colour)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "gauntlet sphere";
            sphere.transform.SetParent(icon.transform, false);
            var width = Mathf.Max(bezel.size.x, bezel.size.z);
            sphere.transform.localScale = Vector3.one * (width * 0.42f / Mathf.Max(0.0001f, icon.transform.lossyScale.x));
            // A little below the middle: centred, it ran into the light at the top.
            sphere.transform.position = bezel.center + outward * (width * 0.04f) + Vector3.down * (width * 0.08f);
            Paint(sphere, colour);
        }

        private static Color IconColour(string tower)
        {
            switch (tower)
            {
                case "red": return new Color(0.80f, 0.22f, 0.18f);
                case "green": return new Color(0.14f, 0.68f, 0.34f);
                case "blue": return new Color(0.16f, 0.50f, 0.88f);
                case "yellow": return new Color(0.88f, 0.78f, 0.16f);
                case "black": return new Color(0.74f, 0.64f, 0.44f);
                default: return new Color(0.92f, 0.92f, 0.92f);
            }
        }

        private static Transform FindIndicatorContainer()
        {
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer != null && renderer.gameObject.name == "EndingGateIndicator_base" && renderer.transform.parent != null
                    && renderer.transform.parent.Find("EndingGateIndicator_light") != null)
                    return renderer.transform.parent;
            }

            return null;
        }
    }
}
