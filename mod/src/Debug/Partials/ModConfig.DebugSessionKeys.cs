using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP.Configuration;
using UnityEngine;

namespace BigWalkArchipelago
{
    internal static partial class ModConfig
    {
        internal static ConfigEntry<KeyboardShortcut> LatchGoalFlagsKey;
        internal static ConfigEntry<KeyboardShortcut> LaserBeamKey;
        internal static ConfigEntry<KeyboardShortcut> DumpPropsKey;
        internal static ConfigEntry<KeyboardShortcut> DumpGourdRosterKey;
        internal static ConfigEntry<KeyboardShortcut> PositionPingKey;
        internal static ConfigEntry<KeyboardShortcut> LookAtKey;
        internal static ConfigEntry<KeyboardShortcut> DaylightKey;
        internal static ConfigEntry<KeyboardShortcut> LockButtonsKey;
        internal static ConfigEntry<KeyboardShortcut> LockNextNeedKey;
        internal static ConfigEntry<KeyboardShortcut> NeedStatusKey;
        internal static ConfigEntry<KeyboardShortcut> SendDeathLinkKey;
        internal static ConfigEntry<KeyboardShortcut> FireTrapKey;
        internal static ConfigEntry<KeyboardShortcut> TrapStateKey;
        internal static ConfigEntry<KeyboardShortcut> MarkTripSpotKey;
        internal static ConfigEntry<KeyboardShortcut> PickupCensusKey;
        internal static ConfigEntry<KeyboardShortcut> PickupTeleportKey;
        internal static ConfigEntry<KeyboardShortcut> SimulateGadgetItemKey;
        internal static ConfigEntry<KeyboardShortcut> DumpHeldItemKey;
        internal static ConfigEntry<KeyboardShortcut> DumpCosmeticGadgetsKey;
        internal static ConfigEntry<KeyboardShortcut> ColourProbeKey;
        internal static ConfigEntry<KeyboardShortcut> AudioProbeKey;
        internal static ConfigEntry<KeyboardShortcut> ApFeedKey;
        internal static ConfigEntry<KeyboardShortcut> MarkObjectiveSpotKey;
        internal static ConfigEntry<KeyboardShortcut> ResetObjectiveSpotsKey;
        internal static ConfigEntry<KeyboardShortcut> FakeObjectivesKey;
        internal static ConfigEntry<KeyboardShortcut> OpenLastStairwayKey;
        internal static ConfigEntry<KeyboardShortcut> MonumentModeKey;
        internal static ConfigEntry<KeyboardShortcut> GrantMonumentKey;
        internal static ConfigEntry<KeyboardShortcut> FillMonumentKey;
        internal static ConfigEntry<KeyboardShortcut> SoundCandidatesKey;
        internal static ConfigEntry<KeyboardShortcut> AudioNextKey;
        internal static ConfigEntry<KeyboardShortcut> AudioPrevKey;
        internal static ConfigEntry<string> AudioFilter;
        internal static ConfigEntry<KeyboardShortcut> BuoyTintKey;
        internal static ConfigEntry<KeyboardShortcut> FlareTintKey;
        internal static ConfigEntry<KeyboardShortcut> MaskTrapKey;
        internal static ConfigEntry<KeyboardShortcut> DazeKey;
        internal static ConfigEntry<KeyboardShortcut> DumpNetworkKey;
        internal static ConfigEntry<KeyboardShortcut> LoopbackJoinKey;
        internal static ConfigEntry<bool> EmulateBigLanding;
        internal static ConfigEntry<KeyboardShortcut> LoopbackFocusKey;
        internal static ConfigEntry<KeyboardShortcut> LoopbackSummonKey;
        internal static ConfigEntry<KeyboardShortcut> LoopbackVanillaGuestKey;
        internal static ConfigEntry<string> AutoHostSave;
        internal static ConfigEntry<bool> AutoHostGuest;
        internal static ConfigEntry<bool> PerfProbe;

