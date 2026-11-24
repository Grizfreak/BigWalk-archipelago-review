using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Holds the nearest push button down while the key is held, by setting its state every frame.
    internal static class DebugTrackedStateHold
    {
        private static TrackedPeckState _held;
        private static bool _wasHolding;

        internal static void Update(bool keyIsDown, string nameFragment, int value, float radius)
        {
            if (!keyIsDown)
            {
                if (_wasHolding)
                {
                    Plugin.Log.LogInfo(
                        $"[{nameof(DebugTrackedStateHold)}] Released '{(_held != null ? _held.gameObject.name : "?")}'.");
                    _wasHolding = false;
                    _held = null;
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(nameFragment))
            {
                if (!_wasHolding)
                {
                    _wasHolding = true;
                    Plugin.Log.LogInfo(
                        $"[{nameof(DebugTrackedStateHold)}] No name configured (Debug/HoldTrackedStateName), nothing to hold.");
                }

                return;
            }

            // Resolved once per hold, so it does not jump to another button as the player moves.
            if (!_wasHolding)
            {
                _wasHolding = true;
                _held = FindNearest(nameFragment, radius);

                if (_held == null)
                {
                    Plugin.Log.LogInfo(
                        $"[{nameof(DebugTrackedStateHold)}] No TrackedPeckState matching '{nameFragment}' within {radius:0}m.");
                    return;
                }

                Plugin.Log.LogInfo(
                    $"[{nameof(DebugTrackedStateHold)}] Holding '{_held.gameObject.name}' at state {value}, let go of the key to release.");
            }

            if (_held == null)
                return;

            try
            {
                _held.SetState(value);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(DebugTrackedStateHold)}] SetState failed, hold abandoned: {ex.Message}");
                _held = null;
            }
        }

        private static TrackedPeckState FindNearest(string nameFragment, float radius)
        {
            var localPlayer = Core.LocalPlayer.Find();
            if (localPlayer == null)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugTrackedStateHold)}] Local player not found.");
                return null;
            }

            var origin = localPlayer.transform.position;
            var all = UnityEngine.Object.FindObjectsByType<TrackedPeckState>(FindObjectsSortMode.None);
            if (all == null || all.Length == 0)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugTrackedStateHold)}] No TrackedPeckState in the current area.");
                return null;
            }

            TrackedPeckState best = null;
            var bestDistance = radius;

            foreach (var state in all)
            {
                if (state == null)
                    continue;

                if (state.gameObject.name.IndexOf(nameFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                float distance;
                try
                {
                    distance = Vector3.Distance(state.transform.position, origin);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(DebugTrackedStateHold)}] Unreadable TrackedPeckState (position): {ex.Message}");
                    continue;
                }

                if (distance >= bestDistance)
                    continue;

                best = state;
                bestDistance = distance;
            }

            if (best != null)
                Plugin.Log.LogInfo($"[{nameof(DebugTrackedStateHold)}] Nearest match: '{best.gameObject.name}' at {bestDistance:0.0}m.");

            return best;
        }
    }
}
