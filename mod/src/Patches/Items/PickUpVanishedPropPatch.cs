using System;
using HarmonyLib;
using Mirror;

namespace BigWalkArchipelago.Patches.Items
{
    // Refuses a pick-up of a prop the mod made vanish, or the Command throws and the player sticks.
    [HarmonyPatch(typeof(PlayerNetworking), "UserCode_CmdPickUp__PlayerHeldInformation")]
    internal static class PickUpVanishedPropPatch
    {
        private static bool Prefix(PlayerNetworking __instance, PlayerHeldInformation heldInformation)
        {
            try
            {
                if (heldInformation == null
                    || heldInformation.heldType != PlayerHeldInformation.HeldType.Prop
                    || IsStillThere(heldInformation.identity))
                    return true;

                heldInformation.identity = null;
                heldInformation.heldType = PlayerHeldInformation.HeldType.Nothing;
                heldInformation.hasDropData = false;
                heldInformation.isResultOfSnatch = false;
                __instance.NetworkplayerHeldInformation = heldInformation;

                Plugin.Log.LogInfo(
                    $"[{nameof(PickUpVanishedPropPatch)}] {__instance.gameObject.name} reached for a prop that is gone; "
                    + "told them they hold nothing.");
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(PickUpVanishedPropPatch)}] Check failed, the game handles this pick-up: {ex.Message}");
                return true;
            }
        }

        private static bool IsStillThere(NetworkIdentity identity)
        {
            return identity != null
                && identity.netId != 0
                && identity.gameObject.activeInHierarchy;
        }
    }
}
