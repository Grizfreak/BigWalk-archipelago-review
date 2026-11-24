using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Core.Visuals
{
    internal static partial class ColourPainter
    {
        // The gadgets painted (a laser is painted on its own first, PaintIfLaser).
        private static readonly string[][] GadgetKinds =
        {
            new[] { "BinocularsProp" }, new[] { "ClockProp" }, new[] { "CompassProp" },
            new[] { "CowBellLargeProp", "CowBellMediumProp", "CowBellSmallProp" }, new[] { "FmRadioProp" },
            new[] { "LaserProp" }, new[] { "MegaphoneProp" }, new[] { "TorchProp" }, new[] { "XrayGogglesProp" },
            new[] { "WalkieTalkieProp" },
        };

        // One colour per object, as the lamps, the same on every machine.
        private static bool PaintIfGadget(GameObject root, string key)
        {
            var row = Array.FindIndex(GadgetKinds, names => StartsWithAny(root.name, names));
            if (row < 0)
                return false;
            if (!Palette.Active)
                return true;

            var colour = Palette.For(string.IsNullOrEmpty(key) ? root.name : key).body;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var material = renderer != null ? renderer.sharedMaterial : null;
                if (material == null || !material.name.StartsWith("hhVertexColors", StringComparison.Ordinal)
                    || material.name.StartsWith("hhVertexColorsEmissive", StringComparison.Ordinal))
                    continue;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_TintColor", colour);
                renderer.SetPropertyBlock(block);
            }

            // A helper would write its own block over ours (WalkieColour): its tints are set too.
            foreach (var helper in root.GetComponentsInChildren<PropertyBlockHelper>(true))
            {
                var settings = helper != null ? helper.colorSettings : null;
                if (settings == null)
                    continue;
                var changed = false;
                for (var i = 0; i < settings.Length; i++)
                {
                    // An element of an IL2CPP struct array comes back as a copy.
                    var setting = settings[i];
                    if (setting.propertyName != "_TintColor" && setting.propertyName != "_Tint")
                        continue;
                    setting.color = colour;
                    settings[i] = setting;
                    changed = true;
                }
                if (changed)
                    helper.Refresh();
            }

            // A torch's light takes its body's colour, kept like a lamp's.
            if (root.name.StartsWith("TorchProp", StringComparison.Ordinal))
            {
                foreach (var light in root.GetComponentsInChildren<Light>(true))
                {
                    if (light == null)
                        continue;
                    light.color = colour;
                    Lamps.RemoveAll(entry => entry.light == light);
                    Lamps.Add((light, null, colour, null, colour));
                }
            }
            return true;
        }

        private const string LaserName = "LaserProp";
        private static readonly string[] GlowColours = { "_TintColor", "_MainColor", "_BaseColor", "_Color", "_EmissionColor" };

        private static bool PaintIfLaser(GameObject root, string key)
        {
            if (!root.name.StartsWith(LaserName, StringComparison.Ordinal))
                return false;
            if (!Palette.Active)
                return true;

            var colour = Palette.For(string.IsNullOrEmpty(key) ? root.name : key);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var material = renderer != null ? renderer.sharedMaterial : null;
                if (material == null)
                    continue;

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                if (material.name.StartsWith("hhVertexColors", StringComparison.Ordinal)
                    && !material.name.StartsWith("hhVertexColorsEmissive", StringComparison.Ordinal))
                {
                    block.SetColor("_TintColor", colour.body);
                }
                else if (renderer.name != "laser")
                {
                    // Not the tip: its vertex colours are set, and a tint on top would darken it.
                    foreach (var property in GlowColours)
                    {
                        if (material.HasProperty(property))
                            block.SetColor(property, colour.halo);
                    }
                }
                renderer.SetPropertyBlock(block);

                var line = renderer.TryCast<LineRenderer>();
                if (line != null)
                {
                    line.startColor = colour.halo;
                    line.endColor = colour.halo;
                }

                // The lasers' red is in their meshes' vertex colours, not in a shader property.
                if (renderer.name == "Beam" || renderer.name == "laser")
                    RecolourLaserPart(renderer, colour.halo);
            }

            foreach (var light in root.GetComponentsInChildren<Light>(true))
                light.color = colour.halo;

            foreach (var system in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = system.main;
                main.startColor = new ParticleSystem.MinMaxGradient(colour.halo);
            }

            return true;
        }
    }
}
