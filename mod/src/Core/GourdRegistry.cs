using System;
using System.Collections.Generic;

namespace BigWalkArchipelago.Core
{
    // Maps a SaveablePropName to its Archipelago location id (the enum name).
    internal static class GourdRegistry
    {
        // Big keys are named by colour, their plinths by place, so the mapping has to be listed.
        private static readonly Dictionary<SaveablePropName, SaveableHomeName> BigKeyHomesByProp = new()
        {
            { SaveablePropName.bigKeyIntro, SaveableHomeName.bigKeyPlinthIntro },
            { SaveablePropName.bigKeyRedZone, SaveableHomeName.bigKeyPlinthMapRoom },
            { SaveablePropName.bigKeyGreenZone, SaveableHomeName.bigKeyPlinthSkiLift },
            { SaveablePropName.bigKeyBlueZone, SaveableHomeName.bigKeyPlinthTrain },
            { SaveablePropName.bigKeyYellowZone, SaveableHomeName.bigKeyPlinthTunnels },
            { SaveablePropName.bigKeyBoss, SaveableHomeName.bigKeyPlinthEnding },
            { SaveablePropName.bigKeyOverflow, SaveableHomeName.bigKeyPlinthGoodbye2 },
        };

        private static readonly HashSet<SaveablePropName> AbsentFromTheBuild = new()
        {
            SaveablePropName.gourdBunker,
            SaveablePropName.gourdHighPegBoard,
            SaveablePropName.gourdFirstPegBoard,
            SaveablePropName.gourdMagiciansTrick,
            SaveablePropName.gourdButtonBoothChallenge,
            SaveablePropName.gourdTileSoup,
            SaveablePropName.gourdPanopticon,
            SaveablePropName.gourdMaypole,
            SaveablePropName.gourdBlindfoldCircus,
            SaveablePropName.gourdMessengerRun,
            SaveablePropName.gourdHotPotato,
            SaveablePropName.gourdScoutTiles,
            SaveablePropName.gourdScoutCounting,
        };

        // Must stay below AbsentFromTheBuild: static initializers run in textual order.
        private static readonly Dictionary<SaveablePropName, string> LocationIdsByProp = BuildLocationIds();

        // The game's enums disagree on these three names, so "gourd" -> "valet" fails.
        private static readonly Dictionary<SaveablePropName, SaveableHomeName> GourdHomeNameExceptions = new()
        {
            { SaveablePropName.gourdCenturonSong, SaveableHomeName.valetCenturionSong },
            { SaveablePropName.gourdSingerAndSelecter, SaveableHomeName.valetSingerAndSelector },
            { SaveablePropName.gourdDancerAndSelecter, SaveableHomeName.valetDancerAndSelector },
        };

        internal static IEnumerable<SaveablePropName> BigKeyProps => BigKeyHomesByProp.Keys;

        internal static bool TryGetBigKeyHome(SaveablePropName propName, out SaveableHomeName home) =>
            BigKeyHomesByProp.TryGetValue(propName, out home);

        internal static bool TryGetLocationId(SaveablePropName propName, out string locationId)
        {
            return LocationIdsByProp.TryGetValue(propName, out locationId);
        }

        internal static bool IsBigKey(SaveablePropName propName)
        {
            return BigKeyHomesByProp.ContainsKey(propName);
        }

        // PropHome.GetSaveableHome throws when no world is loaded; always go through here.
        internal static PropHome TryGetHome(SaveableHomeName homeName)
        {
            try
            {
                return PropHome.GetSaveableHome(homeName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static PropHome TryGetHomeFor(SaveablePropName propName)
        {
            return TryGetHomeName(propName, out var homeName) ? TryGetHome(homeName) : null;
        }

        // A big key also has a notSavable home at its tower, where Prop.Start() pins it on load.
        internal static bool IsBigKeyPlinth(SaveableHomeName homeName)
        {
            return BigKeyHomesByProp.ContainsValue(homeName);
        }

        // Prop.allProps throws when no world is loaded.
        internal static Prop FindProp(SaveablePropName propName)
        {
            try
            {
                foreach (var prop in Prop.allProps)
                {
                    if (prop != null && prop.saveablePropName == propName)
                        return prop;
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        internal static IEnumerable<Prop> LoadedBigKeys()
        {
            var found = new List<Prop>();

            try
            {
                foreach (var prop in Prop.allProps)
                {
                    if (prop != null && IsBigKey(prop.saveablePropName))
                        found.Add(prop);
                }
            }
            catch (Exception)
            {
            }

            return found;
        }

        internal static IEnumerable<SaveablePropName> BigKeys => BigKeyHomesByProp.Keys;

        // gourdXxx (the prop) maps 1:1 to valetXxx (its monument slot).
        internal static bool TryGetHomeName(SaveablePropName propName, out SaveableHomeName homeName)
        {
            if (BigKeyHomesByProp.TryGetValue(propName, out homeName))
                return true;

            if (GourdHomeNameExceptions.TryGetValue(propName, out homeName))
                return true;

            var name = propName.ToString();
            if (name.StartsWith("gourd", StringComparison.Ordinal))
                return Enum.TryParse("valet" + name.Substring("gourd".Length), out homeName);

            homeName = default;
            return false;
        }

        private static Dictionary<SaveablePropName, string> BuildLocationIds()
        {
            var map = new Dictionary<SaveablePropName, string>();

            foreach (SaveablePropName propName in Enum.GetValues(typeof(SaveablePropName)))
            {
                if (propName == SaveablePropName.notSavable)
                    continue;

                // Dev-only test values, never real checks.
                if (propName.ToString().Contains("Testing", StringComparison.OrdinalIgnoreCase))
                    continue;

                // No matching valet and not in the apworld: sending it would be a protocol error.
                if (propName == SaveablePropName.gourdSecretZoneVice)
                    continue;

                // In the enum but never produced by the game; the apworld excludes the same set.
                if (AbsentFromTheBuild.Contains(propName))
                    continue;

                map[propName] = propName.ToString();
            }

            return map;
        }
    }
}
