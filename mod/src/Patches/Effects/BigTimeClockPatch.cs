using HarmonyLib;
using UnityEngine;

namespace BigWalkArchipelago.Patches.Effects
{
    // Big Time clocks show this machine's real time: black hand for hours, red hand for minutes.
    [HarmonyPatch(typeof(LobbyClock), nameof(LobbyClock.Update))]
    internal static class BigTimeClockPatch
    {
        private static bool Prefix(LobbyClock __instance)
        {
            try
            {
                if (__instance == null || !__instance.gameObject.name.Contains(ReceivedItemSpawner.CosmeticNameSuffix, StringComparison.Ordinal))
                    return true;

                var now = DateTime.Now;
                var hours = (now.Hour % 12 + now.Minute / 60f) * 30f;
                var minutes = (now.Minute + now.Second / 60f) * 6f;
                if (__instance.hourHand != null)
                    __instance.hourHand.localEulerAngles = new Vector3(0f, 0f, hours);
                if (__instance.secondHand != null)
                    __instance.secondHand.localEulerAngles = new Vector3(0f, 0f, minutes);
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
