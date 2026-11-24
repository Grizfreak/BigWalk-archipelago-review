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
        // The server seats each player, as a player's own action seats them on their machine only.
        private const float BreakServerDelay = 0.6f;
        private static readonly List<(PlayerCharacter player, Prop chair, float at)> BreakSeats = new();

        private static void TickBreakSeats()
        {
            for (var i = BreakSeats.Count - 1; i >= 0; i--)
            {
                var (player, chair, at) = BreakSeats[i];
                if (Time.unscaledTime < at)
                    continue;
                BreakSeats.RemoveAt(i);
                try
                {
                    var pose = chair != null ? chair.GetComponentInChildren<PlayerPose>() : null;
                    var networking = player != null ? player.playerNetworking : null;
                    if (pose == null || networking == null)
                        continue;
                    networking.ServerEnterPoseAuto(pose.shellReference);
                    Plugin.Log.LogInfo($"{Tag} Big Break: {player.gameObject.name} sat by the server (ticket {pose.ticket}).");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} Big Break: {ex.Message}");
                }
            }
        }
    }
}
