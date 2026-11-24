using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP.Configuration;
using UnityEngine;

namespace BigWalkArchipelago
{
    internal static partial class ModConfig
    {
        internal static ConfigEntry<KeyboardShortcut> HoldTrackedStateKey;
        internal static ConfigEntry<string> HoldTrackedStateName;
        internal static ConfigEntry<int> HoldTrackedStateValue;
        internal static ConfigEntry<KeyboardShortcut> ToggleFlightKey;
        internal static ConfigEntry<KeyboardShortcut> UnlockNextKey;
        internal static ConfigEntry<KeyboardShortcut> SimulateReceivedItemKey;
        internal static ConfigEntry<KeyboardShortcut> DumpNearbyKey;
        internal static ConfigEntry<KeyboardShortcut> DumpMonumentHomesKey;
        internal static ConfigEntry<KeyboardShortcut> DumpNearbyCombinatorKey;
        internal static ConfigEntry<KeyboardShortcut> DumpAllSaveEntriesKey;
        internal static ConfigEntry<KeyboardShortcut> DumpNearbyKeywordKey;
        internal static ConfigEntry<KeyboardShortcut> DumpNearbyPeckDevHelperKey;
        internal static ConfigEntry<KeyboardShortcut> TriggerUnlocksKey;
        internal static ConfigEntry<KeyboardShortcut> TriggerGourdKey;
        internal static ConfigEntry<KeyboardShortcut> DumpSpawnHubGateKey;
        internal static ConfigEntry<KeyboardShortcut> DumpPeckSwitchTargetKey;
        internal static ConfigEntry<KeyboardShortcut> RevealVariantGourdsKey;
        internal static ConfigEntry<KeyboardShortcut> FireRemotePeckSwitchKey;
        internal static ConfigEntry<string> RemotePeckSwitchName;
        internal static ConfigEntry<float> FlightSpeedMultiplier;
        internal static ConfigEntry<KeyboardShortcut> ForceNearbyCombinatorKey;
        internal static ConfigEntry<KeyboardShortcut> DumpGauntletKey;
        internal static ConfigEntry<KeyboardShortcut> DumpTimersKey;
        internal static ConfigEntry<KeyboardShortcut> LogStatesKey;
        internal static ConfigEntry<KeyboardShortcut> DumpPegTilesKey;
        internal static ConfigEntry<float> DumpGauntletRadius;
        internal static ConfigEntry<string> DumpGauntletCentre;
        internal static ConfigEntry<KeyboardShortcut> EnableSkipAidsKey;
        internal static ConfigEntry<KeyboardShortcut> SpawnTestButtonKey;
        internal static ConfigEntry<KeyboardShortcut> MarkSpotKey;
        internal static ConfigEntry<KeyboardShortcut> DumpIndicatorsKey;
        internal static ConfigEntry<KeyboardShortcut> ForceEndingFlagsKey;
        internal static ConfigEntry<KeyboardShortcut> ApplyBigKeyOverflowKey;
        internal static ConfigEntry<KeyboardShortcut> SpawnCosmeticPickupKey;
        internal static ConfigEntry<KeyboardShortcut> ToggleNeverCullKey;
        internal static ConfigEntry<KeyboardShortcut> ForceCosmeticPinKey;
        internal static ConfigEntry<KeyboardShortcut> DumpHostMenuConfirmKey;
        internal static ConfigEntry<KeyboardShortcut> DumpRadioKey;
        internal static ConfigEntry<KeyboardShortcut> DumpKeysKey;
        internal static ConfigEntry<KeyboardShortcut> ForceBigKeyDoorKey;
        internal static ConfigEntry<KeyboardShortcut> GrantBigKeyItemKey;
        internal static ConfigEntry<KeyboardShortcut> GrantBigKeyFeatureKey;
        internal static ConfigEntry<string> BigKeyDoorName;

