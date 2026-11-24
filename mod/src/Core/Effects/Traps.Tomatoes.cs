using System;
using System.Collections.Generic;
using System.Linq;
using BigWalkArchipelago.Core.Net;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    internal static partial class Traps
    {
        // The tomato leaves the world this long after its pop, as the sound dies with it.
        private const float TomatoLinger = 0.1f;
        private const float TomatoSoundSeconds = 1.5f;
        private static readonly List<KeyValuePair<Prop, float>> Tomatoes = new();
        private const float TomatoStartDelay = 0.3f;
        private static readonly List<(Prop tomato, PlayerCharacter player, float at)> TomatoStarts = new();

        private static void StartDueTomatoes()
        {
            for (var i = TomatoStarts.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime < TomatoStarts[i].at)
                    continue;

                var start = TomatoStarts[i];
                TomatoStarts.RemoveAt(i);
                if (start.tomato != null && start.tomato.gameObject != null)
                    StartTomato(start.tomato, start.player);
            }
        }

        private static void GiveTomato(Walker walker)
        {
            var tomato = GadgetItemSpawner.SpawnFor(GadgetKind.Pomodoro, walker.Player);
            if (tomato == null)
                return;

            // Set going once in the hands: the hand-over after the spawn would cut its tick.
            var startAt = Time.unscaledTime + TomatoStartDelay;
            TomatoStarts.Add((tomato, walker.Player, startAt));
            Tomatoes.Add(new KeyValuePair<Prop, float>(tomato, startAt + GadgetItemSpawner.TomatoSeconds + TomatoLinger));
        }

        // With the tomato as the context's prop, or its tick and pop have nowhere to play.
        private static void StartTomato(Prop tomato, PlayerCharacter player)
        {
            foreach (var peckSwitch in tomato.GetComponentsInChildren<PeckSwitch>(true))
            {
                if (peckSwitch == null || peckSwitch.gameObject.name != "OnSetSwitch")
                    continue;

                var context = new PeckContext(0)
                {
                    propIdentity = tomato.GetComponent<NetworkIdentity>(),
                    playerIdentity = player != null ? player.netIdentity : null,
                };
                peckSwitch.Peck(context);
                Plugin.Log.LogInfo($"{Tag} Tomato set going for {GadgetItemSpawner.TomatoSeconds:0}s.");
                return;
            }

            Plugin.Log.LogWarning($"{Tag} The tomato has no OnSetSwitch; it will not pop.");
        }

        // Host, every frame: the tomatoes that have popped, out of hands and out of the world.
        private static readonly HashSet<int> Released = new();

        private static void ClearTomatoes()
        {
            for (var i = Tomatoes.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime < Tomatoes[i].Value)
                    continue;

                var tomato = Tomatoes[i].Key;
                Tomatoes.RemoveAt(i);
                try
                {
                    if (tomato == null || tomato.gameObject == null)
                        continue;

                    // First time due: out of the hands, back in the list for when its pop is over.
                    if (!Released.Contains(tomato.GetInstanceID()))
                    {
                        Released.Add(tomato.GetInstanceID());
                        ReceivedItemSpawner.ReleaseFromHands(tomato);
                        Tomatoes.Add(new KeyValuePair<Prop, float>(tomato, Time.unscaledTime + TomatoSoundSeconds));
                        continue;
                    }

                    Released.Remove(tomato.GetInstanceID());
                    NetworkServer.Destroy(tomato.gameObject);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} Could not take a tomato away: {ex.Message}");
                }
            }
        }
    }
}
