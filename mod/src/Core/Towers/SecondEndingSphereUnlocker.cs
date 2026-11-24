using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Towers
{
    // Hides the hub's black sphere to the second ending; SetActive is not saved, so redo per world.
    internal class SecondEndingSphereUnlocker : MonoBehaviour
    {
        // Required by Il2CppInterop for any type injected into IL2CPP.
        public SecondEndingSphereUnlocker(IntPtr ptr) : base(ptr)
        {
        }

        private const string BlockingObjectNamePrefix = "Spawn_SecondEnding_Sphere";

            // Re-armed per world: loading another save reloads the world without a restart.
        private bool _worldWasReady;

        private void Update()
        {
            // Not host-gated: SetActive is not replicated, so every client hides its own copy.
            var worldReady = WorldManager.isReadyForEffects;
            if (!worldReady)
            {
                _worldWasReady = false;
                return;
            }

            if (_worldWasReady)
                return;

            _worldWasReady = true;

            var disabled = new List<string>();
            var allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t == null)
                    continue;

                var go = t.gameObject;
                if (!go.activeSelf)
                    continue;

                if (!go.name.StartsWith(BlockingObjectNamePrefix, StringComparison.Ordinal))
                    continue;

                go.SetActive(false);
                disabled.Add(go.name);
            }

            if (disabled.Count == 0)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(SecondEndingSphereUnlocker)}] No object '{BlockingObjectNamePrefix}*' found in the current zone (hub not loaded?).");
                return;
            }

            Plugin.Log.LogInfo(
                $"[{nameof(SecondEndingSphereUnlocker)}] {disabled.Count} object(s) disabled: {string.Join(", ", disabled)}.");
        }
    }
}
