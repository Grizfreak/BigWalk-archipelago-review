using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Core.Visuals
{
    internal static partial class ColourPainter
    {
        private const string LauncherName = "FireworkLauncher";

        // The towers' launchers keep their own colours, and the other launchers never take those.
        private static readonly string[] TowerColours = { "Red", "Yellow", "Blue", "Green" };

        private static (string name, Color body, Color halo) FireworkColour(string landmark)
        {
            var free = new List<(string name, Color body, Color halo)>();
            foreach (var index in Palette.Current)
            {
                if (Array.IndexOf(TowerColours, Palette.Colours[index].name) < 0)
                    free.Add(Palette.Colours[index]);
            }
            if (free.Count == 0)
            {
                foreach (var entry in Palette.Colours)
                {
                    if (Array.IndexOf(TowerColours, entry.name) < 0)
                        free.Add(entry);
                }
            }

            unchecked
            {
                var hash = 2166136261u;
                foreach (var c in "firework " + landmark)
                    hash = (hash ^ c) * 16777619u;
                return free[(int)(hash % (uint)free.Count)];
            }
        }

        private static int PaintFireworks()
        {
            if (!Palette.Active)
                return 0;

            var count = 0;
            foreach (var launcher in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (launcher == null || !launcher.name.StartsWith(LauncherName, StringComparison.Ordinal))
                    continue;
                var landmark = PackChecks.LandmarkOf(launcher);
                if (string.IsNullOrEmpty(landmark) || landmark.StartsWith("Lookout_", StringComparison.Ordinal))
                    continue;

                try
                {
                    var colour = FireworkColour(landmark);
                    foreach (var helper in launcher.GetComponentsInChildren<PropertyBlockHelper>(true))
                    {
                        if (helper == null || helper.colorSettings == null)
                            continue;
                        var property = helper.gameObject.name switch
                        {
                            "fireworkLauncherModel" => "_TintColor",
                            "HangingSpark" => "_EmissionColor",
                            _ => null,
                        };
                        if (property == null)
                            continue;
                        var settings = helper.colorSettings;
                        for (var i = 0; i < settings.Length; i++)
                        {
                            // An element of an IL2CPP struct array comes back as a copy.
                            var setting = settings[i];
                            if (setting.propertyName != property)
                                continue;
                            setting.color = property == "_TintColor" ? colour.body : colour.halo;
                            settings[i] = setting;
                        }
                        helper.Refresh();
                    }

                    foreach (var system in launcher.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        if (system.name != "HangingSpark" && system.name != "SparkParticleSystem")
                            continue;
                        var main = system.main;
                        main.startColor = new ParticleSystem.MinMaxGradient(colour.halo);
                    }
                    count++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} Could not paint the firework in {landmark}: {ex.Message}");
                }
            }
            return count;
        }
    }
}