        private static void BindDebugWorldKeys(ConfigFile debug)
        {
            HoldTrackedStateKey = debug.Bind(
                "Debug",
                "HoldTrackedStateKey",
                new KeyboardShortcut(KeyCode.Keypad4),
                "Holds down the nearest matching TrackedPeckState for as long as this key is held, a sustained hold, not a tap.");

            HoldTrackedStateName = debug.Bind(
                "Debug",
                "HoldTrackedStateName",
                "BasicPushButton",
                "Substring of the name of the TrackedPeckState that HoldTrackedStateKey holds.");

            HoldTrackedStateValue = debug.Bind(
                "Debug",
                "HoldTrackedStateValue",
                1,
                "The state to hold it at.");


            ToggleFlightKey = debug.Bind(
                "Debug",
                "ToggleFlightKey",
                new KeyboardShortcut(KeyCode.F2),
                "Key to toggle the free camera on/off (only has an effect if Debug.Enabled is active).");

            UnlockNextKey = debug.Bind(
                "Debug",
                "UnlockNextKey",
                new KeyboardShortcut(KeyCode.F3),
                "Force-unlocks the next locked gourd/big key in the current zone, without solving the puzzle.");

            SimulateReceivedItemKey = debug.Bind(
                "Debug",
                "SimulateReceivedItemKey",
                new KeyboardShortcut(KeyCode.F4),
                "Simulates the remote receipt of an Archipelago item for the next locked gourd/big key in the current zone.");

            DumpNearbyKey = debug.Bind(
                "Debug",
                "DumpNearbyKey",
                new KeyboardShortcut(KeyCode.F5),
                "Logs the RewardGourd instances nearest to the player, regardless of their state.");

            DumpMonumentHomesKey = debug.Bind(
                "Debug",
                "DumpMonumentHomesKey",
                new KeyboardShortcut(KeyCode.F6),
                "Logs all the 'monoument*' PropHome instances actually registered in the current zone, with their fill state.");

            DumpNearbyCombinatorKey = debug.Bind(
                "Debug",
                "DumpNearbyCombinatorKey",
                new KeyboardShortcut(KeyCode.F7),
                "Logs nearby PeckCombinator instances, diagnostic for combined-condition mechanisms.");

            DumpAllSaveEntriesKey = debug.Bind(
                "Debug",
                "DumpAllSaveEntriesKey",
                new KeyboardShortcut(KeyCode.F8),
                "Logs all int entries of SaveManager.currentData.entries.");

            DumpNearbyKeywordKey = debug.Bind(
                "Debug",
                "DumpNearbyKeywordKey",
                new KeyboardShortcut(KeyCode.F9),
                "Logs nearby GameObjects along with their components, generic diagnostic to identify a mechanism with no known dedicated C# class.");

            DumpNearbyPeckDevHelperKey = debug.Bind(
                "Debug",
                "DumpNearbyPeckDevHelperKey",
                new KeyboardShortcut(KeyCode.F10),
                "Logs nearby PeckDevHelper instances along with their assigned UnlockRules.");

            TriggerUnlocksKey = debug.Bind(
                "Debug",
                "TriggerUnlocksKey",
                new KeyboardShortcut(KeyCode.F11),
                "Calls PeckDevHelper.Trigger with UnlockRules.unlocks=true, test for the Arch doors/HubGate.");

            TriggerGourdKey = debug.Bind(
                "Debug",
                "TriggerGourdKey",
                new KeyboardShortcut(KeyCode.F12),
                "Calls PeckDevHelper.Trigger with UnlockRules.gourd=true, test for the purple gourds/variant challenges.");

            DumpSpawnHubGateKey = debug.Bind(
                "Debug",
                "DumpSpawnHubGateKey",
                new KeyboardShortcut(KeyCode.Insert),
                "Logs all TrackedPeckState instances of category SpawnHubGate with their SaveIdentity.saveGuid and current SaveManager value.");

            DumpPeckSwitchTargetKey = debug.Bind(
                "Debug",
                "DumpPeckSwitchTargetKey",
                new KeyboardShortcut(KeyCode.Delete),
                "Logs nearby PeckSwitch instances with their trackedStateSystem and the corresponding SaveManager key.");

            RevealVariantGourdsKey = debug.Bind(
                "Debug",
                "RevealVariantGourdsKey",
                new KeyboardShortcut(KeyCode.Home),
                "Reveals on the map all 'variant challenge' gourds in the loaded zone, via GourdMap.refreshFlag.");

            RemotePeckSwitchName = debug.Bind(
                "Debug",
                "RemotePeckSwitchName",
                "",
                "Substring of the name of the GameObject whose PeckSwitch should be triggered remotely by FireRemotePeckSwitchKey.");

            FlightSpeedMultiplier = debug.Bind(
                "Debug",
                "FlightSpeedMultiplier",
                4f,
                "Multiplier applied to CameraCheatMover.movingSpeed when the free camera is enabled, 1 = the game's vanilla camera-cheat speed.");

            FireRemotePeckSwitchKey = debug.Bind(
                "Debug",
                "FireRemotePeckSwitchKey",
                new KeyboardShortcut(KeyCode.PageUp),
                "Presses remotely the PeckSwitch named by RemotePeckSwitchName.");

            DumpGauntletKey = debug.Bind(
                "Debug",
                "DumpGauntletKey",
                new KeyboardShortcut(KeyCode.Keypad5),
                "Writes the wiring of everything within DumpGauntletRadius of the player to BepInEx/gauntlet-dump-<n>.tsv.");

            DumpPegTilesKey = debug.Bind(
                "Debug",
                "DumpPegTilesKey",
                new KeyboardShortcut(KeyCode.Keypad1),
                "Writes every peg tile validator and every peg tile of the world to BepInEx/peg-tile-dump-<n>.txt.");

            LogStatesKey = debug.Bind(
                "Debug",
                "LogStatesKey",
                new KeyboardShortcut(KeyCode.Keypad2),
                "Switches on and off a log of every value the game saves and every state set within 25 m of the player.");

            DumpTimersKey = debug.Bind(
                "Debug",
                "DumpTimersKey",
                new KeyboardShortcut(KeyCode.Keypad3),
                "Writes every timer of the game to BepInEx/timer-dump.txt, to find the waits of the Cabin Fever puzzles.");

            DumpGauntletRadius = debug.Bind(
                "Debug",
                "DumpGauntletRadius",
                80f,
                "Radius in metres of DumpGauntletKey's dump.");

            DumpGauntletCentre = debug.Bind(
                "Debug",
                "DumpGauntletCentre",
                "",
                "Where DumpGauntletKey's dump is centred: x, y, z in metres. Empty: on the player.");

            EnableSkipAidsKey = debug.Bind(
                "Debug",
                "EnableSkipAidsKey",
                new KeyboardShortcut(KeyCode.Keypad6),
                "Switches on the game's own skip aids so that what they do can be seen.");

            SpawnTestButtonKey = debug.Bind(
                "Debug",
                "SpawnTestButtonKey",
                new KeyboardShortcut(KeyCode.Keypad7),
                "Copies the nearest push button of the game two metres in front of you and logs what happens when it is pressed.");

            MarkSpotKey = debug.Bind(
                "Debug",
                "MarkSpotKey",
                new KeyboardShortcut(KeyCode.Keypad8),
                "Writes the surface the crosshair is on to BepInEx/world-button-spots.txt, to place the in-world buttons.");

            DumpIndicatorsKey = debug.Bind(
                "Debug",
                "DumpIndicatorsKey",
                new KeyboardShortcut(KeyCode.Keypad9),
                "Writes every renderer of the Black Tower's panel of tower icons to BepInEx/indicator-dump.txt.");

            ForceNearbyCombinatorKey = debug.Bind(
                "Debug",
                "ForceNearbyCombinatorKey",
                new KeyboardShortcut(KeyCode.End),
                "Directly forces the state of all systems[]/block.systems[] of PeckCombinator instances within 30m to their desiredState.");

            ForceEndingFlagsKey = debug.Bind(
                "Debug",
                "ForceEndingFlagsKey",
                new KeyboardShortcut(KeyCode.PageDown),
                "Remotely forces the 7 big keys + SaveManager[EndingGate]=1 + SaveManager[GauntletComplete]=1 all at once.");

            ApplyBigKeyOverflowKey = debug.Bind(
                "Debug",
                "ApplyBigKeyOverflowKey",
                new KeyboardShortcut(KeyCode.O),
                "Applies only bigKeyOverflow.");

            SpawnCosmeticPickupKey = debug.Bind(
                "Debug",
                "SpawnCosmeticPickupKey",
                new KeyboardShortcut(KeyCode.P),
                "Triggers ReceivedItemSpawner.SpawnCosmeticPickup directly.");

            ToggleNeverCullKey = debug.Bind(
                "Debug",
                "ToggleNeverCullKey",
                new KeyboardShortcut(KeyCode.C),
                "Toggles CullingAgent.Instance.neverCull.");

            ForceCosmeticPinKey = debug.Bind(
                "Debug",
                "ForceCosmeticPinKey",
                new KeyboardShortcut(KeyCode.N),
                "Directly pins the nearest cosmetic gourd into the nearest empty PropHome.");

            DumpHostMenuConfirmKey = debug.Bind(
                "Debug",
                "DumpHostMenuConfirmKey",
                new KeyboardShortcut(KeyCode.M),
                "Dumps the hosting screen's hierarchy; press it on that screen.");

            DumpRadioKey = debug.Bind(
                "Debug",
                "DumpRadioKey",
                new KeyboardShortcut(KeyCode.B, KeyCode.LeftControl),
                "Logs the radio dial, every BroadcastStation currently loaded with the SavableSystem its peck system writes.");

            DumpKeysKey = debug.Bind(
                "Debug",
                "DumpKeysKey",
                new KeyboardShortcut(KeyCode.K, KeyCode.LeftControl),
                "Logs everything the \"big keys as forage checks\" design needs.");

            ForceBigKeyDoorKey = debug.Bind(
                "Debug",
                "ForceBigKeyDoorKey",
                new KeyboardShortcut(KeyCode.U, KeyCode.LeftControl),
                "Opens a big-key door without the key.");

            GrantBigKeyItemKey = debug.Bind(
                "Debug",
                "GrantBigKeyItemKey",
                new KeyboardShortcut(KeyCode.G, KeyCode.LeftControl),
                "Simulates receiving the big KEY item for one tower.");

            GrantBigKeyFeatureKey = debug.Bind(
                "Debug",
                "GrantBigKeyFeatureKey",
                new KeyboardShortcut(KeyCode.F, KeyCode.LeftControl),
                "Simulates receiving the FEATURE item for one tower, the map room, the chairlift, the train.");

            BigKeyDoorName = debug.Bind(
                "Debug",
                "BigKeyDoorName",
                "",
                "Substring of the SaveablePropName of the big key whose door ForceBigKeyDoorKey should open, bigKeyGreenZone, bigKeyIntro.");
        }
    }
}
