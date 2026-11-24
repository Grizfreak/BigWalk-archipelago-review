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
                "Lists the objects loaded around you that could plausibly become Archipelago items, the ones carrying a savablePropGuid (the game's own per-prop identity, and the only thing a location could be keyed on), a radioVoiceAssigner (a walkie-talkie) or a use-while-held switch. The binary names none of these: they are plain Prop prefabs, so only the running game can inventory them (only has an effect if Debug.Enabled is active).");

            DumpGourdRosterKey = debug.Bind(
                "Debug",
                "DumpGourdRosterKey",
                new KeyboardShortcut(KeyCode.V, KeyCode.LeftControl),
                "Lists every RewardGourd loaded around you with its saveablePropName and whether it is a purple 'variant challenge' gourd. Written to settle which puzzles sit behind the chairlift: the world's logic assumes the island is open apart from the ending, and a puzzle that is not would make some seeds unbeatable. Press it in the gated zone and again somewhere plainly open, the difference is the set that needs its own region (only has an effect if Debug.Enabled is active).");

            PositionPingKey = debug.Bind(
                "Debug",
                "PositionPingKey",
                new KeyboardShortcut(KeyCode.X, KeyCode.LeftControl),
                "Logs the player's current world position and, of every puzzle and big-key plinth known to the Archipelago world, the single nearest one and its distance, for labelling what a puzzle needs (a sound cue, a timer, a held item, ...) against the coordinates already pulled from DebugPuzzleMapDump, without having to eyeball which dot on a map you are standing next to (only has an effect if Debug.Enabled is active).");

            LookAtKey = debug.Bind(
                "Debug",
                "LookAtKey",
                new KeyboardShortcut(KeyCode.E, KeyCode.LeftControl),
                "Logs the object under the crosshair (hierarchy path, components of it and of its parents, nearest puzzle) and everything with a collider within four metres, for naming a puzzle's parts (a speaker, a teapot, a light, a panel) that no class name gives away (only has an effect if Debug.Enabled is active).");

            DaylightKey = debug.Bind(
                "Debug",
                "DaylightKey",
                new KeyboardShortcut(KeyCode.KeypadMultiply, KeyCode.LeftControl),
                "Fixes the time of day at noon (and stops the clock); press again to let it run (only has an effect if Debug.Enabled is active).");

            LockButtonsKey = debug.Bind(
                "Debug",
                "LockButtonsKey",
                new KeyboardShortcut(KeyCode.KeypadMinus, KeyCode.LeftControl),
                "Locks every puzzle need (their objects on the map are hidden), or unlocks them all; for trying lock_puzzle_needs without a server (only has an effect if Debug.Enabled is active).");

            LockNextNeedKey = debug.Bind(
                "Debug",
                "LockNextNeedKey",
                new KeyboardShortcut(KeyCode.KeypadPlus, KeyCode.LeftControl),
                "Locks or unlocks the next puzzle need in the list, one per press, to try them one by one (only has an effect if Debug.Enabled is active).");

            NeedStatusKey = debug.Bind(
                "Debug",
                "NeedStatusKey",
                new KeyboardShortcut(KeyCode.KeypadDivide, KeyCode.LeftControl),
                "Logs, with the time, what this machine believes about the puzzle needs: its role, which are locked and how many objects of each are showing. Press it whenever something looks wrong, so the log says when (only has an effect if Debug.Enabled is active).");

            SendDeathLinkKey = debug.Bind(
                "Debug",
                "SendDeathLinkKey",
                new KeyboardShortcut(KeyCode.P, KeyCode.LeftControl),
                "Sends a DeathLink to the multiworld (a Bounce packet tagged DeathLink), for a friend whose game listens for them. Host only: a guest has no connection (only has an effect if Debug.Enabled is active).");

            FireTrapKey = debug.Bind(
                "Debug",
                "FireTrapKey",
                new KeyboardShortcut(KeyCode.Keypad0, KeyCode.LeftControl),
                "On the host: fires the next trap or bonus on every player, one per press in turn (Big Drop, Big Throw, Big Trip, Big Meeting, Big Night, Big Sleep, Big Load, Big Day, Big Speed, Big Jump, Big Confetti), as if its item had arrived; works without a server, for 30 seconds then. Its name goes in the log and the feed (only has an effect if Debug.Enabled is active).");

            TrapStateKey = debug.Bind(
                "Debug",
                "TrapStateKey",
                new KeyboardShortcut(KeyCode.KeypadEnter, KeyCode.LeftControl),
                "Logs what the traps see: on this machine the effects running, the screen mask, the hour; on the host every player (where, in the Gauntlet or not, masked or not) and every place Big Trip could send someone now (only has an effect if Debug.Enabled is active).");

            MarkTripSpotKey = debug.Bind(
                "Debug",
                "MarkTripSpotKey",
                new KeyboardShortcut(KeyCode.KeypadPeriod, KeyCode.LeftControl),
                "Writes where the player stands (position, facing, nearest puzzle) to BepInEx/trip-spots.txt, to mark the places Big Trip should be able to send a player beside a puzzle or a firework (only has an effect if Debug.Enabled is active).");

            PickupCensusKey = debug.Bind(
                "Debug",
                "PickupCensusKey",
                new KeyboardShortcut(KeyCode.F1, KeyCode.LeftControl),
                "Writes BepInEx/pickup-census.tsv: every backpack, belt and gourd carton of the island and everything that could be a firework launcher, with its guid, position, nearest puzzle and the map versions (2, 3, 4 players) it belongs to. One press from anywhere: the game builds every object everywhere (only has an effect if Debug.Enabled is active).");

            PickupTeleportKey = debug.Bind(
                "Debug",
                "PickupTeleportKey",
                new KeyboardShortcut(KeyCode.F3, KeyCode.LeftControl),
                "Teleports the player beside the next backpack, belt, gourd carton or firework launcher of the island, in turn: for walking every pack and firework check without crossing the island (only has an effect if Debug.Enabled is active).");

            ColourProbeKey = debug.Bind(
                "Debug",
                "ColourProbeKey",
                new KeyboardShortcut(KeyCode.F5, KeyCode.LeftControl),
                "Logs what carries the colour of the buoys, the flare guns and their flares (helpers, shader colour properties, lights, particles), one example of each kind (only has an effect if Debug.Enabled is active).");

            AudioProbeKey = debug.Bind(
                "Debug",
                "AudioProbeKey",
                new KeyboardShortcut(KeyCode.Backslash, KeyCode.LeftControl),
                "Writes BepInEx/audio-probe.tsv: every audio effect of the scene with the state system it listens to and the clip it plays in each state, to find the game's own sounds for the monuments, the puzzles' end and a click (only has an effect if Debug.Enabled is active).");

            MarkObjectiveSpotKey = debug.Bind(
                "Debug",
                "MarkObjectiveSpotKey",
                new KeyboardShortcut(KeyCode.Alpha1, KeyCode.LeftAlt),
                "Writes where the player stands to BepInEx/objective-spots.txt, as the line for ClimbSpot or ClubSpot (only has an effect if Debug.Enabled is active).");

            ResetObjectiveSpotsKey = debug.Bind(
                "Debug",
                "ResetObjectiveSpotsKey",
                new KeyboardShortcut(KeyCode.Alpha2, KeyCode.LeftAlt),
                "Forgets that big_climb and big_party were done, to try them again (only has an effect if Debug.Enabled is active).");

            ApFeedKey = debug.Bind(
                "Debug",
                "ApFeedKey",
                new KeyboardShortcut(KeyCode.Alpha5, KeyCode.LeftControl),
                "Shows, one per press, the overlay's lines and plays the sounds of the Archipelago feed without a server: an item received, a check sent, DeathLink sent, received and forgiven, Big Trip with nowhere to go, the settings overriding the seed, a key placed before its item (only has an effect if Debug.Enabled is active).");

            FakeObjectivesKey = debug.Bind(
                "Debug",
                "FakeObjectivesKey",
                new KeyboardShortcut(KeyCode.Alpha7, KeyCode.LeftControl),
                "Cycles fake objectives (the Gauntlet and the gourds, two endings, none) to see the board in the hub, the overlay line and the shut endings without a server (only has an effect if Debug.Enabled is active).");

            OpenLastStairwayKey = debug.Bind(
                "Debug",
                "OpenLastStairwayKey",
                new KeyboardShortcut(KeyCode.Alpha8, KeyCode.LeftControl),
                "Asks the Gauntlet's last stairway (to the bell chamber) to open, to test the objectives' hold on it (only has an effect if Debug.Enabled is active).");

            MonumentModeKey = debug.Bind(
                "Debug",
                "MonumentModeKey",
                new KeyboardShortcut(KeyCode.Alpha9, KeyCode.LeftControl),
                "Cycles the monuments held to an order (in_order, by_item, none) without a server: the first is the Red tower's, then the drawbridge's, the Green, Blue, Yellow, Black and the Green Dome (only has an effect if Debug.Enabled is active).");

            GrantMonumentKey = debug.Bind(
                "Debug",
                "GrantMonumentKey",
                new KeyboardShortcut(KeyCode.Alpha0, KeyCode.LeftControl),
                "Receives one Progressive Monument, as with by_item in the debug monuments (only has an effect if Debug.Enabled is active).");

            FillMonumentKey = debug.Bind(
                "Debug",
                "FillMonumentKey",
                new KeyboardShortcut(KeyCode.Alpha3, KeyCode.LeftAlt),
                "Fills, in the save, the first monument of the debug order (Ctrl + 9) that is not full, so the next one can open (only has an effect if Debug.Enabled is active).");

            LatchGoalFlagsKey = debug.Bind(
                "Debug",
                "LatchGoalFlagsKey",
                new KeyboardShortcut(KeyCode.Alpha4, KeyCode.LeftAlt),
                "Latches in the save the bell and the Gauntlet as if both had been played, to see an objectives goal go out without playing them (only has an effect if Debug.Enabled is active).");

            LaserBeamKey = debug.Bind(
                "Debug",
                "LaserBeamKey",
                new KeyboardShortcut(KeyCode.Alpha5, KeyCode.LeftAlt),
                "Puts the next of a few emission settings on every laser beam, to see whether its red can be pushed aside; the last puts the game's own back (only has an effect if Debug.Enabled is active).");

            SoundCandidatesKey = debug.Bind(
                "Debug",
                "SoundCandidatesKey",
                new KeyboardShortcut(KeyCode.Alpha6, KeyCode.LeftControl),
                "Plays the next candidate for the 'item received' sound and shows its name on screen, to choose it by ear (only has an effect if Debug.Enabled is active).");

            AudioNextKey = debug.Bind(
                "Debug",
                "AudioNextKey",
                new KeyboardShortcut(KeyCode.RightBracket, KeyCode.LeftControl),
                "Plays the next sound the audio probe or the played-sound log has seen (names containing Debug.AudioFilter) where the player stands, and logs its name: to hear a candidate (only has an effect if Debug.Enabled is active).");

            AudioPrevKey = debug.Bind(
                "Debug",
                "AudioPrevKey",
                new KeyboardShortcut(KeyCode.LeftBracket, KeyCode.LeftControl),
                "Plays the previous sound of that list (only has an effect if Debug.Enabled is active).");

            AudioFilter = debug.Bind(
                "Debug",
                "AudioFilter",
                "",
                "Only the sounds whose name contains this text are played by the two keys above (empty: all). Read when a key is pressed, so it can be changed in the .cfg while the game runs only through a reload: set it before launching.");

            BuoyTintKey = debug.Bind(
                "Debug",
                "BuoyTintKey",
                new KeyboardShortcut(KeyCode.F6, KeyCode.LeftControl),
                "Paints every buoy's body cyan through the next property of its shader in turn (_TintColor, then _TintMask0 to 3, then back to normal), its halo and light too: which one recolours the body (only has an effect if Debug.Enabled is active).");

            FlareTintKey = debug.Bind(
                "Debug",
                "FlareTintKey",
                new KeyboardShortcut(KeyCode.F7, KeyCode.LeftControl),
                "Paints every flare gun and its shot with the next colour of the buoys' palette, then a white gun with shots of every colour, then the shots back (only has an effect if Debug.Enabled is active).");

            MaskTrapKey = debug.Bind(
                "Debug",
                "MaskTrapKey",
                new KeyboardShortcut(KeyCode.F8, KeyCode.LeftControl),
                "Puts the next screen mask (binoculars, telescope, blindfold) on this machine's screen for five seconds, as Big Mask would (only has an effect if Debug.Enabled is active).");

            DazeKey = debug.Bind(
                "Debug",
                "DazeKey",
                new KeyboardShortcut(KeyCode.F9, KeyCode.LeftControl),
                "Dazes this machine's player as a big fall does (PlayerFaller.TriggerFall), for the DeathLink knock-out (only has an effect if Debug.Enabled is active).");

            SimulateGadgetItemKey = debug.Bind(
                "Debug",
                "SimulateGadgetItemKey",
                new KeyboardShortcut(KeyCode.I, KeyCode.LeftControl),
                "Simulates receiving the next filler gadget item (Megaphone, Walkie-Talkie, Backpack, Belt, Flare Gun, one per press, cycling), via ItemApplier.ApplyGadgetItem, same code path a real Archipelago item takes, without a network connection. Tests GadgetItemSpawner's clone/spawn in isolation (only has an effect if Debug.Enabled is active).");

            DumpHeldItemKey = debug.Bind(
                "Debug",
                "DumpHeldItemKey",
                new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl),
                "Logs the GameObject name, guid, saveablePropName and propGroups of whatever the local player is currently holding, for identifying an object by description alone, without needing to know its prefab name first (only has an effect if Debug.Enabled is active).");

            DumpCosmeticGadgetsKey = debug.Bind(
                "Debug",
                "DumpCosmeticGadgetsKey",
                new KeyboardShortcut(KeyCode.M, KeyCode.LeftControl),
                "Finds every cosmetic gadget clone currently loose in the scene (the ones a filler item spawns) and logs each renderer's actual shader/material/lightmapIndex, for diagnosing the pink/magenta clones directly rather than through a held item, whose visible mesh turned out not to live under prop.gameObject at all (only has an effect if Debug.Enabled is active).");

            DumpNetworkKey = debug.Bind(
                "Debug",
                "DumpNetworkKey",
                new KeyboardShortcut(KeyCode.N, KeyCode.LeftControl),
                "Logs this process's Mirror setup without changing any of it: the NetworkManager, the active transport and every transport under it (with the Kcp port and whether its server is running), the authenticator, the server's connections with the identifier each one sent, Application.runInBackground, whether Steam is up, and the command line. Written for running a second instance of the game on the same PC as a network guest: it answers whether the host already listens on UDP and what the two instances would share (only has an effect if Debug.Enabled is active).");

            LoopbackJoinKey = debug.Bind(
                "Debug",
                "LoopbackJoinKey",
                new KeyboardShortcut(KeyCode.L, KeyCode.LeftControl),
                "Co-op on one PC. On the host: starts a second instance of the game as the loopback guest, windowed, which joins on its own if you are already hosting a world. In that guest: joins the host on 127.0.0.1 over Kcp from the title menu, for when the host was not hosting yet or to join again after leaving. tools/launch-guest.ps1 starts the same guest from a terminal (only has an effect if Debug.Enabled is active).");

            EmulateBigLanding = debug.Bind(
                "Debug",
                "EmulateBigLanding",
                false,
                "Does what the Big Landing mod does (predictive pick-up and drop, no big falls) on every player character, without its DLL, which crashes the loopback guest. For co-op tests of another player's mods (only has an effect if Debug.Enabled is active).");

            LoopbackFocusKey = debug.Bind(
                "Debug",
                "LoopbackFocusKey",
                new KeyboardShortcut(KeyCode.T, KeyCode.LeftControl),
                "With two instances of the game on this PC (a host and the loopback guest from tools/launch-guest.ps1): gives the keyboard and mouse to the other one. Press it in either window. Both keep running while unfocused (only has an effect if Debug.Enabled is active).");

            LoopbackSummonKey = debug.Bind(
                "Debug",
                "LoopbackSummonKey",
                new KeyboardShortcut(KeyCode.C, KeyCode.LeftControl),
                "On the host, with a loopback guest connected: teleports the guest next to you, through the mod's own network channel and the game's own player teleport. Never reaches a player on another machine (only has an effect if Debug.Enabled is active).");

            LoopbackVanillaGuestKey = debug.Bind(
                "Debug",
                "LoopbackVanillaGuestKey",
                new KeyboardShortcut(KeyCode.Y, KeyCode.LeftControl),
                "On the host: like LoopbackJoinKey, but the second instance runs without the mod, it only joins, and meets the host's items, messages and objects as a PlayStation or Xbox player would. For testing that players without the mod are never blocked (only has an effect if Debug.Enabled is active).");

            AutoHostSave = debug.Bind(
                "Debug",
                "AutoHostSave",
                "",
                "For testing: the name of an existing save to host as soon as the game is up, through the menus a player would use (its Archipelago slot is its own name, the server HostPort above). Empty: the title menu as usual (only has an effect if Debug.Enabled is active).");

            AutoHostGuest = debug.Bind(
                "Debug",
                "AutoHostGuest",
                false,
                "With AutoHostSave: also start the loopback guest once the world is up, as Ctrl+L does (only has an effect if Debug.Enabled is active).");

            PerfProbe = debug.Bind(
                "Debug",
                "PerfProbe",
                false,
                "For testing: times the mod's own Update, LateUpdate, FixedUpdate and OnGUI and logs the slowest every 30 s (read at launch).");
        }
    }
}
