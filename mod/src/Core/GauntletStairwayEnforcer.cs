using System;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // The game does not save opened stairways, so this reopens the slot's ones every few seconds.
    internal class GauntletStairwayEnforcer : MonoBehaviour
    {
        // Constructor required by Il2CppInterop for any type injected into IL2CPP.
        public GauntletStairwayEnforcer(IntPtr ptr) : base(ptr)
        {
        }

        private const float IntervalSeconds = 2f;

        private bool _worldWasReady;
        private float _nextAt;

        private void Update()
        {
            var worldReady = NetworkServer.active && WorldManager.isReadyForEffects;
            if (!worldReady)
            {
                if (_worldWasReady)
                    GauntletStairways.ForgetGates();

                _worldWasReady = false;
                return;
            }

            if (!_worldWasReady && !ModConfig.ArchipelagoEnabled.Value)
                GauntletStairways.Forget();

            _worldWasReady = true;
            if (Time.unscaledTime < _nextAt)
                return;

            _nextAt = Time.unscaledTime + IntervalSeconds;
            GauntletStairways.Enforce();
        }
    }
}
