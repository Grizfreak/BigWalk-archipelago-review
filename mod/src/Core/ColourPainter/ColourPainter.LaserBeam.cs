using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal static partial class ColourPainter
    {
        private static bool _laserDescribed;

        // One tube per colour, shared by the lasers that wear it.
        private static readonly Dictionary<uint, Mesh> Tubes = new();
        private const int TubeSides = 6;
        private const int TubeRings = 69;

        // The game's red is about 0.8 of a full channel, so the colour is brought to that level.
        private static Mesh BeamTube(Bounds bounds, Color colour)
        {
            var max = Mathf.Max(colour.r, Mathf.Max(colour.g, colour.b));
            var scaled = max > 0f ? colour * (0.8f / max) : colour;
            var key = (Color32)new Color(scaled.r, scaled.g, scaled.b, 1f);
            var id = (uint)key.r << 16 | (uint)key.g << 8 | key.b;
            if (Tubes.TryGetValue(id, out var known) && known != null)
                return known;

            var radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            var top = bounds.max.y;
            var bottom = bounds.min.y;
            var centre = bounds.center;
            var columns = TubeSides + 1;
            var vertices = new Vector3[TubeRings * columns];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var colours = new Color32[vertices.Length];
            for (var ring = 0; ring < TubeRings; ring++)
            {
                var t = ring / (float)(TubeRings - 1);
                var y = Mathf.Lerp(top, bottom, t);
                for (var side = 0; side < columns; side++)
                {
                    // The game's tube has a corner on +Z: start there.
                    var angle = Mathf.PI / 2f - side * 2f * Mathf.PI / TubeSides;
                    var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    var at = ring * columns + side;
                    vertices[at] = new Vector3(centre.x, y, centre.z) + outward * radius;
                    normals[at] = outward;
                    uvs[at] = new Vector2(side / (float)TubeSides, t);
                    colours[at] = key;
                }
            }

            var triangles = new int[(TubeRings - 1) * TubeSides * 6];
            var index = 0;
            for (var ring = 0; ring < TubeRings - 1; ring++)
            {
                for (var side = 0; side < TubeSides; side++)
                {
                    var a = ring * columns + side;
                    var b = a + 1;
                    var c = a + columns;
                    var d = c + 1;
                    triangles[index++] = a;
                    triangles[index++] = c;
                    triangles[index++] = b;
                    triangles[index++] = b;
                    triangles[index++] = c;
                    triangles[index++] = d;
                }
            }

            var tube = new Mesh { name = "laser (AP colour)" };
            tube.vertices = vertices;
            tube.normals = normals;
            tube.uv = uvs;
            tube.colors32 = colours;
            tube.triangles = triangles;
            tube.bounds = bounds;
            UnityEngine.Object.DontDestroyOnLoad(tube);
            Tubes[id] = tube;
            return tube;
        }
        private static Shader _plainShader;
        private static bool _plainLooked;

        private static void RecolourLaserPart(Renderer renderer, Color colour)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            var mesh = filter != null ? filter.sharedMesh : null;
            var material = renderer.sharedMaterial;

            // Once per launch, what the part is made of, for the next measurement.
            if (!_laserDescribed && renderer.name == "Beam")
            {
                _laserDescribed = true;
                var shader = material != null ? material.shader : null;
                var properties = new List<string>();
                if (shader != null)
                {
                    for (var i = 0; i < shader.GetPropertyCount(); i++)
                        properties.Add($"{shader.GetPropertyName(i)}({shader.GetPropertyType(i)})");
                }
                var sample = mesh != null && mesh.isReadable && mesh.colors32 != null && mesh.colors32.Length > 0
                    ? mesh.colors32[0].ToString()
                    : "none";
                Plugin.Log.LogInfo($"{Tag} Laser beam: shader {(shader != null ? shader.name : "?")} [{string.Join(", ", properties)}], "
                                   + $"texture {(material != null && material.mainTexture != null ? material.mainTexture.name : "none")}, "
                                   + $"mesh {(mesh != null ? mesh.name : "?")} readable={(mesh != null && mesh.isReadable)} "
                                   + $"vertices={(mesh != null ? mesh.vertexCount : 0)} first colour={sample}.");
            }

            if (mesh != null && mesh.isReadable)
            {
                try
                {
                    // An instance of its own: the mesh is the same for every laser.
                    var own = filter.mesh;
                    var colours = own.colors32;
                    var count = own.vertexCount;
                    var painted = new Color32[count];
                    var target = (Color32)colour;
                    for (var i = 0; i < count; i++)
                    {
                        var alpha = colours != null && colours.Length == count ? colours[i].a : (byte)255;
                        painted[i] = new Color32(target.r, target.g, target.b, alpha);
                    }
                    own.colors32 = painted;
                    return;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} A laser's {renderer.name} kept its colours: {ex.Message}");
                }
            }

            // Keeps househouse/laser, which stretches the beam; it vanishes under other shaders.
            if (renderer.name == "Beam")
            {
                if (filter != null && mesh != null)
                    filter.sharedMesh = BeamTube(mesh.bounds, colour);
                return;
            }

            // Unreadable mesh: Standard ignores the red vertex colours, Particles Unlit does not.
            if (!_plainLooked)
            {
                _plainLooked = true;
                _plainShader = Shader.Find("Standard");
                if (_plainShader == null)
                    Plugin.Log.LogWarning($"{Tag} No Standard shader to colour the lasers' beams with; they stay red.");
            }
            if (_plainShader == null)
                return;

            var plain = new Material(_plainShader) { name = "laser (AP colour)" };
            plain.SetColor("_Color", colour);
            plain.EnableKeyword("_EMISSION");
            plain.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            plain.SetColor("_EmissionColor", colour * 2f);
            renderer.sharedMaterial = plain;
        }
    }
}
