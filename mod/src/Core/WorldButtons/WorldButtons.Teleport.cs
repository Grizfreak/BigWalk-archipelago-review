using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal partial class WorldButtons
    {
        // The delay lets the button's click be heard before the player is sent away.
        private const float TeleportDelay = 0.4f;

        private static PlayerCharacter _pendingPlayer;
        private static Vector3 _pendingDestination;
        private static Quaternion _pendingRotation;
        private static float _pendingAt = -1f;

        internal static void TeleportSoon(PlayerCharacter player, Vector3 destination, Quaternion rotation)
        {
            _pendingPlayer = player;
            _pendingDestination = destination;
            _pendingRotation = rotation;
            _pendingAt = Time.unscaledTime + TeleportDelay;
        }

        private static void RunPendingTeleport()
        {
            if (_pendingAt < 0f || Time.unscaledTime < _pendingAt)
                return;

            _pendingAt = -1f;
            TeleportTo(_pendingPlayer, _pendingDestination, _pendingRotation);
            _pendingPlayer = null;
        }

        internal static void TeleportTo(PlayerCharacter player, Vector3 destination, Quaternion rotation)
        {
            if (player == null || player.grease == null)
            {
                Plugin.Log.LogInfo($"[{nameof(WorldButtons)}] No local player to move.");
                return;
            }

            var marker = new GameObject("World button destination");
            try
            {
                marker.transform.SetPositionAndRotation(destination, rotation);
                player.grease.Teleport(marker.transform);
                Plugin.Log.LogInfo($"[{nameof(WorldButtons)}] Teleported to {destination}.");
            }
            finally
            {
                UnityEngine.Object.Destroy(marker);
            }
        }
    }
}
