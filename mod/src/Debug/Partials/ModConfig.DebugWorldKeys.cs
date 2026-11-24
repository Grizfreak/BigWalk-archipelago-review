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
                "Holds down the nearest matching TrackedPeckState for as long as this key is held, a sustained hold, not a tap. For the sealed-box puzzles, whose door is opened by a momentary push button a second player would normally keep pressed (only has an effect if Debug.Enabled is active).");

            HoldTrackedStateName = debug.Bind(
                "Debug",
                "HoldTrackedStateName",
                "BasicPushButton",
                "Substring (case-insensitive) of the name of the TrackedPeckState that HoldTrackedStateKey holds. Only the NEAREST match is touched: this default is a generic name shared by buttons all over the world, and forcing every match would press buttons in puzzles you are nowhere near. Find the right name with DumpPeckSwitchTargetKey.");

            HoldTrackedStateValue = debug.Bind(
                "Debug",
                "HoldTrackedStateValue",
                1,
                "The state to hold it at. 1 is 'pressed' for a push button (its PeckSwitchTrigger writes 1, its UpSwitch writes 0).");


            ToggleFlightKey = debug.Bind(
                "Debug",
                "ToggleFlightKey",
                new KeyboardShortcut(KeyCode.F2),
                "Key to toggle the free camera on/off (only has an effect if Debug.Enabled is active).");

            UnlockNextKey = debug.Bind(
                "Debug",
                "UnlockNextKey",
                new KeyboardShortcut(KeyCode.F3),
                "Force-unlocks the next locked gourd/big key in the current zone, without solving the puzzle (only has an effect if Debug.Enabled is active).");

            SimulateReceivedItemKey = debug.Bind(
                "Debug",
                "SimulateReceivedItemKey",
                new KeyboardShortcut(KeyCode.F4),
                "Simulates the remote receipt of an Archipelago item for the next locked gourd/big key in the current zone (only has an effect if Debug.Enabled is active).");

            DumpNearbyKey = debug.Bind(
                "Debug",
                "DumpNearbyKey",
                new KeyboardShortcut(KeyCode.F5),
                "Logs the RewardGourd instances nearest to the player (name + state + distance), regardless of their state, diagnostic to see what's actually present around you (only has an effect if Debug.Enabled is active).");

            DumpMonumentHomesKey = debug.Bind(
                "Debug",
                "DumpMonumentHomesKey",
                new KeyboardShortcut(KeyCode.F6),
                "Logs all the 'monoument*' PropHome instances actually registered in the current zone, with their fill state (only has an effect if Debug.Enabled is active).");

            DumpNearbyCombinatorKey = debug.Bind(
                "Debug",
                "DumpNearbyCombinatorKey",
                new KeyboardShortcut(KeyCode.F7),
                "Logs nearby PeckCombinator instances (directControlSystem, onConditionMet/onConditionStop, rules with systems[]/minimumMatches), diagnostic for combined-condition mechanisms (e.g. two buttons pressed simultaneously) (only has an effect if Debug.Enabled is active).");

            DumpAllSaveEntriesKey = debug.Bind(
                "Debug",
                "DumpAllSaveEntriesKey",
                new KeyboardShortcut(KeyCode.F8),
                "Logs all int entries of SaveManager.currentData.entries, to diff before/after an in-game action and find the real key without guessing via the Peck graph (only has an effect if Debug.Enabled is active).");

            DumpNearbyKeywordKey = debug.Bind(
                "Debug",
                "DumpNearbyKeywordKey",
                new KeyboardShortcut(KeyCode.F9),
                "Logs nearby GameObjects (name or component containing 'arch'/'door'/'switch'/'button'/'gate'/'peck'/'shortcut') along with their components, generic diagnostic to identify a mechanism with no known dedicated C# class (only has an effect if Debug.Enabled is active).");

            DumpNearbyPeckDevHelperKey = debug.Bind(
                "Debug",
                "DumpNearbyPeckDevHelperKey",
                new KeyboardShortcut(KeyCode.F10),
                "Logs nearby PeckDevHelper instances along with their assigned UnlockRules (unlocks/lights/chairlift/train/bell/tunnel/map/gourd), diagnostic for figuring out which rule triggers which cheat switch (only has an effect if Debug.Enabled is active).");

            TriggerUnlocksKey = debug.Bind(
                "Debug",
                "TriggerUnlocksKey",
                new KeyboardShortcut(KeyCode.F11),
                "Calls PeckDevHelper.Trigger with UnlockRules.unlocks=true (cheat already built into the game), test for the Arch doors/HubGate (only has an effect if Debug.Enabled is active).");

            TriggerGourdKey = debug.Bind(
                "Debug",
                "TriggerGourdKey",
                new KeyboardShortcut(KeyCode.F12),
                "Calls PeckDevHelper.Trigger with UnlockRules.gourd=true (cheat already built into the game), test for the purple gourds/variant challenges, normally unlocked after a first ending (only has an effect if Debug.Enabled is active).");

            DumpSpawnHubGateKey = debug.Bind(
                "Debug",
                "DumpSpawnHubGateKey",
                new KeyboardShortcut(KeyCode.Insert),
                "Logs all TrackedPeckState instances of category SpawnHubGate (the 3 Arch doors) with their SaveIdentity.saveGuid and current SaveManager value, diagnostic to target these 3 doors directly without going through PeckDevHelper.Trigger (only has an effect if Debug.Enabled is active).");

            DumpPeckSwitchTargetKey = debug.Bind(
                "Debug",
                "DumpPeckSwitchTargetKey",
                new KeyboardShortcut(KeyCode.Delete),
                "Logs all nearby PeckSwitch instances with their actual trackedStateSystem (the real TrackedPeckState modified on peck, not a guess) and the corresponding SaveManager key (only has an effect if Debug.Enabled is active).");

            RevealVariantGourdsKey = debug.Bind(
                "Debug",
                "RevealVariantGourdsKey",
                new KeyboardShortcut(KeyCode.Home),
                "Reveals on the map all 'variant challenge' gourds (purple/postgame) in the loaded zone, via GourdMap.refreshFlag (only has an effect if Debug.Enabled is active).");

            RemotePeckSwitchName = debug.Bind(
                "Debug",
                "RemotePeckSwitchName",
                "",
                "Substring (case-insensitive) of the name of the GameObject whose PeckSwitch should be triggered remotely by FireRemotePeckSwitchKey, find the name via F9/Delete beforehand. Empty = nothing gets triggered.");

            FlightSpeedMultiplier = debug.Bind(
                "Debug",
                "FlightSpeedMultiplier",
                4f,
                "Multiplier applied to CameraCheatMover.movingSpeed when the free camera is enabled (ToggleFlightKey), 1 = the game's vanilla camera-cheat speed.");

            FireRemotePeckSwitchKey = debug.Bind(
                "Debug",
                "FireRemotePeckSwitchKey",
                new KeyboardShortcut(KeyCode.PageUp),
                "Remotely triggers (PeckSwitch.Peck(), without having to stand in front of it) all PeckSwitch instances in the zone whose name contains RemotePeckSwitchName, to solo-test a mechanism designed for 2 players (only has an effect if Debug.Enabled is active).");

            DumpGauntletKey = debug.Bind(
                "Debug",
                "DumpGauntletKey",
                new KeyboardShortcut(KeyCode.Keypad5),
                "Writes the wiring of everything within DumpGauntletRadius of the player (states and their current value, what listens to each, switches, collective-press switches, combinators) to BepInEx/gauntlet-dump-<n>.tsv. Press it before and after a step in the Silent Gauntlet and compare the two files (only has an effect if Debug.Enabled is active).");

            DumpPegTilesKey = debug.Bind(
                "Debug",
                "DumpPegTilesKey",
                new KeyboardShortcut(KeyCode.Keypad1),
                "Writes every peg tile validator (Tile Thief's: the tiles it expects, its sets, its slots) and every peg tile of the world to BepInEx/peg-tile-dump-<n>.txt (only has an effect if Debug.Enabled is active).");

            LogStatesKey = debug.Bind(
                "Debug",
                "LogStatesKey",
                new KeyboardShortcut(KeyCode.Keypad2),
                "Switches on and off a log of every value the game saves and every state set within 25 m of the player (with its place in the scene), to find what a button or door writes (only has an effect if Debug.Enabled is active).");

            DumpTimersKey = debug.Bind(
                "Debug",
                "DumpTimersKey",
                new KeyboardShortcut(KeyCode.Keypad3),
                "Writes every timer of the game (PeckEffectTimer and its networked sibling: where it is, how long it runs, what starts it) to BepInEx/timer-dump.txt, to find the waits of the Cabin Fever puzzles (only has an effect if Debug.Enabled is active).");

            DumpGauntletRadius = debug.Bind(
                "Debug",
                "DumpGauntletRadius",
                80f,
                "Radius in metres of DumpGauntletKey's dump.");

            DumpGauntletCentre = debug.Bind(
                "Debug",
                "DumpGauntletCentre",
                "",
                "Where DumpGauntletKey's dump is centred: x, y, z in metres. Empty: on the player. The game builds every place at once, so a place can be dumped from anywhere (2026-10-16, the Green Dome's door).");

            EnableSkipAidsKey = debug.Bind(
                "Debug",
                "EnableSkipAidsKey",
                new KeyboardShortcut(KeyCode.Keypad6),
                "Switches on the game's own skip aids (the two-button poles in each Silent Gauntlet chamber, off in a normal game) so that what they do can be seen. Hold their buttons together (End does) and compare two DumpGauntletKey dumps (only has an effect if Debug.Enabled is active).");

            SpawnTestButtonKey = debug.Bind(
                "Debug",
                "SpawnTestButtonKey",
                new KeyboardShortcut(KeyCode.Keypad7),
                "Copies the nearest push button of the game two metres in front of you and logs what happens when it is pressed (an experiment for the in-world buttons of ROADMAP U4/U10; only has an effect if Debug.Enabled is active).");

            MarkSpotKey = debug.Bind(
                "Debug",
                "MarkSpotKey",
                new KeyboardShortcut(KeyCode.Keypad8),
                "Writes the surface the crosshair is on (a point and the way it faces) to BepInEx/world-button-spots.txt, to place the in-world buttons of ROADMAP U10 (only has an effect if Debug.Enabled is active).");

            DumpIndicatorsKey = debug.Bind(
                "Debug",
                "DumpIndicatorsKey",
                new KeyboardShortcut(KeyCode.Keypad9),
                "Writes every renderer of the Black Tower's panel of tower icons (EndingGateIndicator) to BepInEx/indicator-dump.txt, to copy the icons onto the teleport buttons of ROADMAP U10 (only has an effect if Debug.Enabled is active).");

            ForceNearbyCombinatorKey = debug.Bind(
                "Debug",
                "ForceNearbyCombinatorKey",
                new KeyboardShortcut(KeyCode.End),
                "Directly forces (TrackedPeckState.SetState, no button simulation) the state of all systems[]/block.systems[] of PeckCombinator instances within 30m to their desiredState, to solo-satisfy an 'N simultaneous players' condition (e.g. N-hold bells) without depending on the press/hold system (only has an effect if Debug.Enabled is active).");

            ForceEndingFlagsKey = debug.Bind(
                "Debug",
                "ForceEndingFlagsKey",
                new KeyboardShortcut(KeyCode.PageDown),
                "Remotely forces the 7 big keys (via ItemApplier) + SaveManager[EndingGate]=1 + SaveManager[GauntletComplete]=1 all at once, to test whether the hub's black sphere reacts to this flag combination without having to redo everything for real. Use on a save that has never seen the ending screen (only has an effect if Debug.Enabled is active).");

            ApplyBigKeyOverflowKey = debug.Bind(
                "Debug",
                "ApplyBigKeyOverflowKey",
                new KeyboardShortcut(KeyCode.O),
                "Applies only bigKeyOverflow (via ItemApplier.ApplyBigKeyItem, so a real live pin on bigKeyPlinthGoodbye2), isolated test (a single variable, unlike ForceEndingFlagsKey) of the 2026-09-11 hypothesis: the bigKeyPlinthGoodbye2 plinth sits just behind the hub's black sphere, possibly the real trigger for OpenSystem/PropHomeBlock rather than some notion of 'game already finished' (only has an effect if Debug.Enabled is active).");

            SpawnCosmeticPickupKey = debug.Bind(
                "Debug",
                "SpawnCosmeticPickupKey",
                new KeyboardShortcut(KeyCode.P),
                "Triggers ReceivedItemSpawner.SpawnCosmeticPickup() directly (without going through a real item), to test in isolation the cloning/network spawn of the cosmetic gourd at the hub (only has an effect if Debug.Enabled is active).");

            ToggleNeverCullKey = debug.Bind(
                "Debug",
                "ToggleNeverCullKey",
                new KeyboardShortcut(KeyCode.C),
                "Toggles CullingAgent.Instance.neverCull (dev cheat already built into the game), isolated test of the 2026-09-11 hypothesis: the cosmetic gourd spawned by P stays invisible despite every Unity indicator being green (enabled/isVisible/observers), possibly due to the game's own region-based culling system, which might ignore an object never registered in any CullingRegion (only has an effect if Debug.Enabled is active).");

            ForceCosmeticPinKey = debug.Bind(
                "Debug",
                "ForceCosmeticPinKey",
                new KeyboardShortcut(KeyCode.N),
                "Directly pins (Prop.ServerSetPinned, without going through manual interaction/holding) the nearest cosmetic gourd into the nearest empty PropHome (30m), to test CosmeticMonumentFillTracker without having to redo the deposit interaction for real (only has an effect if Debug.Enabled is active).");

            DumpHostMenuConfirmKey = debug.Bind(
                "Debug",
                "DumpHostMenuConfirmKey",
                new KeyboardShortcut(KeyCode.M),
                "Dumps the full hierarchy of the hosting screen (HostMenuConfirm), press while on this screen, before coding an addition of the AP host:port field on it (only has an effect if Debug.Enabled is active).");

            DumpRadioKey = debug.Bind(
                "Debug",
                "DumpRadioKey",
                new KeyboardShortcut(KeyCode.B, KeyCode.LeftControl),
                "Logs the radio dial (FmRadioManager.stationTrackGroups, one line per slot with its unlock state), every BroadcastStation currently loaded with the SavableSystem its peck system writes, and this save's station ledger. Answers the one question the decompilation could not: whether the dial is ordered like the FmStation* enum, which is what Core/RadioStations.cs falls back on when no tower is loaded. Letter + LeftControl because that is the only key format proven to register reliably in this game (only has an effect if Debug.Enabled is active).");

            DumpKeysKey = debug.Bind(
                "Debug",
                "DumpKeysKey",
                new KeyboardShortcut(KeyCode.K, KeyCode.LeftControl),
                "Logs everything the \"big keys as forage checks\" design needs: every KeyBlank loaded (which key, how many segments and which are cut, its finishedPropGroup), every big-key plinth with the PropGroup it accepts and the TrackedPeckState it drives when filled, and what each key prop currently carries. Answers how many locations the apworld would gain, whether skipping RefreshPropGroup is enough to keep an uncut key out of its socket, and whether a feature switch exists separately from the key (only has an effect if Debug.Enabled is active).");

            ForceBigKeyDoorKey = debug.Bind(
                "Debug",
                "ForceBigKeyDoorKey",
                new KeyboardShortcut(KeyCode.U, KeyCode.LeftControl),
                "Opens a big-key door without the key, by driving the TrackedPeckState the key itself carries in taggedPinSystems for its plinth's PropGroup, which is what Prop.SetPinDirectControlSystem does when a key goes in, and what a received feature item will do instead. Takes the key whose plinth is nearest unless BigKeyDoorName says otherwise, and prints all seven with their distances either way. Moved off Ctrl+D (player report, 2026-09-22: fired on its own during ordinary play), D is a WASD movement key, so Ctrl+D collides with crouch-while-moving-right in this game's own controls. Letter + LeftControl otherwise, since that is the only key format proven to register reliably in this game, but never a WASD letter specifically, for the same reason (only has an effect if Debug.Enabled is active).");

            GrantBigKeyItemKey = debug.Bind(
                "Debug",
                "GrantBigKeyItemKey",
                new KeyboardShortcut(KeyCode.G, KeyCode.LeftControl),
                "Simulates receiving the big KEY item for one tower (Core/KeyCustody.Grant), which unlocks that key from its stone and drops it at the spawn point. Not the same thing as the feature item that opens its door, that one is Ctrl+D, or F4. Uses BigKeyDoorName to choose, or the nearest plinth when that is empty. Needed because the keys are shuffled into the multiworld like everything else, so they no longer arrive on their own (only has an effect if Debug.Enabled is active).");

            GrantBigKeyFeatureKey = debug.Bind(
                "Debug",
                "GrantBigKeyFeatureKey",
                new KeyboardShortcut(KeyCode.F, KeyCode.LeftControl),
                "Simulates receiving the FEATURE item for one tower (Core/KeyFeatures.Grant), the map room, the chairlift, the train. Unlike ForceBigKeyDoorKey, which drives the door's state directly and bypasses the ledger, this takes the path a real Archipelago item takes and therefore also tests that the door reopens after a world reload. Uses BigKeyDoorName to choose, or the nearest plinth when that is empty. F4 is no substitute: it targets the nearest uncollected prop, which is rarely the one you are standing in front of (only has an effect if Debug.Enabled is active).");

            BigKeyDoorName = debug.Bind(
                "Debug",
                "BigKeyDoorName",
                "",
                "Substring (case-insensitive) of the SaveablePropName of the big key whose door ForceBigKeyDoorKey should open, bigKeyGreenZone, bigKeyIntro, and so on. Empty = whichever plinth is nearest. An escape hatch for the case where the plinths turn out to be instantiated everywhere, like the key blanks are, and distance stops meaning anything.");
        }
    }
}
