using BigWalkArchipelago.Core;
using HarmonyLib;

namespace BigWalkArchipelago.Patches
{
    // `tower_mode`: a monument that is not the next one refuses a gourd, on every machine.
    [HarmonyPatch(typeof(PropHome), nameof(PropHome.IsSafeToPlace))]
    internal static class MonumentPlacePatch
    {
        private static void Postfix(PropHome __instance, ref bool __result)
        {
            if (!__result || !MonumentOrder.Active)
                return;

            if (MonumentOrder.IsClosed(__instance))
            {
                __result = false;
                ApNoticesLine.MonumentClosed();
            }
        }
    }

    // IsSafeToPlace is not asked for every pin, so the host refuses the pin itself too.
    [HarmonyPatch(typeof(PropHome), nameof(PropHome.SetPinned))]
    internal static class MonumentPinPatch
    {
        private static bool Prefix(PropHome __instance, Prop __0)
        {
            if (__0 == null || !MonumentOrder.Active || !Mirror.NetworkServer.active)
                return true;

            if (!MonumentOrder.IsClosed(__instance))
                return true;

            Plugin.Log.LogInfo(
                $"[{nameof(MonumentPinPatch)}] {__0.name} refused in {__instance.saveableHomeName}: that monument is not open.");
            ApNoticesLine.MonumentClosed();
            return false;
        }
    }

    // Placing a prop in a home goes through ServerSetPinned, not PropHome.SetPinned.
    [HarmonyPatch(typeof(Prop), nameof(Prop.ServerSetPinned))]
    internal static class MonumentServerPinPatch
    {
        private static bool Prefix(Prop __instance, PropHome __0)
        {
            if (__0 == null || !MonumentOrder.Active || !Mirror.NetworkServer.active)
                return true;

            if (!MonumentOrder.IsClosed(__0))
                return true;

            Plugin.Log.LogInfo(
                $"[{nameof(MonumentServerPinPatch)}] {__instance.name} refused in {__0.saveableHomeName}: that monument is not open.");
            ApNoticesLine.MonumentClosed();
            return false;
        }
    }

    // A closed tower's button refuses the state raise, like a Gauntlet stairway.
    [HarmonyPatch(typeof(TrackedPeckState), nameof(TrackedPeckState.SetState), new[] { typeof(PeckContext) })]
    internal static class TowerButtonHoldPatch
    {
        private static bool Prefix(TrackedPeckState __instance, PeckContext __0)
        {
            if (!MonumentOrder.Active || __0 == null || __0.compressedState < 1)
                return true;

            if (!MonumentOrder.HoldsButton(__instance))
                return true;

            Plugin.Log.LogInfo(
                $"[{nameof(TowerButtonHoldPatch)}] {__instance.savableSystem} stays still: that tower's monument is not open.");
            ApNoticesLine.MonumentClosed();
            return false;
        }
    }

    // `lock_endings_until_objectives` with big_wall: the chapel bell waits for the objectives.
    [HarmonyPatch(typeof(TrackedPeckState), nameof(TrackedPeckState.SetState), new[] { typeof(PeckContext) })]
    internal static class ChapelBellHoldPatch
    {
        private static bool Prefix(TrackedPeckState __instance, PeckContext __0)
        {
            // Not while the world loads: that is the save's state coming back, not a ring.
            if (__0 == null || __0.compressedState < 1 || __instance.savableSystem != SavableSystem.EndingGate
                || !WorldManager.isReadyForEffects)
                return true;

            if (!BigWalkArchipelago.Core.ObjectiveBoard.HoldsTheLastStairway || !BigWalkArchipelago.Core.ObjectiveBoard.Locked("big_wall"))
                return true;

            Plugin.Log.LogInfo($"[{nameof(ChapelBellHoldPatch)}] The bell stays still: the objectives are not done.");
            BigWalkArchipelago.Core.ObjectiveBoard.SayEndingIsShut("big_wall");
            return false;
        }
    }

    // One line on the screen when something is refused, at most one every few seconds.
    internal static class ApNoticesLine
    {
        private static float _saidAt = -100f;

        internal static void MonumentClosed()
        {
            if (UnityEngine.Time.unscaledTime - _saidAt < 6f)
                return;

            _saidAt = UnityEngine.Time.unscaledTime;
            BigWalkArchipelago.Core.Net.ApNotices.Post("That monument is not open yet: they open one after the other", warning: true);
        }
    }
}
