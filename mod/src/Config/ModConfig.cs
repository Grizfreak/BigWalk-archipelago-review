using BepInEx.Configuration;

namespace BigWalkArchipelago
{
    // Named ModConfig so it does not clash with the BasePlugin.Config property.
    internal static partial class ModConfig
    {
        internal static ConfigEntry<bool> DebugModeEnabled;
        internal static ConfigEntry<bool> ArchipelagoEnabled;
        internal static ConfigEntry<string> ArchipelagoHostPort;
        internal static ConfigEntry<float> CosmeticGourdRestoreInterval;
        internal static ConfigEntry<bool> ShowConnectionStatus;
        internal static ConfigEntry<int> StatusFontSize;
        internal static ConfigEntry<bool> ShowItemFeed;
        internal static ConfigEntry<bool> GuestsCanAskHints;
        internal static ConfigEntry<int> TilePool;
        internal static ConfigEntry<bool> ApSounds;
        internal static ConfigEntry<bool> ShowObjectives;
        internal static ConfigEntry<bool> ShowTowerOrder;
        internal static ConfigEntry<string> ClimbSpot;
        internal static ConfigEntry<string> ReceivedSound;
        internal static ConfigEntry<string> CheckSound;
        internal static ConfigEntry<float> NoticeSeconds;
        internal static ConfigEntry<int> GadgetCopiesKept;
        internal static ConfigEntry<bool> SpawnGourdAtPlayer;
        internal static ConfigEntry<bool> PutGourdInHands;
        internal static ConfigEntry<bool> ColorBigKeys;
        internal static ConfigEntry<string> KeyColorProperty;
        internal static ConfigEntry<string> GourdName;
        internal static ConfigEntry<bool> DeathLinkEnabled;
        internal static ConfigEntry<bool> GentleEffects;
        internal static ConfigEntry<int> DeathLinkAmnesty;

        // Debug settings live in their own file, written only when Debug.Enabled is on.
        internal const string DebugFileName = "com.grizfreak.bigwalk.archipelago.debug.cfg";

