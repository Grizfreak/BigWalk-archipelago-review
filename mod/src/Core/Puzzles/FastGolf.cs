using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Puzzles
{
    // fast_golf: removes PropGroup.TooHeavy from the cannonballs, again later for late props.
    internal static class FastGolf
    {
        private const string Tag = "[" + nameof(FastGolf) + "]";
        private const string BallPrefab = "cannonballProp";
        private static readonly float[] SweepsAfter = { 5f, 30f, 120f };

        private static bool _mirroredLightweight;
        private static bool _mirrored;
        private static float _readySince = -1f;
        private static int _sweepsDone;

        private static bool IsGuest => NetworkClient.active && !NetworkServer.active;

        internal static bool HostLightweight => ApRuntime.SlotData != null && ApRuntime.SlotData.FastGolf;

        private static bool Lightweight => IsGuest ? _mirrored && _mirroredLightweight : HostLightweight;

        internal static void ApplyFromHost(bool lightweight)
        {
            if (_mirrored && _mirroredLightweight == lightweight)
                return;
            _mirrored = true;
            _mirroredLightweight = lightweight;
            _sweepsDone = 0;
        }

        internal static void ForgetMirror()
        {
            _mirrored = false;
            _mirroredLightweight = false;
        }

        // Every frame from TrapRunner: cheap until a sweep is due.
        internal static void Tick()
        {
            if (!WorldManager.isReadyForEffects)
            {
                _readySince = -1f;
                _sweepsDone = 0;
                return;
            }

            if (_readySince < 0f)
                _readySince = Time.unscaledTime;
            if (_sweepsDone >= SweepsAfter.Length || Time.unscaledTime - _readySince < SweepsAfter[_sweepsDone] || !Lightweight)
                return;

            _sweepsDone++;
            var lightened = 0;
            try
            {
                foreach (var prop in UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (prop == null || !prop.gameObject.name.StartsWith(BallPrefab, StringComparison.Ordinal) || prop.propGroups == null)
                        continue;
                    while (prop.propGroups.Remove(PropGroup.TooHeavy))
                        lightened++;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} {ex.Message}");
            }

            if (lightened > 0)
                Plugin.Log.LogInfo($"{Tag} {lightened} golf ball(s) no longer too heavy.");
        }
    }
}
