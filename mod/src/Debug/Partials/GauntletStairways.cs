namespace BigWalkArchipelago.Core.Doors
{
    internal static partial class GauntletStairways
    {
        internal static void DebugOpenLast()
        {
            Discover();
            var gate = GateByLevel[Chambers.Length - 1];
            if (gate == null)
            {
                Plugin.Log.LogInfo($"[{nameof(GauntletStairways)}] Debug: the last stairway is not in this world.");
                return;
            }

            Plugin.Log.LogInfo($"[{nameof(GauntletStairways)}] Debug: asking the last stairway to open (state {gate.currentPeckContext.state}).");
            gate.SetState(1);
            Plugin.Log.LogInfo($"[{nameof(GauntletStairways)}] Debug: the last stairway is now at state {gate.currentPeckContext.state}.");
        }
    }
}
