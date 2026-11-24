using System;
using UnityEngine;

namespace BigWalkArchipelago.Core.Items
{
    // Hides the vanilla gadgets used as filler items, locally on each machine after a capture.
    internal class VanillaGadgetRemover : MonoBehaviour
    {
        // Constructor required by Il2CppInterop for any type injected into IL2CPP.
        public VanillaGadgetRemover(IntPtr ptr) : base(ptr)
        {
        }

        private bool _worldWasReady;
        private float _nextRecheck;

        private const float RecheckIntervalSeconds = 1f;

        private void Update()
        {
            var worldReady = WorldManager.isReadyForEffects;
            if (!worldReady)
            {
                _worldWasReady = false;
                GadgetItemSpawner.ForgetWorld();
                return;
            }

            if (_worldWasReady)
            {
                // Mirror re-enables a scene object it spawns, so a late spawn can unhide a gadget.
                if (ModConfig.ArchipelagoEnabled.Value && Time.time >= _nextRecheck)
                {
                    _nextRecheck = Time.time + RecheckIntervalSeconds;
                    GadgetItemSpawner.ReHideVanillaInstances();
                }

                return;
            }

            _worldWasReady = true;
            _nextRecheck = Time.time + RecheckIntervalSeconds;

            GadgetItemSpawner.CaptureTemplates();

            // Off without Archipelago: there are no filler items to stand in for the removed props.
            if (!ModConfig.ArchipelagoEnabled.Value)
                return;

            GadgetItemSpawner.HideVanillaInstances();
        }
    }
}
