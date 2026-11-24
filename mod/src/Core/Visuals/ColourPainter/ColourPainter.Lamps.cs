using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Core.Visuals
{
    internal static partial class ColourPainter
    {
        private static readonly string[] BuoyNames = { "BuoyLight", "BuoyProp", "BuoyRedProp" };

        // Switching a lamp on resets its light and halo, so they are put back twice a second.
        private static readonly List<(Light light, Renderer halo, Color colour, Renderer body, Color bodyColour)> Lamps = new();
        private static float _nextLampCheck;

        private static void KeepTheLamps()
        {
            if (Lamps.Count == 0 || Time.unscaledTime < _nextLampCheck)
                return;

            _nextLampCheck = Time.unscaledTime + 0.5f;
            var fixedCount = 0;
            for (var i = Lamps.Count - 1; i >= 0; i--)
            {
                var (light, halo, colour, body, bodyColour) = Lamps[i];
                if (light == null && halo == null && body == null)
                {
                    Lamps.RemoveAt(i);
                    continue;
                }

                if (light != null && light.enabled && light.color != colour)
                {
                    light.color = colour;
                    fixedCount++;
                }

                if (halo != null)
                {
                    var block = new MaterialPropertyBlock();
                    halo.GetPropertyBlock(block);
                    // An off lamp hides its halo; a block the game rewrote reads black.
                    if (halo.gameObject.activeInHierarchy && block.GetColor("_MainColor") != colour)
                    {
                        block.SetColor("_MainColor", colour);
                        halo.SetPropertyBlock(block);
                        fixedCount++;
                    }
                }

                // The buoy's PropertyBlockHelper rewrites its block untinted on a switch.
                if (body != null)
                {
                    var block = new MaterialPropertyBlock();
                    body.GetPropertyBlock(block);
                    if (block.GetColor("_TintColor") != bodyColour)
                    {
                        block.SetColor("_TintColor", bodyColour);
                        body.SetPropertyBlock(block);
                        fixedCount++;
                    }
                }
            }

            if (fixedCount > 0 && !_lampFixLogged)
            {
                _lampFixLogged = true;
                Plugin.Log.LogInfo($"{Tag} A lamp's light or halo had been changed by the game: put back to the seed's colour.");
            }
        }

        private static bool _lampFixLogged;

        private static bool PaintIfBuoy(GameObject root, string key)
        {
            if (!StartsWithAny(root.name, BuoyNames))
                return false;
            if (!Palette.Active)
                return true;

            var colour = Palette.For(string.IsNullOrEmpty(key) ? root.name : key);
            if (root.name.StartsWith("BuoyLight", StringComparison.Ordinal))
                colour = ("Orange", Color.white, PedestalLampHalo);
            Renderer haloRenderer = null;
            Renderer bodyRenderer = null;
            Light lampLight = null;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name == "buoy")
                {
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    block.SetColor("_TintColor", colour.body);
                    renderer.SetPropertyBlock(block);
                    bodyRenderer = renderer;
                }
                else if (renderer.name == "LobbyFlareMesh" && UnderBuoyLight(renderer.transform))
                {
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    block.SetColor("_MainColor", colour.halo);
                    renderer.SetPropertyBlock(block);
                    haloRenderer = renderer;
                }
            }

            foreach (var light in root.GetComponentsInChildren<Light>(true))
            {
                if (light.name == "bouyLight")
                {
                    light.color = colour.halo;
                    lampLight = light;
                }
            }

            if (lampLight != null || haloRenderer != null || bodyRenderer != null)
            {
                Lamps.RemoveAll(entry => entry.light == lampLight && entry.halo == haloRenderer && entry.body == bodyRenderer);
                Lamps.Add((lampLight, haloRenderer, colour.halo, bodyRenderer, colour.body));
            }

            return true;
        }

        // A pedestal lamp's sphere is orange in the model and takes no tint: its light matches.
        private static readonly Color PedestalLampHalo = new Color(1.00f, 0.40f, 0.00f);

        // The halo sits at bouyLight/LobbyFlare/LobbyFlareMesh, two levels under the light.
        private static bool UnderBuoyLight(Transform transform)
        {
            for (var t = transform.parent; t != null; t = t.parent)
            {
                if (t.name == "bouyLight")
                    return true;
            }
            return false;
        }
    }
}
