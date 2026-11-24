using System;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Doors
{
    // Tells ArchDoors when to enforce: once a world is ready on the host and slot_data is known.
    internal class ArchDoorUnlocker : MonoBehaviour
    {
        // Required by Il2CppInterop for any injected type.
        public ArchDoorUnlocker(IntPtr ptr) : base(ptr)
        {
        }

        private bool _worldWasReady;
        private bool _enforced;

        private void Update()
        {
            var worldReady = NetworkServer.active && WorldManager.isReadyForEffects;
            if (!worldReady)
            {
                // Only when a world goes away: slot_data usually arrives while one loads.
                if (_worldWasReady)
                    ArchDoors.Forget();

                _worldWasReady = false;
                _enforced = false;
                return;
            }

            if (!_worldWasReady)
            {
                _worldWasReady = true;
                if (!ModConfig.ArchipelagoEnabled.Value)
                    ArchDoors.ConfigureWithoutArchipelago();
            }

            // ArchDoors.Configure enforces on its own if slot_data arrives while a world is up.
            if (_enforced || !ArchDoors.Configured)
                return;

            _enforced = true;
            ArchDoors.Enforce();
        }
    }
}
