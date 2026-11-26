using BepInEx.Configuration;

namespace BigWalkArchipelago
{
    // Named ModConfig so it does not clash with the BasePlugin.Config property.
    internal static partial class ModConfig
    {
        internal static ConfigEntry<bool> ArchipelagoEnabled;
        internal static ConfigEntry<string> ArchipelagoHostPort;
        internal static ConfigEntry<bool> ShowConnectionStatus;
        internal static ConfigEntry<int> StatusFontSize;
        internal static ConfigEntry<bool> ShowItemFeed;
        internal static ConfigEntry<bool> GuestsCanAskHints;
        internal static ConfigEntry<bool> GuestsCanResync;
        internal static ConfigEntry<int> TilePool;
        internal static ConfigEntry<bool> ApSounds;
        internal static ConfigEntry<bool> ShowObjectives;
        internal static ConfigEntry<bool> ShowTowerOrder;
        internal static ConfigEntry<float> NoticeSeconds;
        internal static ConfigEntry<int> GadgetCopiesKept;
        internal static ConfigEntry<bool> SpawnGourdAtPlayer;
        internal static ConfigEntry<bool> PutGourdInHands;
        internal static ConfigEntry<bool> ColorBigKeys;
        internal static ConfigEntry<string> GourdName;
        internal static ConfigEntry<bool> DeathLinkEnabled;
        internal static ConfigEntry<bool> GentleEffects;
        internal static ConfigEntry<int> DeathLinkAmnesty;

        internal static void Bind(ConfigFile file)
        {
            ArchipelagoEnabled = file.Bind(
                "Archipelago",
                "Enabled",
                true,
                "Connects to an Archipelago server when hosting a game. Off: checks are only logged.");

            ArchipelagoHostPort = file.Bind(
                "Archipelago",
                "HostPort",
                "archipelago.gg:",
                "Last address typed on the hosting screen.");

            ColorBigKeys = file.Bind(
                "Archipelago",
                "ColorBigKeys",
                true,
                "Tints the drawbridge key and the four coloured towers' keys so they can be told apart.");

            SpawnGourdAtPlayer = file.Bind(
                "Archipelago",
                "SpawnGourdAtPlayer",
                true,
                "Drops an item received during play in front of a player instead of at the hub. Items rebuilt at the start of a session always go to the hub.");

            PutGourdInHands = file.Bind(
                "Archipelago",
                "PutGourdInHands",
                true,
                "Puts an item received during play into a player's free hands, the one given the fewest items this session first. Off: it only drops in front of them.");

            GourdName = file.Bind(
                "Archipelago",
                "GourdName",
                "Gourd",
                "What the gourds are called on screen when this machine hosts. Left at \"Gourd\", shows the seed's gourd_name.");

            GentleEffects = file.Bind(
                "Archipelago",
                "GentleEffects",
                false,
                "Softens the traps that are hard to look at (flashes), on this machine only.");

            DeathLinkEnabled = file.Bind(
                "Archipelago",
                "DeathLinkEnabled",
                true,
                "When this machine hosts: false sends and takes no DeathLink, true does what the seed says.");

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
                "Shows the Archipelago connection and the lines under it in the corner of the screen.");

            StatusFontSize = file.Bind(
                "Archipelago",
                "StatusFontSize",
                22,
                "Size of the status line, in points. The lines under it are slightly smaller.");

            GuestsCanAskHints = file.Bind(
                "Archipelago",
                "GuestsCanAskHints",
                false,
                "On the host: whether the guests may ask for a hint at the hub's hint board. A hint costs the slot's points.");

            GuestsCanResync = file.Bind(
                "Archipelago",
                "GuestsCanResync",
                false,
                "On the host: whether the guests may press the resync stations. Off, only the host can.");

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
                "Lists items received, checks sent and other events under the status line.");

            ApSounds = file.Bind(
                "Archipelago",
                "ApSounds",
                true,
                "A small sound when an item is received, and one when a check other than a puzzle is sent.");

            ShowObjectives = file.Bind(
                "Archipelago",
                "ShowObjectives",
                false,
                "Keeps the seed's objectives on the overlay at all times. Off: on the board in the hub only.");

            ShowTowerOrder = file.Bind(
                "Archipelago",
                "ShowTowerOrder",
                false,
                "Lists on the board in the hub the order the seed opens the towers in. Off: only the towers full and the one open.");

            NoticeSeconds = file.Bind(
                "Archipelago",
                "NoticeSeconds",
                8f,
                "How long, in seconds, a line under the status line stays on screen.");

            GadgetCopiesKept = file.Bind(
                "Archipelago",
                "GadgetCopiesKept",
                10,
                "How many copies of each received object the world keeps at most. 0: no limit.");

#if DEBUG
            BindDebug(file);
#endif
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
