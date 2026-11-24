using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal static partial class TrapEffects
    {
        // Muted via CmdSetMute and DissonanceComms.IsMuted, held every frame against the mute key.
        internal const string Silence = "silence";
        private static float _silenceUntil = -1f;
        private static bool _silenceWasMuted;
        private static bool _silenceCommsWasMuted;
        private static Dissonance.DissonanceComms _comms;

        private static void StartSilence(PlayerCharacter player, int seconds)
        {
            if (_silenceUntil < 0f)
            {
                var networking = player.GetComponent<PlayerNetworking>();
                _silenceWasMuted = networking != null && networking.isMuted;
                _comms = UnityEngine.Object.FindObjectOfType<Dissonance.DissonanceComms>();
                _silenceCommsWasMuted = _comms != null && _comms.IsMuted;
            }

            _silenceUntil = Mathf.Max(_silenceUntil, Time.unscaledTime + seconds);
            HoldSilence();
            Plugin.Log.LogInfo($"{Tag} Big Silence: muted for {seconds}s (was muted: {_silenceWasMuted}, mic {_silenceCommsWasMuted}).");
        }

        private static void HoldSilence()
        {
            try
            {
                var player = LocalPlayer.Find();
                var networking = player != null ? player.GetComponent<PlayerNetworking>() : null;
                if (networking != null && !networking.isMuted)
                    networking.CmdSetMute(true);
                if (_comms == null)
                    _comms = UnityEngine.Object.FindObjectOfType<Dissonance.DissonanceComms>();
                if (_comms != null && !_comms.IsMuted)
                    _comms.IsMuted = true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Big Silence: {ex.Message}");
            }
        }

        private static void EndSilence()
        {
            if (_silenceUntil < 0f)
                return;
            _silenceUntil = -1f;
            try
            {
                var player = LocalPlayer.Find();
                var networking = player != null ? player.GetComponent<PlayerNetworking>() : null;
                if (networking != null && networking.isMuted != _silenceWasMuted)
                    networking.CmdSetMute(_silenceWasMuted);
                if (_comms != null)
                    _comms.IsMuted = _silenceCommsWasMuted;
                Plugin.Log.LogInfo($"{Tag} Big Silence over.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Big Silence end: {ex.Message}");
            }
            _comms = null;
        }
    }
}
