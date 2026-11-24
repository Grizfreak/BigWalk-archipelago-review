using BigWalkArchipelago.Core;
using HarmonyLib;

namespace BigWalkArchipelago.Patches
{
    // Falls for DeathLink: the host sees its own in TriggerFall and its guests' in ShowFall.
    [HarmonyPatch(typeof(PlayerFaller), nameof(PlayerFaller.TriggerFall))]
    internal static class PlayerFallPatch
    {
        private static void Postfix(PlayerFaller __instance)
        {
            if (Mirror.NetworkServer.active && __instance != null)
                Traps.OnFall(__instance.playerCharacter);
        }
    }

    // Not ProcessRemoteFall: the host never runs it for a guest.
    [HarmonyPatch(typeof(PlayerFaller), nameof(PlayerFaller.ShowFall))]
    internal static class RemoteFallPatch
    {
        private static void Postfix(PlayerFaller __instance)
        {
            var player = __instance != null ? __instance.playerCharacter : null;
            if (!Mirror.NetworkServer.active || player == null)
                return;

            var identity = player.GetComponent<Mirror.NetworkIdentity>();
            if (identity == null || !identity.isLocalPlayer)
                Traps.OnFall(player);
        }
    }

    // Puzzle failures for DeathLink go through PeckSwitch.Peck; patch both overloads.
    [HarmonyPatch(typeof(PeckSwitch), nameof(PeckSwitch.Peck), new System.Type[] { typeof(PeckContext) })]
    internal static class PuzzleFailurePatch
    {
        private static void Postfix(PeckSwitch __instance, PeckContext __0)
        {
            PuzzleFailures.OnPeck(__instance, __0 != null && __0.playerIdentity == null && __0.actionNumber == -1);
            ObjectiveSpots.OnPeck(__instance);
        }
    }

    [HarmonyPatch(typeof(PeckSwitch), nameof(PeckSwitch.Peck), new System.Type[0])]
    internal static class PuzzleFailurePlainPatch
    {
        private static void Postfix(PeckSwitch __instance)
        {
            PuzzleFailures.OnPeck(__instance, false);
            ObjectiveSpots.OnPeck(__instance);
        }
    }

    // Big Sleep: skip the local crouch input so the posture stays as it was.
    [HarmonyPatch(typeof(PlayerCroucher), nameof(PlayerCroucher.UpdateLocal))]
    internal static class SleepCrouchPatch
    {
        private static bool Prefix()
        {
            return !TrapEffects.Asleep;
        }
    }

    // Puzzle screen masks: Big Trip and Big Meeting leave a masked player where they are.
    [HarmonyPatch(typeof(PeckEffectMask), nameof(PeckEffectMask.SetMask))]
    internal static class ScreenMaskPatch
    {
        private static void Postfix(PeckEffectMask __instance, bool __0)
        {
            TrapEffects.NoteMask(__instance, __0);
        }
    }
}
