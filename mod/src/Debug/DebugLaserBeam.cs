using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Cycles test settings on the laser beams, on a material copy (a property block would be lost).
    internal static class DebugLaserBeam
    {
        private const string Tag = "[" + nameof(DebugLaserBeam) + "]";

        private static readonly (string what, float emission, Color colour)[] Settings =
        {
            ("emission 0, colour blue 1", 0f, new Color(0f, 0f, 1f)),
            ("emission 0, colour blue 8 (HDR)", 0f, new Color(0f, 0f, 8f)),
            ("emission 1, colour black", 1f, Color.black),
            ("emission 11.2, colour blue 50 (HDR)", 11.2f, new Color(0f, 0f, 50f)),
            ("emission 11.2, colour minus red (-10, 5, 10)", 11.2f, new Color(-10f, 5f, 10f)),
            ("emission -1, colour blue 10", -1f, new Color(0f, 0f, 10f)),
            ("emission 0, colour black", 0f, Color.black),
            ("the game's own", float.NaN, Color.black),
        };

        private static int _next;
        private static readonly Dictionary<int, Material> Originals = new();

        internal static void Cycle()
        {
            var setting = Settings[_next];
            _next = (_next + 1) % Settings.Length;

            var count = 0;
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer == null || renderer.name != "Beam")
                    continue;

                var id = renderer.GetInstanceID();
                if (!Originals.TryGetValue(id, out var original))
                    Originals[id] = original = renderer.sharedMaterial;
                if (original == null)
                    continue;

                if (float.IsNaN(setting.emission))
                {
                    renderer.sharedMaterial = original;
                }
                else
                {
                    var copy = new Material(original) { name = "laser (beam probe)" };
                    copy.SetFloat("_Emission", setting.emission);
                    copy.SetColor("_EmissionColor", setting.colour);
                    renderer.sharedMaterial = copy;
                }

                count++;
            }

            Plugin.Log.LogInfo($"{Tag} Setting {(_next == 0 ? Settings.Length : _next)}/{Settings.Length}: {setting.what}, on {count} beam(s).");
            Core.Net.ApNotices.Post($"Beam {(_next == 0 ? Settings.Length : _next)}: {setting.what}");
        }
    }
}