        private static void BindDebugSessionKeys(ConfigFile debug)
        {
            DumpPropsKey = debug.Bind(
                "Debug",
                "DumpPropsKey",
                new KeyboardShortcut(KeyCode.J, KeyCode.LeftControl),
                "Lists the objects loaded around you that could plausibly become Archipelago items, the ones carrying a savablePropGuid.");

            DumpGourdRosterKey = debug.Bind(
                "Debug",
                "DumpGourdRosterKey",
                new KeyboardShortcut(KeyCode.V, KeyCode.LeftControl),
                "Lists every RewardGourd loaded around you with its saveablePropName and whether it is a purple 'variant challenge' gourd.");

            PositionPingKey = debug.Bind(
                "Debug",
                "PositionPingKey",
                new KeyboardShortcut(KeyCode.X, KeyCode.LeftControl),
                "Logs the player's current world position and, of every puzzle and big-key plinth known to the Archipelago world.");

            LookAtKey = debug.Bind(
                "Debug",
                "LookAtKey",
                new KeyboardShortcut(KeyCode.E, KeyCode.LeftControl),
                "Logs the object under the crosshair and everything with a collider within four metres.");

            DaylightKey = debug.Bind(
                "Debug",
                "DaylightKey",
                new KeyboardShortcut(KeyCode.KeypadMultiply, KeyCode.LeftControl),
                "Fixes the time of day at noon.");

            LockButtonsKey = debug.Bind(
                "Debug",
                "LockButtonsKey",
                new KeyboardShortcut(KeyCode.KeypadMinus, KeyCode.LeftControl),
                "Locks every puzzle need, or unlocks them all.");

            LockNextNeedKey = debug.Bind(
                "Debug",
                "LockNextNeedKey",
                new KeyboardShortcut(KeyCode.KeypadPlus, KeyCode.LeftControl),
                "Locks or unlocks the next puzzle need in the list, one per press, to try them one by one.");

            NeedStatusKey = debug.Bind(
                "Debug",
                "NeedStatusKey",
                new KeyboardShortcut(KeyCode.KeypadDivide, KeyCode.LeftControl),
                "Logs, with the time, what this machine believes about the puzzle needs.");

            SendDeathLinkKey = debug.Bind(
                "Debug",
                "SendDeathLinkKey",
                new KeyboardShortcut(KeyCode.P, KeyCode.LeftControl),
                "Sends a DeathLink to the multiworld, for a friend whose game listens for them.");

            FireTrapKey = debug.Bind(
                "Debug",
                "FireTrapKey",
                new KeyboardShortcut(KeyCode.Keypad0, KeyCode.LeftControl),
                "On the host.");

            TrapStateKey = debug.Bind(
                "Debug",
                "TrapStateKey",
                new KeyboardShortcut(KeyCode.KeypadEnter, KeyCode.LeftControl),
                "Logs what the traps see.");

            MarkTripSpotKey = debug.Bind(
                "Debug",
                "MarkTripSpotKey",
                new KeyboardShortcut(KeyCode.KeypadPeriod, KeyCode.LeftControl),
                "Writes where the player stands to BepInEx/trip-spots.txt.");

            PickupCensusKey = debug.Bind(
                "Debug",
                "PickupCensusKey",
                new KeyboardShortcut(KeyCode.F1, KeyCode.LeftControl),
                "Writes BepInEx/pickup-census.tsv.");

            PickupTeleportKey = debug.Bind(
                "Debug",
                "PickupTeleportKey",
                new KeyboardShortcut(KeyCode.F3, KeyCode.LeftControl),
                "Teleports the player beside the next backpack, belt, gourd carton or firework launcher of the island, in turn.");

            ColourProbeKey = debug.Bind(
                "Debug",
                "ColourProbeKey",
                new KeyboardShortcut(KeyCode.F5, KeyCode.LeftControl),
                "Logs what carries the colour of the buoys, the flare guns and their flares, one example of each kind.");

            AudioProbeKey = debug.Bind(
                "Debug",
                "AudioProbeKey",
                new KeyboardShortcut(KeyCode.Backslash, KeyCode.LeftControl),
                "Writes BepInEx/audio-probe.tsv.");

            MarkObjectiveSpotKey = debug.Bind(
                "Debug",
                "MarkObjectiveSpotKey",
                new KeyboardShortcut(KeyCode.Alpha1, KeyCode.LeftAlt),
                "Writes where the player stands to BepInEx/objective-spots.txt.");

            ResetObjectiveSpotsKey = debug.Bind(
                "Debug",
                "ResetObjectiveSpotsKey",
                new KeyboardShortcut(KeyCode.Alpha2, KeyCode.LeftAlt),
                "Forgets that big_climb and big_party were done, to try them again.");

            ApFeedKey = debug.Bind(
                "Debug",
                "ApFeedKey",
                new KeyboardShortcut(KeyCode.Alpha5, KeyCode.LeftControl),
                "Shows, one per press, the overlay's lines and plays the sounds of the Archipelago feed without a server.");

            FakeObjectivesKey = debug.Bind(
                "Debug",
                "FakeObjectivesKey",
                new KeyboardShortcut(KeyCode.Alpha7, KeyCode.LeftControl),
                "Cycles fake objectives to see the board in the hub, the overlay line and the shut endings without a server.");

            OpenLastStairwayKey = debug.Bind(
                "Debug",
                "OpenLastStairwayKey",
                new KeyboardShortcut(KeyCode.Alpha8, KeyCode.LeftControl),
                "Asks the Gauntlet's last stairway to open, to test the objectives' hold on it.");

            MonumentModeKey = debug.Bind(
                "Debug",
                "MonumentModeKey",
                new KeyboardShortcut(KeyCode.Alpha9, KeyCode.LeftControl),
                "Cycles the monuments held to an order without a server.");

            GrantMonumentKey = debug.Bind(
                "Debug",
                "GrantMonumentKey",
                new KeyboardShortcut(KeyCode.Alpha0, KeyCode.LeftControl),
                "Receives one Progressive Monument.");

            FillMonumentKey = debug.Bind(
                "Debug",
                "FillMonumentKey",
                new KeyboardShortcut(KeyCode.Alpha3, KeyCode.LeftAlt),
                "Fills, in the save, the first monument of the debug order that is not full.");

            LatchGoalFlagsKey = debug.Bind(
                "Debug",
                "LatchGoalFlagsKey",
                new KeyboardShortcut(KeyCode.Alpha4, KeyCode.LeftAlt),
                "Latches in the save the bell and the Gauntlet as if both had been played, to see an objectives goal go out without playing them.");

            LaserBeamKey = debug.Bind(
                "Debug",
                "LaserBeamKey",
                new KeyboardShortcut(KeyCode.Alpha5, KeyCode.LeftAlt),
                "Puts the next of a few emission settings on every laser beam, to see whether its red can be pushed aside.");

            SoundCandidatesKey = debug.Bind(
                "Debug",
                "SoundCandidatesKey",
                new KeyboardShortcut(KeyCode.Alpha6, KeyCode.LeftControl),
                "Plays the next candidate for the 'item received' sound and shows its name on screen, to choose it by ear.");

            AudioNextKey = debug.Bind(
                "Debug",
                "AudioNextKey",
                new KeyboardShortcut(KeyCode.RightBracket, KeyCode.LeftControl),
                "Plays the next sound the audio probe or the played-sound log has seen where the player stands, and logs its name.");

            AudioPrevKey = debug.Bind(
                "Debug",
                "AudioPrevKey",
                new KeyboardShortcut(KeyCode.LeftBracket, KeyCode.LeftControl),
                "Plays the previous sound of that list (only has an effect if Debug.Enabled is active).");

            AudioFilter = debug.Bind(
                "Debug",
                "AudioFilter",
                "",
                "Only the sounds whose name contains this text are played by the two keys above.");

            BuoyTintKey = debug.Bind(
                "Debug",
                "BuoyTintKey",
                new KeyboardShortcut(KeyCode.F6, KeyCode.LeftControl),
                "Paints every buoy's body cyan through the next property of its shader in turn, its halo and light too.");

            FlareTintKey = debug.Bind(
                "Debug",
                "FlareTintKey",
                new KeyboardShortcut(KeyCode.F7, KeyCode.LeftControl),
                "Paints every flare gun and its shot with the next colour of the buoys' palette, then a white gun with shots of every colour.");

            MaskTrapKey = debug.Bind(
                "Debug",
                "MaskTrapKey",
                new KeyboardShortcut(KeyCode.F8, KeyCode.LeftControl),
                "Puts the next screen mask on this machine's screen for five seconds.");

            DazeKey = debug.Bind(
                "Debug",
                "DazeKey",
                new KeyboardShortcut(KeyCode.F9, KeyCode.LeftControl),
                "Dazes this machine's player as a big fall does, for the DeathLink knock-out.");

            SimulateGadgetItemKey = debug.Bind(
                "Debug",
                "SimulateGadgetItemKey",
                new KeyboardShortcut(KeyCode.I, KeyCode.LeftControl),
                "Simulates receiving the next filler gadget item, via ItemApplier.ApplyGadgetItem, same code path a real Archipelago item takes.");

            DumpHeldItemKey = debug.Bind(
                "Debug",
                "DumpHeldItemKey",
                new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl),
                "Logs the GameObject name, guid, saveablePropName and propGroups of whatever the local player is currently holding.");

