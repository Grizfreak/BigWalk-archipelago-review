using System;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // SkipAidToggler reads `skipAidsActive` only on wake, so set it and wake each target by hand.
    internal static class DebugSkipAids
    {
        internal static void EnableAll()
        {
            try
            {
                SkipAidToggler.skipAidsActive = true;

                var togglers = UnityEngine.Object.FindObjectsByType<SkipAidToggler>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                var woken = 0;
                if (togglers != null)
                {
                    foreach (var toggler in togglers)
                    {
                        if (toggler == null || toggler.target == null || toggler.target.gameObject.activeSelf)
                            continue;

                        toggler.target.gameObject.SetActive(true);
                        woken++;
                    }
                }

                Plugin.Log.LogInfo(
                    $"[{nameof(DebugSkipAids)}] skipAidsActive is on; {woken} skip aid(s) switched on "
                    + $"of {(togglers != null ? togglers.Length : 0)}.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(DebugSkipAids)}] Could not switch the skip aids on: {ex.Message}");
            }
        }
    }
}
