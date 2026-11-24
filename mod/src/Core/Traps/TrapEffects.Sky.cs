using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal static partial class TrapEffects
    {
        private const float MidnightHour = 0f;
        private const float NoonHour = 12f;

        private const float FastForwardSeconds = 4f;
        private static bool _skyRunning;
        private static float _skyFrom;
        private static float _skyTo;
        private static float _skySpan;
        private static float _skyStart;

        // The clock fast-forwards to the hour, never backwards, then runs on; nothing is put back.
        private static void StartSky(float hour)
        {
            _skyFrom = SkyManager.GetCurrentTime();
            _skyTo = hour;
            _skySpan = Mathf.Repeat(hour - _skyFrom, 24f);
            _skyStart = Time.unscaledTime;
            _skyRunning = true;
            Plugin.Log.LogInfo($"{Tag} Sky: fast forward from {_skyFrom:F2}h to {hour:F2}h.");
        }

        private static void StepSky()
        {
            var t = Mathf.Clamp01((Time.unscaledTime - _skyStart) / FastForwardSeconds);
            try
            {
                // Eased, so the sun speeds up and settles rather than jerking.
                var eased = t * t * (3f - 2f * t);
                SkyManager.SetFixedTime(Mathf.Repeat(_skyFrom + _skySpan * eased, 24f));
                if (t < 1f)
                    return;

                _skyRunning = false;
                SkyManager.ClearFixedTime();
                Plugin.Log.LogInfo($"{Tag} Sky at {_skyTo:F2}h; the clock runs on from there.");
            }
            catch (Exception ex)
            {
                _skyRunning = false;
                Plugin.Log.LogInfo($"{Tag} Sky fast forward stopped (no sky now: {ex.Message}).");
            }
        }
    }
}
