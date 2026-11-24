using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Core.Keys
{
    // Tints the five identical yellow big keys apart; they have no PropertyBlockHelper.
    internal static class KeyColours
    {
        // `_TintColor` multiplies a yellow base: blue is out of reach, black is exact.
        private static readonly Dictionary<SaveablePropName, string> Palette = new()
        {
            { SaveablePropName.bigKeyIntro, "#1E1E1E" },
            { SaveablePropName.bigKeyRedZone, "#E03A3A" },
            { SaveablePropName.bigKeyGreenZone, "#3ACF63" },
            // A near-neutral tint, the closest to cool this material allows.
            { SaveablePropName.bigKeyBlueZone, "#8898A0" },
            { SaveablePropName.bigKeyYellowZone, "#E8C53C" },
        };

        // Once per world: nothing the game does afterwards puts the original material back.
        private static bool _painted;

        private static bool _propertyLogged;

        // A world reload rebuilds every renderer, so the paint goes on again.
        internal static void Rearm()
        {
            _painted = false;
        }

        internal static void PaintOnce()
        {
            if (_painted || !ModConfig.ColorBigKeys.Value)
                return;

            var properties = ParseProperties(ModConfig.KeyColorProperty.Value);
            if (properties.Length == 0)
                return;

            var keys = GourdRegistry.LoadedBigKeys();
            var painted = 0;
            var seen = 0;

            foreach (var prop in keys)
            {
                seen++;
                if (!Palette.TryGetValue(prop.saveablePropName, out var html))
                    continue;

                if (!ColorUtility.TryParseHtmlString(html, out var colour))
                    continue;

                if (Paint(prop, properties, colour))
                    painted++;
            }

            // Nothing loaded yet means try again next tick.
            if (seen == 0)
                return;

            _painted = true;
            Plugin.Log.LogInfo(
                $"[{nameof(KeyColours)}] Painted {painted} of {seen} big key(s) through "
                + $"{string.Join(", ", properties)}. A property the shader does not declare is accepted "
                + "silently and does nothing, so if they still look alike the line above names the ones "
                + "it really has.");
        }

        private static string[] ParseProperties(string configured)
        {
            if (string.IsNullOrWhiteSpace(configured))
                return Array.Empty<string>();

            var parts = configured.Split(',');
            var kept = new List<string>();
            foreach (var part in parts)
            {
                var name = part.Trim();
                if (name.Length > 0)
                    kept.Add(name);
            }

            return kept.ToArray();
        }

        private static bool Paint(Prop prop, string[] properties, Color colour)
        {
            try
            {
                var renderers = prop.GetComponentsInChildren<Renderer>(true);
                if (renderers == null || renderers.Length == 0)
                    return false;

                LogShaderPropertiesOnce(renderers[0]);

                var block = new MaterialPropertyBlock();
                foreach (var renderer in renderers)
                {
                    if (renderer == null)
                        continue;

                    // Read-modify-write: keep what the game already set on this renderer.
                    renderer.GetPropertyBlock(block);
                    foreach (var property in properties)
                        block.SetColor(property, colour);
                    renderer.SetPropertyBlock(block);
                }

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(KeyColours)}] Could not paint {prop.saveablePropName}: {ex.Message}");
                return false;
            }
        }

        // A MaterialPropertyBlock ignores unknown properties: log what the shader declares.
        private static void LogShaderPropertiesOnce(Renderer renderer)
        {
            if (_propertyLogged)
                return;

            _propertyLogged = true;

            try
            {
                var material = renderer.sharedMaterial;
                var shader = material != null ? material.shader : null;
                if (shader == null)
                {
                    Plugin.Log.LogInfo($"[{nameof(KeyColours)}] A key renderer has no shader to inspect.");
                    return;
                }

                var count = shader.GetPropertyCount();
                var names = new List<string>();
                for (var i = 0; i < count; i++)
                    names.Add($"{shader.GetPropertyName(i)}({shader.GetPropertyType(i)})");

                Plugin.Log.LogInfo(
                    $"[{nameof(KeyColours)}] Shader '{shader.name}' on the keys declares: {string.Join(", ", names)}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(KeyColours)}] Could not read the key shader's properties ({ex.Message}).");
            }
        }
    }
}
