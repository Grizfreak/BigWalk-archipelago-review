using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Ctrl+F7 cycles the flare guns and shots through the buoy palette, then rainbow, then red.
    internal static class DebugFlareTint
    {
        private const string Tag = "[" + nameof(DebugFlareTint) + "]";
        private static int _step = -1;
        private static bool _rainbow;

        internal static void Next()
        {
            var palette = DebugBuoyTint.Palette;
            _step = (_step + 1) % (palette.Length + 2);
            _rainbow = _step == palette.Length;
            var reset = _step == palette.Length + 1;
            var body = _rainbow ? new Color(0.95f, 0.95f, 0.92f) : reset ? Color.white : palette[_step].body;
            var shot = reset ? new Color(1f, 0f, 0f) : _rainbow ? Color.white : palette[_step].halo;
            var guns = 0;
            var shots = 0;

            if (!reset)
            {
                foreach (var helper in UnityEngine.Object.FindObjectsByType<PropertyBlockHelper>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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
                    if (target.name == "flaregun")
                        guns++;
                }
            }

            foreach (var effect in UnityEngine.Object.FindObjectsByType<PeckEffectParticleNetworked>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (effect == null || effect.name != "FlareGunFlare" || effect.targetParticleSystem == null)
                    continue;
                FlareColours.Apply(effect.targetParticleSystem, shot);
                shots++;
            }

            Plugin.Log.LogInfo(reset
                ? $"{Tag} Shots back to red ({shots}); the guns keep the last body colour until reloaded."
                : _rainbow
                    ? $"{Tag} Rainbow: {guns} white gun(s); shots take the palette in turn, a colour every {Palette.RainbowStep:0}s of the network clock."
                    : $"{Tag} Colour {_step + 1}/{palette.Length}: {palette[_step].name} on {guns} gun(s) and {shots} shot(s).");
        }

        private static Color _shown;

        internal static void Tick()
        {
            if (!_rainbow)
                return;
            var colour = Palette.RainbowNow();
            if (colour == _shown)
                return;
            _shown = colour;
            foreach (var effect in UnityEngine.Object.FindObjectsByType<PeckEffectParticleNetworked>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (effect != null && effect.name == "FlareGunFlare")
                    FlareColours.Apply(effect.targetParticleSystem, colour);
            }
        }
    }
}
