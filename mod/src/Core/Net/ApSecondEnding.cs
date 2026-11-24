namespace BigWalkArchipelago.Core.Net
{
    // The game saves nothing when the second ending is reached, so the mod latches it in the save.
    internal static class ApSecondEnding
    {
        // Same prefix as ApGoalFlags: no game key starts with it, so SaveValuePatch ignores it.
        private const string ReachedKey = "ap_flag_SecondEnding";

        // The second ending's peck sits under ".../SecondGoodbye PlayerCount2/...".
        private const string SecondEndingPathMarker = "SecondGoodbye";

        internal static bool Reached => IsLatched(ReachedKey);

        // Matched by path: EndingTransition's own hooks are inlined and never fire.
        internal static void OnEndingStartedByPeck(string path)
        {
            var second = path != null
                         && path.Contains(SecondEndingPathMarker, StringComparison.Ordinal);

            Plugin.Log.LogInfo(
                $"[{nameof(ApSecondEnding)}] An ending was started by a peck, on '{path}', "
                + $"{(second ? "the SECOND ending" : "NOT the second ending, left alone")}. "
                + $"(EndingGate latched: {ApGoalFlags.IsLatched(SavableSystem.EndingGate)}, "
                + $"GauntletComplete latched: {ApGoalFlags.IsLatched(SavableSystem.GauntletComplete)})");

            if (second)
                Latch("started by a peck");
        }

        // Log only: no ending has been seen starting this way.
        internal static void OnEndingStartedByZone(string path)
        {
            Plugin.Log.LogInfo(
                $"[{nameof(ApSecondEnding)}] An ending was started by a player zone, on '{path}'. "
                + "Recorded, not goaled.");
        }

        private static void Latch(string how)
        {
            if (IsLatched(ReachedKey))
                return;

            SaveManager.SetIntValue(ReachedKey, 1);
            Plugin.Log.LogInfo(
                $"[{nameof(ApSecondEnding)}] Second ending reached ({how}), latched as {ReachedKey}.");
        }

        private static bool IsLatched(string key)
        {
            return SaveManager.GetIntValue(key, 0, false) != 0;
        }
    }
}
