namespace BigWalkArchipelago.Debug
{
    // Runs the variant gourd reveal by hand, without waiting for the map unlocker's next poll.
    internal static class DebugVariantGourdReveal
    {
        internal static void RevealAll()
        {
            var count = VariantGourdRevealer.RevealAll();
            Plugin.Log.LogInfo($"[{nameof(DebugVariantGourdReveal)}] {count} variant challenge gourd(s) revealed on the map.");
        }
    }
}
