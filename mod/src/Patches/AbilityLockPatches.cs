using System;
using BigWalkArchipelago.Core;
using HarmonyLib;

namespace BigWalkArchipelago.Patches
{
    // The click also picks up and drops, so pointing is refused here instead of in Rewired.
    [HarmonyPatch(typeof(PlayerGestures), nameof(PlayerGestures.UpdateSingleArm))]
    internal static class ArmGestureLockPatch
    {
        private static readonly System.Collections.Generic.HashSet<string> Logged = new();

        internal static void LogOnce(string what)
        {
            if (Logged.Add(what))
                Plugin.Log.LogInfo($"[{nameof(ArmGestureLockPatch)}] {what}");
        }

        private static void Prefix(PlayerGestures __instance, PlayerGestures.GestureArmParams gestureArmParams, bool isWaving, ref bool isPointing)
        {
            try
            {
                // Every player drawn here: a remote player's pointing gets through otherwise.
                if (!isPointing || !AbilityLocks.Active || __instance?.playerCharacter == null)
                    return;

                var left = gestureArmParams.layerIndex == __instance.leftArmParams.layerIndex;
                var ability = isWaving
                    ? (left ? AbilityLocks.LeftExtending : AbilityLocks.RightExtending)
                    : (left ? AbilityLocks.LeftPointing : AbilityLocks.RightPointing);
                if (AbilityLocks.IsLocked(ability))
                {
                    isPointing = false;
                    LogOnce($"refused {ability} for {(__instance.playerCharacter.isLocalPlayer ? "the local player" : "a remote player")}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ArmGestureLockPatch)}] {ex.Message}");
            }
        }
    }

    // PlayerGestures.Update copies the pointing flag to the network and sounds; refuse it before.
    [HarmonyPatch(typeof(PlayerGestures), nameof(PlayerGestures.Update))]
    internal static class LocalPointingLockPatch
    {
        private static void Prefix(PlayerGestures __instance)
        {
            try
            {
                if (!AbilityLocks.Active || __instance?.playerCharacter == null || !__instance.playerCharacter.isLocalPlayer)
                    return;

                var decisions = __instance.playerCharacter.decisions;
                if (decisions == null)
                    return;

                if (decisions.leftHandIsPointing && IsLocked(__instance.leftArmWavingState.isActive, left: true))
                {
                    decisions.leftHandIsPointing = false;
                    ArmGestureLockPatch.LogOnce("refused left pointing at the source for the local player");
                }

                if (decisions.rightHandIsPointing && IsLocked(__instance.rightArmWavingState.isActive, left: false))
                {
                    decisions.rightHandIsPointing = false;
                    ArmGestureLockPatch.LogOnce("refused right pointing at the source for the local player");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(LocalPointingLockPatch)}] {ex.Message}");
            }
        }

        private static bool IsLocked(bool waving, bool left)
        {
            var ability = waving
                ? (left ? AbilityLocks.LeftExtending : AbilityLocks.RightExtending)
                : (left ? AbilityLocks.LeftPointing : AbilityLocks.RightPointing);
            return AbilityLocks.IsLocked(ability);
        }
    }

    // Refused for furniture only; the chairlift and train too with `rides_require_seat`.
    [HarmonyPatch(typeof(PlayerPoser), nameof(PlayerPoser.EnterPose))]
    internal static class ChairSittingLockPatch
    {
        private static bool Prefix(PlayerPose pose)
        {
            try
            {
                if (!AbilityLocks.IsLocked(AbilityLocks.ChairSitting))
                    return true;

                var refused = AbilityLocks.IsFurniture(pose)
                              || (AbilityLocks.IsLocked(AbilityLocks.RideSitting) && AbilityLocks.IsRide(pose));
                // Logged both ways, so a seat the lock gets wrong can be named.
                Plugin.Log.LogInfo(
                    $"[{nameof(ChairSittingLockPatch)}] '{AbilityLocks.Describe(pose)}' "
                    + (refused ? "refused: Chair Sitting is locked." : "let through: not furniture."));
                return !refused;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ChairSittingLockPatch)}] {ex.Message}");
                return true;
            }
        }
    }

    // With no wind-up a throw is a plain drop, so the object still leaves the hand.
    [HarmonyPatch(typeof(PlayerHeldInformation), nameof(PlayerHeldInformation.ThrowInfo))]
    internal static class ThrowLockPatch
    {
        private static void Prefix(ref float windUp)
        {
            if (windUp > 0f && AbilityLocks.IsLocked(AbilityLocks.Throwing) && !TrapEffects.TrapIsThrowing)
                windUp = 0f;
        }
    }

    [HarmonyPatch(typeof(PlayerDecisions), nameof(PlayerDecisions.SetWindUpStartTime))]
    internal static class WindUpLockPatch
    {
        private static bool Prefix() => !AbilityLocks.IsLocked(AbilityLocks.Throwing) || TrapEffects.TrapIsThrowing;
    }

    // The local ActionPickUpPlayer is not the path the game uses; every carry passes here.
    [HarmonyPatch(typeof(PlayerNetworking), "UserCode_CmdPickUpPlayer__PlayerCharacter")]
    internal static class CarryLockPatch
    {
        private static bool Prefix()
        {
            if (!Mirror.NetworkServer.active || !AbilityLocks.IsLocked(AbilityLocks.Carrying))
                return true;

            Plugin.Log.LogInfo($"[{nameof(CarryLockPatch)}] Refused: Carrying is locked.");
            return false;
        }
    }

    // Clearing the aimed player hides the pick-up outline, so no carry is asked for.
    [HarmonyPatch(typeof(PlayerCaster), nameof(PlayerCaster.LocalUpdate))]
    internal static class CarryAimLockPatch
    {
        private static void Postfix(PlayerCaster __instance)
        {
            if (__instance != null && __instance.castCharacter != null && AbilityLocks.IsLocked(AbilityLocks.Carrying))
                __instance.castCharacter = null;
        }
    }
}
