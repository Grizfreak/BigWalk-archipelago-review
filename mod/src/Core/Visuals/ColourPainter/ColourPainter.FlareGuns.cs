using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Core.Visuals
{
    internal static partial class ColourPainter
    {
        private static readonly string[] GunNames = { "FlareGunPropBlue", "FlareGunPropGreen", "FlareGunPropYellow", "FlareGunProp" };

        private static Color _rainbowShown;

        // A gun's place in the seed's draw: base 0, Blue 1, Green 2, Yellow 3.
        private static int FlareGunSlot(string name)
        {
            if (name.StartsWith("FlareGunPropBlue", StringComparison.Ordinal))
                return 1;
            if (name.StartsWith("FlareGunPropGreen", StringComparison.Ordinal))
                return 2;
            if (name.StartsWith("FlareGunPropYellow", StringComparison.Ordinal))
                return 3;
            return 0;
        }

        private static bool PaintIfGun(GameObject root)
        {
            if (!StartsWithAny(root.name, GunNames))
                return false;

            var rainbow = root.name.Contains(RainbowMarker, StringComparison.Ordinal);
            if (!rainbow && !Palette.Active)
                return true;

            var colour = rainbow ? default : Palette.At(FlareGunSlot(root.name));
            var body = rainbow ? Palette.RainbowBody : colour.body;

            foreach (var helper in root.GetComponentsInChildren<PropertyBlockHelper>(true))
            {
                var target = helper != null ? helper.targetRenderer : null;
                if (target == null || helper.targetSubmesh != 0 || target.name != "flaregun" && target.name != "flaregunbarrel")
                    continue;
                var settings = helper.colorSettings;
                if (settings == null || settings.Length == 0)
                    continue;

                // An element of an IL2CPP struct array comes back as a copy.
                var setting = settings[0];
                setting.color = body;
                settings[0] = setting;
                helper.Refresh();
            }

            foreach (var effect in root.GetComponentsInChildren<PeckEffectParticleNetworked>(true))
            {
                if (effect == null || effect.name != "FlareGunFlare" || effect.targetParticleSystem == null)
                    continue;
                if (rainbow)
                {
                    if (!RainbowShots.Contains(effect.targetParticleSystem))
                        RainbowShots.Add(effect.targetParticleSystem);
                }
                else
                {
                    FlareColours.Apply(effect.targetParticleSystem, colour.halo);
                }
            }

            return true;
        }

        private static void TickRainbow()
        {
            if (RainbowShots.Count == 0)
                return;
            var colour = Palette.RainbowNow();
            if (colour == _rainbowShown)
                return;
            _rainbowShown = colour;

            for (var i = RainbowShots.Count - 1; i >= 0; i--)
            {
                var shot = RainbowShots[i];
                if (shot == null)
                {
                    RainbowShots.RemoveAt(i);
                    continue;
                }
                FlareColours.Apply(shot, colour);
            }
        }
    }
}
