using System.Text;
using BigWalkArchipelago.Core.Net;
using HarmonyLib;
using UnityEngine;

namespace BigWalkArchipelago.Patches.Checks
{
    // Hooks where an ending starts: EndingTransition.OnTransitionEnd is inlined and never fires.
    internal static class EndingStartPatch
    {
        [HarmonyPatch(typeof(PeckEffectEndingTransition), "OnPeck")]
        internal static class ByPeck
        {
            private static void Prefix(PeckEffectEndingTransition __instance)
            {
                if (__instance == null)
                    return;

                ApSecondEnding.OnEndingStartedByPeck(PathOf(__instance.gameObject));
            }
        }

        [HarmonyPatch(typeof(AutomaticDisconnector), nameof(AutomaticDisconnector.StartEndingTransition))]
        internal static class ByZone
        {
            private static void Prefix(AutomaticDisconnector __instance)
            {
                if (__instance == null)
                    return;

                ApSecondEnding.OnEndingStartedByZone(PathOf(__instance.gameObject));
            }
        }

        // Object names repeat all over the game, so only the full path identifies one.
        private static string PathOf(GameObject go)
        {
            if (go == null)
                return "<null>";

            var path = new StringBuilder(go.name);
            var parent = go.transform != null ? go.transform.parent : null;
            while (parent != null)
            {
                path.Insert(0, parent.gameObject.name + "/");
                parent = parent.parent;
            }

            return path.ToString();
        }
    }
}
