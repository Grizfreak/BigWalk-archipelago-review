using System;
using System.Globalization;
using BigWalkArchipelago.Core.Net;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Checks
{
    internal static partial class ObjectiveSpots
    {
        private const string SalonRoot = "LandmarksNonChallenge/Salon_Secret/Positioner";
        private const float SalonRadius = 20f;

        // A look id changing near the salon is a brush: the mod never writes these ids.
        private static readonly System.Collections.Generic.Dictionary<int, (int head, int torso, int legs)> Looks = new();

        private static void CheckTheMakeover()
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null)
                return;

            var salon = Find(SalonRoot);
            foreach (var player in players)
            {
                if (player == null || player.playerNetworking == null)
                    continue;

                var networking = player.playerNetworking;
                var now = (networking.lookIdHead, networking.lookIdTorso, networking.lookIdLegs);
                var id = player.GetInstanceID();
                var seen = Looks.TryGetValue(id, out var before);
                Looks[id] = now;
                if (!seen || before == now || salon == null)
                    continue;

                if (Vector3.Distance(player.transform.position, salon.position) > SalonRadius)
                    continue;

                // With `everyone_gathers`, every player painted there, each in turn.
                Painted.Add(id);
                if (EveryoneGathers && !EveryonePainted(players))
                {
                    Plugin.Log.LogInfo($"{Tag} {player.gameObject.name} was painted at the Secret Salon; not every player yet.");
                    continue;
                }

                SaveManager.SetIntValue(Latch("big_makeover"), 1);
                Plugin.Log.LogInfo($"{Tag} big_makeover is done: {player.gameObject.name} was painted at the Secret Salon "
                                   + $"({before} -> {now}).");
                ApNotices.Post("Big Makeover: painted at the Secret Salon");
                return;
            }
        }

        // The players painted at the Secret Salon in this session, by instance.
        private static readonly System.Collections.Generic.HashSet<int> Painted = new();

        private static bool EveryonePainted(Il2CppSystem.Collections.Generic.List<PlayerCharacter> players)
        {
            foreach (var player in players)
            {
                if (player != null && !Painted.Contains(player.GetInstanceID()))
                    return false;
            }
            return true;
        }
    }
}