        internal static void Bind(ConfigFile file)
        {
            DebugModeEnabled = file.Bind(
                "Debug",
                "Enabled",
                false,
                "Enables the debug tools (free camera, etc). Leave disabled for normal use.");

            var debug = new ConfigFile(System.IO.Path.Combine(BepInEx.Paths.ConfigPath, DebugFileName), false)
            {
                SaveOnConfigSet = DebugModeEnabled.Value,
            };

            ArchipelagoEnabled = file.Bind(
                "Archipelago",
                "Enabled",
                true,
                "Connects to an Archipelago server when hosting a game. Disable to play with the mod's other features (hub shortcuts, purple gourds on the map, cosmetic spawns) while checks are only logged locally, as they were before the network client existed.");

            ArchipelagoHostPort = file.Bind(
                "Archipelago",
                "HostPort",
                "archipelago.gg:",
                "Last value entered in the host:port field of the hosting screen (Patches/HostMenuConfirmPatch.cs), persisted so it doesn't need to be retyped on every launch.");

            ColorBigKeys = file.Bind(
                "Archipelago",
                "ColorBigKeys",
                true,
                "Tints the five identical yellow big keys, the drawbridge and the four coloured towers, so they can be told apart once they start arriving from Archipelago and piling up at the spawn point. The Black Monolith and Green Dome keys are left alone: they already have models of their own, and repainting them would destroy the one distinction the game gives you for free.");

            KeyColorProperty = debug.Bind(
                "Archipelago",
                "KeyColorProperty",
                "_TintColor",
                "Shader colour properties the tint above is written to, comma-separated. Measured in game on 2026-09-21: the keys run 'househouse/VertexColors', which declares _TintColor, _TintMask0 to _TintMask3, and _EmissionColor, and NOT the _RColor the Black Monolith key's own helper uses, which is why the first attempt silently painted nothing. _TintColor multiplies the mesh's baked vertex colours, which is the classic setup for a shader by that name. If a key still comes out yellow, add the masks (_TintColor,_TintMask0,_TintMask1,_TintMask2,_TintMask3) and see which one owns its body.");

            CosmeticGourdRestoreInterval = debug.Bind(
                "Archipelago",
                "GourdRestoreInterval",
                0.5f,
                "Seconds between each gourd when a session start restores several at once. They are dropped one at a time on the game's own spawn point and left to settle, which is both what keeps them from ending up outside the playable area and what stops a heap of them grinding against each other. Lower is faster but piles them up harder.");

            SpawnGourdAtPlayer = file.Bind(
                "Archipelago",
                "SpawnGourdAtPlayer",
                true,
                "Drops a gourd or gadget received during play just in front of the player it goes to (see PutGourdInHands for who that is) instead of at the hub, so you do not have to walk back across the island for it. Only applies to gourds arriving mid-game: the batch rebuilt when a session starts always goes to the hub, since that is stock rather than a gift and the players are not necessarily near the hub when it happens. Falls back to the hub when there is no player in the world yet.");

            PutGourdInHands = file.Bind(
                "Archipelago",
                "PutGourdInHands",
                true,
                "Puts a gourd or gadget received during play straight into a player's hands. It goes to a player whose hands are free, the one given the fewest items this session first, ties at random; if nobody's hands are free, to one of everyone the same way, who drops what they were holding first. The count is not saved. With this off, items only drop in front of that player. Never applies to the batch rebuilt at the start of a session. Note that a gourd received inside a sealed puzzle room stays there until the next world load, hands or not, the game does not let you carry it out.");

            GourdName = file.Bind(
                "Archipelago",
                "GourdName",
                "Gourd",
                "What the gourds are called on screen when this machine hosts (Settings > Archipelago > Gourd name): the item feed and the goal line say it instead of \"Gourd\", on every player's screen. Guests see the host's. Left at \"Gourd\", shows the seed's gourd_name.");

            GentleEffects = file.Bind(
                "Archipelago",
                "GentleEffects",
                false,
                "Accessibility, this machine only (Settings > Archipelago > Gentle effects): on, the traps that are hard to look at are softened here (no Big Flare, Big Goggles or Big Pixels on this screen, Big Smell without its gas, Big Brain's lamps without their light), for photosensitive players. The other players keep theirs; nothing else changes, the clock included.");

            DeathLinkEnabled = file.Bind(
                "Archipelago",
                "DeathLinkEnabled",
                true,
                "When this machine hosts (Settings > Archipelago > DeathLink): false sends no DeathLink and takes none, whatever the seed's death_link says; true does what the seed says. Live: the DeathLink tag comes off the connection at once.");

            DeathLinkAmnesty = file.Bind(
                "Archipelago",
                "DeathLinkAmnesty",
                -1,
                new ConfigDescription(
                    "When this machine hosts (Settings > Archipelago > DeathLink amnesty): how many failed puzzles are forgiven before one sends a DeathLink. -1 keeps the seed's death_link_amnesty.",
                    new AcceptableValueRange<int>(-1, 10)));

            ShowConnectionStatus = file.Bind(
                "Archipelago",
                "ShowConnectionStatus",
                true,
                "Shows the Archipelago connection in the corner of the screen, for the whole session rather than only when something is wrong, a silent, self-healing connection is still worth being able to check at a glance. Only ever appears for the host, since nobody else connects. Turn off for a clean screen; the same information stays in the BepInEx log either way. This is the master switch: the item feed and the resync hint below are drawn under this line and go with it.");

            StatusFontSize = file.Bind(
                "Archipelago",
                "StatusFontSize",
                22,
                "Size of the Archipelago status line, in points. It sits over whatever the game is rendering, so it is drawn with a shadow rather than a panel; raise this if it is hard to read at your resolution. The feed and the hint under it are drawn slightly smaller than this, proportionally.");

            GuestsCanAskHints = file.Bind(
                "Archipelago",
                "GuestsCanAskHints",
                false,
                "On the host: whether the guests may ask for a hint at the hub's hint board (they then get the item names' suggestions too). Also in the game's Settings > Archipelago. Off, the board stays on its hook for them and only the host asks; a hint costs the slot's points either way.");

            TilePool = file.Bind(
                "Archipelago",
                "TilePool",
                0,
                new ConfigDescription(
                    "When this machine hosts (Settings > Archipelago > Tile pool): how the PNGs of BepInEx/config/BigWalkArchipelago/tiles/pool are drawn for the nine icon tiles. 0 over the seed's own draw (custom_tiles), 1 only on the icons the seed left as the game's, 2 off. A PegTileA<n>.png of its own always wins.",
                    new AcceptableValueRange<int>(0, 2)));

            ShowItemFeed = file.Bind(
                "Archipelago",
                "ShowItemFeed",
                true,
                "Lists the lines under the status line (items received, DeathLink, traps that did nothing, a key placed before its item...), each fading after a few seconds; false hides them all. Also in the game's Settings > Archipelago. Big Walk itself never says whether a puzzle counted or what just arrived from the multiworld, which left 'nothing happened' and 'it happened and I could not see it' looking identical. Identical lines in a row collapse into a count, so the burst replayed on connect does not fill the screen.");

            ApSounds = file.Bind(
                "Archipelago",
                "ApSounds",
                true,
                "A small sound when an item is received from the multiworld, and one when a check other than a puzzle is sent. One sound stands for a burst: after one, none for fifteen seconds.");

            ShowObjectives = file.Bind(
                "Archipelago",
                "ShowObjectives",
                false,
                "Keeps the seed's objectives on the overlay at all times, ticked or not. Off: they are on the board in the hub and at each ending's entry point only. A seed with a goal alone shows that goal as its one objective.");

            ShowTowerOrder = file.Bind(
                "Archipelago",
                "ShowTowerOrder",
                false,
                "Lists on the board in the hub the order the seed opens the towers in. Off (the default): only the towers already full and the one open are named, the others are hidden so the order stays a surprise. Only has an effect when a seed opens the towers in order (tower_mode in_order or by_item).");

            ClimbSpot = debug.Bind(
                "Archipelago",
                "ClimbSpot",
                "94.5, 179.6, -329.1, 25",
                "Where the objective big_climb (the island's highest point) is done: x, y, z and a radius, in metres. The default is the Peak fireworks. A player within the radius does it.");

            ReceivedSound = file.Bind(
                "Archipelago",
                "ReceivedSound",
                "",
                "The game's own clip played when an item is received (empty: the default). Names come from the debug key Ctrl + 6, which cycles through candidates and shows each name on screen.");

            CheckSound = file.Bind(
                "Archipelago",
                "CheckSound",
                "",
                "The game's own clip played when a check other than a puzzle is sent (empty: the default).");

            NoticeSeconds = file.Bind(
                "Archipelago",
                "NoticeSeconds",
                8f,
                "How long, in seconds, a line in the feed above stays on screen.");

            GadgetCopiesKept = file.Bind(
                "Archipelago",
                "GadgetCopiesKept",
                10,
                "How many copies of each received object (map, torch, radio...) the world keeps at most. Past that, a reload puts back no more, and a new one takes the place of the oldest left lying around (never one held, worn or stowed). Backpacks, belts and cartons are only limited on reload. 0: no limit.");

#if DEBUG
            BindDebugWorldKeys(debug);
            BindDebugSessionKeys(debug);
#endif

            if (DebugModeEnabled.Value)
                debug.Save();
            DropOrphans(file);
        }

        // BepInEx writes back settings an older version left; drop them once.
        private static void DropOrphans(ConfigFile file)
        {
            try
            {
                var property = HarmonyLib.AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
                if (property?.GetValue(file) is System.Collections.IDictionary orphans && orphans.Count > 0)
                {
                    orphans.Clear();
                    file.Save();
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log?.LogWarning($"[{nameof(ModConfig)}] Old settings left in the .cfg: {ex.Message}");
            }
        }
    }
}
