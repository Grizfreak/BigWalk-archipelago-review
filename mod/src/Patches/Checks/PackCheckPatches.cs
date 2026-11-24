using HarmonyLib;
using Mirror;

namespace BigWalkArchipelago.Patches.Checks
{
    // Runs on the host after the pick-up Command; the held information then names the prop.
    [HarmonyPatch(typeof(PlayerNetworking), "UserCode_CmdPickUp__PlayerHeldInformation")]
    internal static class PackPickUpPatch
    {
        private static void Postfix(PlayerNetworking __instance, PlayerHeldInformation heldInformation)
        {
            try
            {
                if (!NetworkServer.active || heldInformation == null
                    || heldInformation.heldType != PlayerHeldInformation.HeldType.Prop || heldInformation.identity == null)
                    return;

                var held = __instance.playerHeldInformation;
                if (held == null || held.identity == null || held.identity.netId != heldInformation.identity.netId)
                    return;

                var prop = heldInformation.identity.GetComponent<Prop>();
                PackChecks.OnPickedUp(prop);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(PackPickUpPatch)}] {ex.Message}");
            }
        }
    }
}
