using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    internal static partial class TrapEffects
    {
        // The host spawns a chair by each player; each machine seats its player once it is there.
        internal const string Break = "break";
        private const float BreakWait = 5f;
        private static Vector3 _breakAt;
        private static float _breakUntil = -1f;

        private static void StartBreak(Vector3? chair)
        {
            if (chair == null)
                return;
            _breakAt = chair.Value;
            _breakUntil = Time.unscaledTime + BreakWait;
            Plugin.Log.LogInfo($"{Tag} Big Break: a chair at {_breakAt}.");
        }

        private static List<PlayerPose> BreakSeats()
        {
            var seats = new List<PlayerPose>();
            foreach (var pose in UnityEngine.Object.FindObjectsByType<PlayerPose>(FindObjectsSortMode.None))
            {
                var prop = pose != null ? pose.parentProp : null;
                if (prop != null && prop.gameObject.name.StartsWith("foldingChair", StringComparison.Ordinal) && ReceivedItemSpawner.IsCosmeticClone(prop))
                    seats.Add(pose);
            }
            return seats;
        }

        private static PlayerPose NearestSeat(List<PlayerPose> seats, Vector3 at, float within)
        {
            PlayerPose seat = null;
            foreach (var pose in seats)
            {
                var distance = Vector3.Distance(pose.transform.position, at);
                if (distance < within)
                {
                    within = distance;
                    seat = pose;
                }
            }
            return seat;
        }

        // Each machine also seats the others as it sees them, or a guest would not show them sat.
        private static readonly HashSet<int> BreakSat = new();

        private static void StepBreak(float now)
        {
            if (now >= _breakUntil)
            {
                _breakUntil = -1f;
                BreakSat.Clear();
                return;
            }
            var seats = BreakSeats();
            if (seats.Count == 0)
                return;
            foreach (var player in UnityEngine.Object.FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None))
            {
                if (player == null || BreakSat.Contains(player.GetInstanceID()))
                    continue;
                var poser = player.poser;
                if (poser == null)
                    continue;
                var seat = player.isLocalPlayer ? NearestSeat(seats, _breakAt, 3f) : NearestSeat(seats, BodyPosition(player), 3f);
                if (seat == null)
                    continue;
                BreakSat.Add(player.GetInstanceID());
                if (poser.currentPose != null && poser.currentPose.Pointer == seat.Pointer)
                    continue;
                poser.EnterPose(seat);
                Plugin.Log.LogInfo($"{Tag} Big Break: {player.gameObject.name} sat here on ticket {seat.ticket}.");
            }
        }
    }
}
