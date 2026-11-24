namespace BigWalkArchipelago.Core.Items
{
    // Clones the island's hand props as filler items; each machine hides its own vanilla copies.
    internal static partial class GadgetItemSpawner
    {
        private static readonly Dictionary<GadgetKind, string> PrefabNames = new()
        {
            { GadgetKind.Megaphone, "MegaphoneProp" },
            { GadgetKind.WalkieTalkie, "WalkieTalkieProp" },
            { GadgetKind.Backpack, "BackpackProp" },
            { GadgetKind.Belt, "HolsterProp" },
            { GadgetKind.FlareGun, "FlareGunProp" },
            { GadgetKind.Laser, "LaserProp" },
            { GadgetKind.Binoculars, "BinocularsProp" },
            { GadgetKind.Compass, "CompassProp" },
            { GadgetKind.FoldingMap, "FoldingMapProp" },
            { GadgetKind.Radio, "FmRadioProp" },
            { GadgetKind.GourdCarton, "GourdCartonProp" },
            { GadgetKind.Torch, "TorchProp" },
            // Left in the world (KeptInWorld): a buoy light is scenery worth keeping lit.
            { GadgetKind.Lamp, "BuoyLight" },
            { GadgetKind.XrayGoggles, "XrayGogglesProp" },
            // Distinct prefabs, not a colour variant of the base FlareGunProp.
            { GadgetKind.FlareGunBlue, "FlareGunPropBlue" },
            { GadgetKind.FlareGunGreen, "FlareGunPropGreen" },
            { GadgetKind.FlareGunYellow, "FlareGunPropYellow" },
            { GadgetKind.Pomodoro, "Pomodoro" },

            // After FlareGun, so that MatchKind still names a vanilla FlareGunProp a FlareGun.
            { GadgetKind.RainbowFlareGun, "FlareGunProp" },
            { GadgetKind.Blindfold, "BlindfoldProp" },
            { GadgetKind.Clock, "ClockProp" },
            { GadgetKind.Chair, "foldingChair" },
        };

        // Round lamps are three prefabs; only BuoyProp has a yellow light, so it lends its look.
        private static readonly Dictionary<GadgetKind, string[]> AlternatePrefabNames = new()
        {
            { GadgetKind.Lamp, new[] { "BuoyProp", "BuoyRedProp" } },
        };

        private static bool MatchesKind(GadgetKind kind, string name)
        {
            if (NameMatches(name, PrefabNames[kind]))
                return true;

            if (AlternatePrefabNames.TryGetValue(kind, out var alternates))
            {
                foreach (var alternate in alternates)
                {
                    if (NameMatches(name, alternate))
                        return true;
                }
            }

            return false;
        }

        private static readonly HashSet<GadgetKind> KeptInWorld = new()
        {
            GadgetKind.Lamp,

            // The puzzles' tomatoes stay where they are: a clone is a trap's, never one of theirs.
            GadgetKind.Pomodoro,

            // The blindfold puzzles' helmets too, for the same reason.
            GadgetKind.Blindfold,

            GadgetKind.Clock,

            GadgetKind.Chair,
        };

        internal const float TomatoSeconds = 3f;

        // Packs, flare guns and these lend no tickets, so host and guests agree before the options.
        private static readonly HashSet<GadgetKind> IslandObjects = new()
        {
            GadgetKind.Megaphone, GadgetKind.WalkieTalkie, GadgetKind.Laser, GadgetKind.Binoculars,
            GadgetKind.Compass, GadgetKind.FoldingMap, GadgetKind.Radio, GadgetKind.Torch, GadgetKind.XrayGoggles,
        };

        private static readonly HashSet<GadgetKind> Packs = new()
        {
            GadgetKind.Backpack,
            GadgetKind.Belt,
            GadgetKind.GourdCarton,
        };

        // The flare guns stay on the island whatever the options; no flare gun is filler any more.
        private static readonly HashSet<GadgetKind> FlareGuns = new()
        {
            GadgetKind.FlareGun,
            GadgetKind.FlareGunBlue,
            GadgetKind.FlareGunGreen,
            GadgetKind.FlareGunYellow,
            GadgetKind.RainbowFlareGun,
        };

        // A clone borrowing a not-yet-registered prop's ticket ends with two props on one ticket.
        private static bool LendsNoTickets(GadgetKind kind)
        {
            return KeptInWorld.Contains(kind) || Packs.Contains(kind) || FlareGuns.Contains(kind)
                || IslandObjects.Contains(kind);
        }

        private static GadgetKind? MatchKind(string name)
        {
            foreach (var pair in PrefabNames)
            {
                if (NameMatches(name, pair.Value))
                    return pair.Key;
            }

            return null;
        }

        // "FlareGunProp" must not also match "FlareGunPropBlue".
        private static bool NameMatches(string name, string exact)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return name == exact || name.StartsWith(exact + " ", StringComparison.Ordinal);
        }

        private static bool IsOurs(string name)
        {
            return name.Contains(TemplateNameSuffix, StringComparison.Ordinal)
                || name.Contains(ReceivedItemSpawner.CosmeticNameSuffix, StringComparison.Ordinal);
        }
    }
}
