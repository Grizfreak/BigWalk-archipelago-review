namespace BigWalkArchipelago.Debug
{
    // Free camera through the game's own cheat camera (cameraCheatMover Detach/Attach).
    internal static class DebugFlightTool
    {
        private static bool _active;
        private static float _originalMovingSpeed;
        private static bool _hasOriginalMovingSpeed;

        internal static bool IsActive => _active;

        internal static void Toggle()
        {
            var localPlayer = Core.LocalPlayer.Find();
            if (localPlayer == null)
            {
                Plugin.Log.LogWarning($"[{nameof(DebugFlightTool)}] No local player found.");
                return;
            }

            var cheater = localPlayer.cheater;
            if (cheater == null)
            {
                Plugin.Log.LogWarning($"[{nameof(DebugFlightTool)}] Local player has no PlayerCheater.");
                return;
            }

            var cameraCheatMover = cheater.cameraCheatMover;
            if (cameraCheatMover == null)
            {
                Plugin.Log.LogWarning($"[{nameof(DebugFlightTool)}] cheater.cameraCheatMover not found.");
                return;
            }

            _active = !_active;
            if (_active)
            {
                cameraCheatMover.Detach();

                // Set again on each activation so a config change applies without a restart.
                if (!_hasOriginalMovingSpeed)
                {
                    _originalMovingSpeed = cameraCheatMover.movingSpeed;
                    _hasOriginalMovingSpeed = true;
                }

                cameraCheatMover.movingSpeed = _originalMovingSpeed * ModConfig.FlightSpeedMultiplier.Value;
            }
            else
            {
                cameraCheatMover.Attach();

                if (_hasOriginalMovingSpeed)
                    cameraCheatMover.movingSpeed = _originalMovingSpeed;
            }

            Plugin.Log.LogInfo($"[{nameof(DebugFlightTool)}] Free camera {(_active ? "enabled" : "disabled")}.");
        }
    }
}
