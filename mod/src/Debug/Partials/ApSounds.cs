namespace BigWalkArchipelago.Core.Net
{
    internal static partial class ApSounds
    {
        // For the debug key: the next sound plays whatever just played.
        internal static void ForgetBursts()
        {
            _lastReceived = -100f;
            _lastCheck = -100f;
            _lastDeathLinkSound = -100f;
        }
    }
}
