using HarmonyLib;

namespace BigWalkArchipelago.Patches.Effects
{
    // Big Ghost trap: runs after the game sets the player's whiteness, so the trap wins the frame.
    [HarmonyPatch(typeof(PlayerSpeechless), nameof(PlayerSpeechless.Update))]
    internal static class BigGhostPatch
    {
        private static void Postfix(PlayerSpeechless __instance)
        {
            if (!TrapEffects.GhostActive)
                return;
            var player = __instance.playerCharacter;
            if (player != null && player.looks != null)
                player.looks.SetSpeechlessness(1f);
        }
    }
}