            DumpCosmeticGadgetsKey = debug.Bind(
                "Debug",
                "DumpCosmeticGadgetsKey",
                new KeyboardShortcut(KeyCode.M, KeyCode.LeftControl),
                "Finds every cosmetic gadget clone currently loose in the scene and logs each renderer's actual shader/material/lightmapIndex.");

            DumpNetworkKey = debug.Bind(
                "Debug",
                "DumpNetworkKey",
                new KeyboardShortcut(KeyCode.N, KeyCode.LeftControl),
                "Logs this process's Mirror setup without changing any of it.");

            LoopbackJoinKey = debug.Bind(
                "Debug",
                "LoopbackJoinKey",
                new KeyboardShortcut(KeyCode.L, KeyCode.LeftControl),
                "Co-op on one PC: the host starts a loopback guest, the guest joins the host.");

            EmulateBigLanding = debug.Bind(
                "Debug",
                "EmulateBigLanding",
                false,
                "Does what the Big Landing mod does on every player character, without its DLL.");

            LoopbackFocusKey = debug.Bind(
                "Debug",
                "LoopbackFocusKey",
                new KeyboardShortcut(KeyCode.T, KeyCode.LeftControl),
                "With two instances of the game on this PC: gives the keyboard and mouse to the other one.");

            LoopbackSummonKey = debug.Bind(
                "Debug",
                "LoopbackSummonKey",
                new KeyboardShortcut(KeyCode.C, KeyCode.LeftControl),
                "On the host, with a loopback guest connected.");

            LoopbackVanillaGuestKey = debug.Bind(
                "Debug",
                "LoopbackVanillaGuestKey",
                new KeyboardShortcut(KeyCode.Y, KeyCode.LeftControl),
                "On the host.");

            AutoHostSave = debug.Bind(
                "Debug",
                "AutoHostSave",
                "",
                "For testing.");

            AutoHostGuest = debug.Bind(
                "Debug",
                "AutoHostGuest",
                false,
                "With AutoHostSave.");

            PerfProbe = debug.Bind(
                "Debug",
                "PerfProbe",
                false,
                "For testing.");
        }
    }
}
