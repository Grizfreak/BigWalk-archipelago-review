using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BigWalkArchipelago.Core;
using HarmonyLib;

// References the BepInEx Il2CppInterop assemblies, never the Il2CppInspector dummy DLLs.

namespace BigWalkArchipelago
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BasePlugin
    {
        // Also names the .cfg file: changing it loses the user's settings to a fresh file.
        public const string PluginGuid = "com.grizfreak.bigwalk.archipelago";
        public const string PluginName = "Big Walk Archipelago";
        public const string PluginVersion = "1.0.15";

        // BasePlugin.Log is an instance property; a static one lets patches log without a Plugin.
        internal static new ManualLogSource Log;

        internal static ICheckReporter Reporter { get; private set; }

        private Harmony _harmony;

        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo($"{PluginName} v{PluginVersion} loaded (build {BuildFingerprint()}).");

            ModConfig.Bind(base.Config);

#if DEBUG
            // First, because the loopback guest without the mod loads almost nothing.
            if (ModConfig.DebugModeEnabled.Value)
            {
                Debug.LoopbackGuest.DetectRole();
                if (Debug.LoopbackGuest.IsVanilla)
                {
                    LoadAsGuestWithoutTheMod();
                    return;
                }
            }
#endif

            Reporter = new Core.Net.ApReporter();

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();
            Patches.PlayerCountProceedLog.TryApply(_harmony);
            Patches.BigRunCompat.TryApply(_harmony);
            Patches.AudioBehaviourGuard.TryApply(_harmony);
            Core.ErrorFloodWatch.Install();
#if DEBUG
            Debug.PerfProbe.TryApply(_harmony);
#endif

            AddComponent<Core.Net.ApRuntime>();
            AddComponent<Core.Net.ApStatusOverlay>();
            AddComponent<Core.ArchDoorUnlocker>();
            AddComponent<Core.GauntletStairwayEnforcer>();
            AddComponent<Core.VariantGourdMapUnlocker>();
            AddComponent<Core.SecondEndingSphereUnlocker>();
            AddComponent<Core.CosmeticMonumentFillTracker>();
            AddComponent<Core.CosmeticGourdSpawnHandler>();
            AddComponent<Core.VanillaGadgetRemover>();
            AddComponent<Core.GadgetSpawnHandler>();
            AddComponent<Core.StaleHeldPropReleaser>();
            AddComponent<Core.PuzzleGourdRetirer>();
            AddComponent<Core.PuzzleNeedHider>();
            AddComponent<Core.AbilityLockEnforcer>();
            AddComponent<Core.WorldButtons>();
            AddComponent<Core.TeleportButtonRunner>();
            AddComponent<Core.PegTileSpawnHandler>();
            AddComponent<Core.KeyColourPainter>();
            AddComponent<Core.Net.ModChannel>();
            AddComponent<Core.TrapRunner>();
            AddComponent<Core.ModVersionLabel>();

#if DEBUG
            if (ModConfig.DebugModeEnabled.Value)
            {
                Debug.LoopbackGuest.TryApply(_harmony);
                if (Debug.LoopbackGuest.IsGuest)
                    AddComponent<Debug.LoopbackGuestMonitor>();
                AddComponent<Debug.DebugHotkeys>();
                AddComponent<Debug.DebugGadgetRigDump>();
                AddComponent<Debug.DebugWorldLayoutDump>();
                AddComponent<Debug.DebugPuzzleMapDump>();
                AddComponent<Debug.DebugPuzzleNeedsDump>();
                AddComponent<Debug.DebugPuzzleTour>();
                AddComponent<Debug.DebugLogicTour>();
                AddComponent<Debug.DebugGauntletTools>();
                AddComponent<Debug.DebugRecon07>();
                AddComponent<Debug.AutoHost>();
                Log.LogInfo($"[Debug] Debug module active (key: {ModConfig.ToggleFlightKey.Value}).");
            }
#endif

            Log.LogInfo("Harmony initialized.");
        }

#if DEBUG
        // A loopback guest standing in for a console player: no patches, no mod components.
        private void LoadAsGuestWithoutTheMod()
        {
            _harmony = new Harmony(PluginGuid);
            Debug.LoopbackGuest.TryApply(_harmony);
            AddComponent<Debug.LoopbackGuestMonitor>();
            AddComponent<Debug.DebugHotkeys>();
            Log.LogInfo(
                "[LoopbackGuest] Without the mod: only the join is loaded (no patches, no spawn handlers, "
                + "no ModChannel). This instance meets the host as a console player would.");
        }
#endif

        // First 16 hex digits of this DLL's SHA-256, as tools/deploy-mod.ps1 prints it.
        private static string BuildFingerprint()
        {
            try
            {
                var path = typeof(Plugin).Assembly.Location;
                using var sha = System.Security.Cryptography.SHA256.Create();
                using var stream = System.IO.File.OpenRead(path);
                return System.BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").Substring(0, 16);
            }
            catch (System.Exception)
            {
                return "unknown";
            }
        }
    }
}
