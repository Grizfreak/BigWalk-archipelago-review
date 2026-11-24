using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    internal static partial class TrapEffects
    {
        // A timed tomato in the hands; when it pops the player is thrown high (no DeathLink).
        internal const string Air = "air";
        // The game caps a body near 60 m/s, so the push is held for a while instead.
        private const float ThrustSpeed = 60f;
        private const float ThrustSeconds = 5f;
        private const float CeilingRoom = 2f;
        private static bool _countdownLaunch;

        // The teleport comes a second after the tomato pops, so the pop is heard where they stood.
        private const float CountdownSeconds = 0.3f + GadgetItemSpawner.TomatoSeconds + 1f;
        private static float _countdownEnd = -1f;
        private static string _countdownName;
        private static PlayerCharacter _countdownPlayer;
        private static Vector3 _countdownDestination;
        private static Quaternion _countdownRotation;

        private static void StartCountdown(string key, PlayerCharacter player, Vector3 destination, Quaternion rotation)
        {
            _countdownName = DisplayName(key);
            _countdownPlayer = player;
            _countdownDestination = destination;
            _countdownRotation = rotation;
            _countdownEnd = Time.unscaledTime + CountdownSeconds;
            _countdownLaunch = false;
        }

        private static void StepCountdown()
        {
            if (Time.unscaledTime < _countdownEnd)
                return;

            _countdownEnd = -1f;
            var player = _countdownPlayer;
            _countdownPlayer = null;
            if (_countdownLaunch)
            {
                _countdownLaunch = false;
                Launch(player);
                return;
            }
            try
            {
                WorldButtons.TeleportTo(player, _countdownDestination, _countdownRotation);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} {_countdownName}: the teleport failed: {ex.Message}");
            }
        }

        // The game holds a body's climb back, so the speed is set again every frame for a while.
        private static Rigidbody _launchBody;
        private static float _launchUntil = -1f;

        private static void Launch(PlayerCharacter player)
        {
            try
            {
                var body = player != null ? player.GetComponentInChildren<Rigidbody>() : null;
                if (body == null)
                {
                    Plugin.Log.LogWarning($"{Tag} Big Air: no body to throw.");
                    return;
                }
                var reach = ThrustSpeed * ThrustSeconds;
                var from = body.position + Vector3.up * 1.5f;
                foreach (var hit in Physics.RaycastAll(new Ray(from, Vector3.up), reach + CeilingRoom))
                {
                    if (hit.collider == null || hit.collider.attachedRigidbody == body || hit.collider.transform.IsChildOf(player.transform))
                        continue;
                    reach = Mathf.Min(reach, Mathf.Max(0f, hit.distance - CeilingRoom));
                }
                _launchBody = body;
                _launchUntil = Time.unscaledTime + reach / ThrustSpeed;
                Plugin.Log.LogInfo($"{Tag} Big Air: up {reach:F0} m ({(reach < ThrustSpeed * ThrustSeconds ? "under a ceiling" : "open sky")}).");
                HoldLaunch();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Big Air failed: {ex.Message}");
            }
        }

        private static void HoldLaunch()
        {
            if (_launchBody == null || Time.unscaledTime >= _launchUntil)
            {
                _launchBody = null;
                _launchUntil = -1f;
                return;
            }
            var velocity = _launchBody.velocity;
            _launchBody.velocity = new Vector3(velocity.x, ThrustSpeed, velocity.z);
        }
    }
}
