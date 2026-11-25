using BepInEx.Configuration;

namespace BigWalkArchipelago
{
    internal static partial class ModConfig
    {
        internal static ConfigEntry<bool> DebugModeEnabled;

        // Debug settings live in their own file, written only when Debug.Enabled is on.
        private const string DebugFileName = "com.grizfreak.bigwalk.archipelago.debug.cfg";

        private static void BindDebug(ConfigFile file)
        {
            DebugModeEnabled = file.Bind("Debug", "Enabled", false, "Enables the debug tools (free camera, etc).");

            var debug = new ConfigFile(System.IO.Path.Combine(BepInEx.Paths.ConfigPath, DebugFileName), false)
            {
                SaveOnConfigSet = DebugModeEnabled.Value,
            };
            BindDebugWorldKeys(debug);
            BindDebugSessionKeys(debug);
            if (DebugModeEnabled.Value)
                debug.Save();
        }
    }
}
