using HarmonyLib;

namespace BigWalkArchipelago.Patches.Effects
{
    // Big Smell's push, added to the controls' velocity so the game's movement stops at walls.
    [HarmonyPatch(typeof(PlayerMover), nameof(PlayerMover.LocalFixedUpdate))]
    internal static class BigSmellPatch
    {
        private static void Postfix(PlayerMover __instance)
        {
            try
            {
                var push = TrapEffects.SmellVelocity;
                if (push.sqrMagnitude > 0f)
                    __instance.localControlsVelocity += push;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(BigSmellPatch)}] {ex.Message}");
            }
        }
    }
}
