using System;
using UnityEngine;

namespace BigWalkArchipelago.Core.Visuals
{
    // Big Confetti filler: a local copy of a puzzle's confetti around every player, not networked.
    internal static class BigConfetti
    {
        private const string Tag = "[" + nameof(BigConfetti) + "]";
        private const string ParticleName = "RewardConfettiParticle";
        private const string Sound = "sfx_challengeWin_confetti_random";

        private const float Side = 1.2f;
        private const float Lifetime = 8f;

        private static ParticleSystem _template;
        private static bool _templateLogged;

        private static ParticleSystem Template()
        {
            if (_template != null)
                return _template;

            foreach (var system in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (system == null || system.gameObject.name != ParticleName)
                    continue;
                _template = system;
                break;
            }

            if (_template != null && !_templateLogged)
            {
                _templateLogged = true;
                var t = _template.transform;
                Plugin.Log.LogInfo($"{Tag} Confetti from {(t.parent != null ? t.parent.name : "-")} at {t.position}: up {t.up}, forward {t.forward}.");
            }

            return _template;
        }

        internal static void Play()
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null)
                return;

            var template = Template();
            if (template == null)
                Plugin.Log.LogInfo($"{Tag} No {ParticleName} in the scene: the sound alone.");

            var bursts = 0;
            foreach (var player in players)
            {
                if (player == null)
                    continue;

                var at = player.transform.position;
                ApSounds.PlayNamedAt(Sound, at);
                if (template == null)
                    continue;

                var right = player.transform.right;
                bursts += Burst(template, at - right * Side) + Burst(template, at + right * Side);
            }

            Plugin.Log.LogInfo($"{Tag} {bursts} burst(s).");
        }

        private static int Burst(ParticleSystem template, Vector3 position)
        {
            try
            {
                var copy = UnityEngine.Object.Instantiate(template.gameObject, position, template.transform.rotation);
                copy.name = ParticleName + " (AP)";
                copy.SetActive(true);
                var system = copy.GetComponent<ParticleSystem>();
                if (system != null)
                {
                    system.Clear(true);
                    system.Play(true);
                }
                UnityEngine.Object.Destroy(copy, Lifetime);
                return 1;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} A burst did not go: {ex.Message}");
                return 0;
            }
        }
    }
}
