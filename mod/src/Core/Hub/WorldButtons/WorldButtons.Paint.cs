using UnityEngine;

namespace BigWalkArchipelago.Core.Hub
{
    internal partial class WorldButtons
    {
        // The colour of the tower the button stands for; green without a picture.
        private static Color PlateColour(string icon)
        {
            switch (icon)
            {
                case "red": return new Color(0.80f, 0.22f, 0.18f);
                case "green": return new Color(0.14f, 0.68f, 0.34f);
                case "blue": return new Color(0.16f, 0.50f, 0.88f);
                case "yellow": return new Color(0.88f, 0.78f, 0.16f);
                case "black": return new Color(0.16f, 0.16f, 0.19f);
                case "hub": return new Color(0.74f, 0.64f, 0.44f);
                case "gauntlet": return new Color(0.04f, 0.04f, 0.05f);
                case "drawbridge": return new Color(0.42f, 0.27f, 0.14f);
                case "dome": return new Color(0.36f, 0.72f, 0.20f);
                default: return new Color(0.23f, 0.81f, 0.39f);
            }
        }

        // The game's plate is yellow in the mesh, so a plain slab of the same size replaces it.
        private static void ReplacePlate(GameObject target, Color colour)
        {
            Renderer plate = null;
            var biggest = 0f;
            foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                var size = renderer.bounds.size.sqrMagnitude;
                if (size > biggest)
                {
                    biggest = size;
                    plate = renderer;
                }
            }

            var filter = plate != null ? plate.GetComponent<MeshFilter>() : null;
            if (filter == null || filter.sharedMesh == null)
                return;

            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "AP button plate";
            slab.transform.SetParent(target.transform, false);
            slab.transform.rotation = plate.transform.rotation;
            slab.transform.position = plate.transform.TransformPoint(filter.sharedMesh.bounds.center);
            slab.transform.localScale = Vector3.Scale(filter.sharedMesh.bounds.size, plate.transform.lossyScale);
            // A copy: the game animates the plate material on press, lighting all that share it.
            if (plate.sharedMaterial != null && _plateMaterial == null)
                _plateMaterial = new Material(plate.sharedMaterial);
            if (_plateMaterial != null)
                _material = _plateMaterial;

            Paint(slab, colour);
            plate.enabled = false;
            Plugin.Log.LogInfo($"[{nameof(WorldButtons)}] Plate '{plate.gameObject.name}' replaced by a {colour} slab.");
        }

        internal static GameObject MakeSlab(string name, Vector3 position, Quaternion rotation, Vector3 size, Color colour)
        {
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = name;
            slab.transform.SetPositionAndRotation(position, rotation);
            slab.transform.localScale = size;
            Paint(slab, colour);
            return slab;
        }

        private static void Paint(GameObject part, Color colour)
        {
            var collider = part.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.Destroy(collider);

            var renderer = part.GetComponent<Renderer>();
            if (renderer == null)
                return;

            var material = FindGameMaterial();
            if (material != null)
                renderer.sharedMaterial = material;

            // The vertex-colour shader multiplies this over the mesh colour; a primitive has none.
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_TintColor", colour);
            block.SetColor("_Color", colour);
            block.SetColor("_BaseColor", colour);
            renderer.SetPropertyBlock(block);
        }

        // The engine's default material may be stripped from the build and render pink.
        private static Material FindGameMaterial()
        {
            if (_material != null)
                return _material;

            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var material = renderer != null ? renderer.sharedMaterial : null;
                if (material != null && material.shader != null && material.shader.name.Contains("VertexColors"))
                {
                    _material = material;
                    Plugin.Log.LogInfo($"[{nameof(WorldButtons)}] Borrowing material '{material.name}' ({material.shader.name}).");
                    return _material;
                }
            }

            Plugin.Log.LogInfo($"[{nameof(WorldButtons)}] No game material found; the engine's default will be used.");
            return null;
        }
    }
}
