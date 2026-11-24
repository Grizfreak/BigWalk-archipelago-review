using System;
using System.Collections.Generic;
using BigWalkArchipelago.Core.Net;
using UnityEngine;

namespace BigWalkArchipelago.Core.Hub
{
    internal static partial class MapTracker
    {
        private static void ApplyMarker(string key, Marker marker, byte state)
        {
            if (marker.Root == null)
                return;
            var shown = (state & Visible) != 0;
            if (marker.Root.activeSelf != shown)
                marker.Root.SetActive(shown);
            if (!shown)
                return;

            var grey = (state & GreyBit) != 0;
            if (grey != marker.Greyed)
            {
                marker.Greyed = grey;
                foreach (var (renderer, own, greyBlock) in marker.Parts)
                {
                    if (renderer != null)
                        renderer.SetPropertyBlock(grey ? greyBlock : own);
                }
            }

            if (key.StartsWith("objective:", StringComparison.Ordinal))
            {
                SetOpen(marker, (state & OpenBit) != 0);
                return;
            }

            var hinted = (state & HintBit) != 0;
            var stickColour = (state & TunnelBit) != 0 ? BuriedStickColour : StickColour;
            if (marker.Stick != null && (hinted != marker.Hinted || stickColour != marker.StickColour))
            {
                marker.Hinted = hinted;
                marker.StickColour = stickColour;
                if (hinted)
                    Glowing.Add(marker.Stick);
                else
                {
                    Glowing.Remove(marker.Stick);
                    Tint(marker.Stick, stickColour);
                }
            }
        }

        private static readonly HashSet<Renderer> Rainbow = new();

        private static void PulseRainbow()
        {
            if (Rainbow.Count == 0)
                return;
            var colour = Color.HSVToRGB(Time.unscaledTime * 0.25f % 1f, 0.9f, 1f) * 1.4f;
            colour.a = 1f;
            foreach (var renderer in new List<Renderer>(Rainbow))
            {
                if (renderer == null)
                    Rainbow.Remove(renderer);
                else
                    Tint(renderer, colour);
            }
        }

        private static void SetOpen(Marker marker, bool open)
        {
            if (marker.Stick == null || open == marker.Hinted)
                return;
            marker.Hinted = open;
            var height = open ? OpenStick : ObjectiveStick;
            var foot = marker.Root.transform.position;
            marker.Stick.transform.localScale = new Vector3(open ? 0.025f : 0.012f, height, open ? 0.025f : 0.012f);
            marker.Stick.transform.position = foot + Vector3.up * (height / 2f);
            var model = marker.Root.transform.Find("model");
            if (model != null)
                model.position = foot + Vector3.up * (height + ModelSize / 2f);
            if (open)
                Rainbow.Add(marker.Stick);
            else
            {
                Rainbow.Remove(marker.Stick);
                Tint(marker.Stick, ObjectiveColour);
            }
        }

        private static void PulseHints()
        {
            if (Glowing.Count == 0)
                return;
            var colour = Color.Lerp(Gold, Glow, (Mathf.Sin(Time.unscaledTime * 4f) + 1f) / 2f);
            foreach (var renderer in new List<Renderer>(Glowing))
            {
                if (renderer == null)
                    Glowing.Remove(renderer);
                else
                    Tint(renderer, colour);
            }
        }

        private static void Tint(Renderer renderer, Color colour)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_TintColor", colour);
            renderer.SetPropertyBlock(block);
        }

        private static void DestroyMarkers()
        {
            foreach (var stick in FlagSticks.Values)
            {
                if (stick != null)
                    UnityEngine.Object.Destroy(stick);
            }
            FlagSticks.Clear();
            Glowing.Clear();
            Rainbow.Clear();
            foreach (var marker in Markers.Values)
            {
                if (marker.Root != null)
                    UnityEngine.Object.Destroy(marker.Root);
            }
            Markers.Clear();
            Built.Clear();
            _placed.Clear();
            _scene = null;
            _hostKeysBuilt = false;
        }
    }
}
