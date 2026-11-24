using System;
using System.Collections.Generic;
using System.Linq;
using BigWalkArchipelago.Core.Net;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    // Runs the effect clocks, the host's queue, DeathLink triggers and the guests' mask status.
    internal sealed class TrapRunner : MonoBehaviour
    {
        public TrapRunner(IntPtr ptr) : base(ptr)
        {
        }

        private static bool _worldWasReady;
        private static float _nextStatus;
        private static bool _lastMaskedSent;

        // After the animation has placed the bodies: what must win over it (Big Head's lift).
        private void LateUpdate()
        {
            try
            {
                TrapEffects.LateTick();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(TrapRunner)}] {ex.Message}");
            }
        }

        private void Update()
        {
            try
            {
                var ready = WorldManager.isReadyForEffects;
                if (!ready && _worldWasReady)
                {
                    TrapEffects.EndAll();
                    Traps.ForgetWorld();
                }

                if (ready && !_worldWasReady && NetworkServer.active)
                    Traps.OnWorldReady();

                _worldWasReady = ready;
                TrapEffects.Tick();
                PackChecks.Tick();
                ZoneLocks.Tick();
                MonumentOrder.TickFullChecks();
                FastGolf.Tick();
                ColourPainter.Tick();
                HintBoards.Tick();
                MapTracker.Tick();
                BigFlare.Tick();
                SettingsMenuArchipelagoRows.KeepPlaced();

                if (NetworkServer.active)
                {
                    ObjectiveSpots.TickCountdown();
                    Traps.TickHost(ApRuntime.SessionSettled);
                }
                else
                {
                    Traps.ForgetGuests();
                    if (NetworkClient.active)
                        TellHostAboutMask();
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(TrapRunner)}] {ex.Message}");
            }
        }

        // On a change, and again every few seconds in case one was missed.
        private static void TellHostAboutMask()
        {
            var masked = TrapEffects.LocalMasked;
            if (masked == _lastMaskedSent && Time.unscaledTime < _nextStatus)
                return;

            if (ModChannel.SendStatus(masked))
            {
                _lastMaskedSent = masked;
                _nextStatus = Time.unscaledTime + 5f;
            }
        }
    }
}
