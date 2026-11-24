using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Recolours a flare gun's shot. The interop keeps only MinMaxGradient's Color ctor and fields.
    internal static class FlareColours
    {
        internal static void Apply(ParticleSystem shot, Color colour)
        {
            if (shot == null)
                return;

            foreach (var system in shot.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = system.main;
                switch (system.name)
                {
                    case "FlareParticleSystem":
                    case "ExplosionLight":
                        main.startColor = new ParticleSystem.MinMaxGradient(colour);
                        break;
                    case "SmokeParticleSystem":
                        main.startColor = Gradient(
                            (Scale(colour, 0.38f), 0f), (colour, 0.11f), (Color.Lerp(colour, Color.white, 0.2f), 0.97f));
                        break;
                    case "SmokeParticleSystem (1)":
                        main.startColor = Gradient((Color.Lerp(colour, Color.white, 0.15f), 0f), (Scale(colour, 0.6f), 1f));
                        break;
                    // These puffs are peach on every gun and read as red smoke after a blue shot.
                    case "PuffSystem":
                        var puff = new ParticleSystem.MinMaxGradient(Color.white);
                        puff.m_ColorMin = Color.Lerp(colour, Color.white, 0.55f);
                        puff.m_ColorMax = Color.Lerp(colour, Color.white, 0.35f);
                        puff.m_Mode = ParticleSystemGradientMode.TwoColors;
                        main.startColor = puff;
                        break;
                }
            }
        }

        private static Color Scale(Color colour, float by) => new Color(colour.r * by, colour.g * by, colour.b * by, 1f);

        private static ParticleSystem.MinMaxGradient Gradient(params (Color colour, float time)[] keys)
        {
            var colours = new GradientColorKey[keys.Length];
            for (var i = 0; i < keys.Length; i++)
                colours[i] = new GradientColorKey(keys[i].colour, keys[i].time);
            var gradient = new UnityEngine.Gradient();
            gradient.SetKeys(colours, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

            var made = new ParticleSystem.MinMaxGradient(Color.white);
            made.m_GradientMax = gradient;
            made.m_Mode = ParticleSystemGradientMode.Gradient;
            return made;
        }
    }
}
