namespace BigWalkArchipelago.Core.Net
{
    // Maps game identifiers to Archipelago ids by the same arithmetic as the apworld.
    internal static class ApLocationIds
    {
        // Frozen: a published id never changes.
        internal const long Base = 8_600_000;
        internal const long GourdItemId = Base + 1;
        private const long RadioOffset = 1_000;
        private const long DepositOffset = 2_000;
        private const long GauntletOffset = 2_500;
        internal const int TeleportOffset = 2_700;
        private const long CutOffset = 3_000;

        // Key items (the physical key) sit apart from feature items (the door it opens).
        private const long KeyItemOffset = 4_000;
        private const long ArchDoorOffset = 5_000;
        internal const long PuzzleNeedOffset = 11_000;
        internal const long AbilityOffset = 13_400;

        // A cut has no game identifier: key + separator + index, a character no enum name contains.
        private const string CutSeparator = "#cut";

        // Room for ten segments per key, twice the widest one.
        private const long CutStride = 10;

        internal static void Configure(ApSlotData slotData)
        {
            if (slotData == null)
                return;

            _pickupGuids = slotData.PickupGuids;
            _fireworkKeys = slotData.FireworkKeys;
            _flareGunGuids = slotData.FlareGunGuids;
            _towerKeys = slotData.TowerReachedKeys;
            _objectGuids = slotData.ObjectGuids;
            _objectKinds = slotData.ObjectKinds;
            _lightKeys = slotData.LightKeys;
            _monumentKeys = slotData.MonumentFullKeys;
            _monumentKeyKeys = slotData.MonumentKeyKeys;
        }

        // Packs and fireworks: their id is the position in the slot's lists.
        internal const string PickupPrefix = "pickup:";
        internal const string FireworkPrefix = "firework:";
        private static string[] _pickupGuids = Array.Empty<string>();
        private const long PickupOffset = 13_000;

        internal static bool IsPickupLocation(long id) =>
            id >= Base + PickupOffset && id < Base + PickupOffset + _pickupGuids.Length;
        private static string[] _fireworkKeys = Array.Empty<string>();
        private const long FireworkOffset = 13_100;
        internal const string TowerPrefix = "tower:";
        private static string[] _towerKeys = Array.Empty<string>();
        private const long TowerOffset = 13_200;
        internal const string FlareGunPrefix = "flaregun:";
        private static string[] _flareGunGuids = Array.Empty<string>();
        private const long FlareGunOffset = 13_050;
        internal const string ObjectPrefix = "object:";
        private static string[] _objectGuids = Array.Empty<string>();
        private const long ObjectOffset = 13_500;
        internal const string ObjectKindPrefix = "objectkind:";
        private static string[] _objectKinds = Array.Empty<string>();
        private const long ObjectKindOffset = 13_700;
        // A monument's id is the tower's position in the apworld's TOWERS, not in the slot's list.
        internal const string LightPrefix = "light:";
        private static string[] _lightKeys = Array.Empty<string>();
        private const long LightOffset = 14_000;

        internal const string MonumentPrefix = "monument:";
        private static string[] _monumentKeys = Array.Empty<string>();
        private const long MonumentOffset = 13_800;

        internal const string MonumentKeyPrefix = "monumentkey:";
        private static string[] _monumentKeyKeys = Array.Empty<string>();
        private const long MonumentKeyOffset = 13_850;

        private static bool TryResolveMonument(string locationName, out long locationId) =>
            TryResolveByTower(locationName, MonumentPrefix, _monumentKeys, MonumentOffset, out locationId)
            || TryResolveByTower(locationName, MonumentKeyPrefix, _monumentKeyKeys, MonumentKeyOffset, out locationId);

        private static bool TryResolveByTower(string locationName, string prefix, string[] listed, long offset, out long locationId)
        {
            locationId = 0;
            if (!locationName.StartsWith(prefix, StringComparison.Ordinal))
                return false;
            var prop = locationName.Substring(prefix.Length);
            var position = MonumentOrder.TowerIndexOf(prop);
            if (position < 0 || Array.IndexOf(listed, prop) < 0)
                return false;
            locationId = Base + offset + position;
            return true;
        }

        private static bool TryResolveListed(string locationName, string prefix, string[] list, long offset, out long locationId)
        {
            locationId = 0;
            if (!locationName.StartsWith(prefix, StringComparison.Ordinal))
                return false;

            var position = Array.IndexOf(list, locationName.Substring(prefix.Length));
            if (position < 0)
                return false;

            locationId = Base + offset + position;
            return true;
        }

        internal static bool TryResolveLocation(string locationName, out long locationId)
        {
            if (Enum.TryParse<SaveablePropName>(locationName, out var propName)
                && propName != SaveablePropName.notSavable)
            {
                locationId = Base + (long)propName;
                return true;
            }

            if (Enum.TryParse<SavableSystem>(locationName, out var system)
                && RadioStations.IsRealStation(system))
            {
                locationId = Base + RadioOffset + (long)system;
                return true;
            }

            if (Enum.TryParse<SavableSystem>(locationName, out var chamber)
                && GauntletStairways.IsChamber(chamber))
            {
                locationId = Base + GauntletOffset + (long)chamber;
                return true;
            }

            if (TryResolveCut(locationName, out locationId))
                return true;

            if (TryResolveListed(locationName, PickupPrefix, _pickupGuids, PickupOffset, out locationId)
                || TryResolveListed(locationName, FireworkPrefix, _fireworkKeys, FireworkOffset, out locationId)
                || TryResolveListed(locationName, FlareGunPrefix, _flareGunGuids, FlareGunOffset, out locationId)
                || TryResolveListed(locationName, TowerPrefix, _towerKeys, TowerOffset, out locationId)
                || TryResolveListed(locationName, ObjectPrefix, _objectGuids, ObjectOffset, out locationId)
                || TryResolveListed(locationName, ObjectKindPrefix, _objectKinds, ObjectKindOffset, out locationId)
                || TryResolveMonument(locationName, out locationId)
                || TryResolveListed(locationName, LightPrefix, _lightKeys, LightOffset, out locationId))
                return true;

            locationId = 0;
            return false;
        }

        // Keep in step with the parsing: CheckTracker replays these names from the save.
        internal static string CutLocationName(SaveablePropName propName, int index)
        {
            return $"{propName}{CutSeparator}{index}";
        }

        private static bool TryResolveCut(string locationName, out long locationId)
        {
            locationId = 0;

            var separator = locationName.IndexOf(CutSeparator, StringComparison.Ordinal);
            if (separator <= 0)
                return false;

            if (!int.TryParse(locationName.Substring(separator + CutSeparator.Length), out var index)
                || index < 0 || index >= CutStride)
                return false;

            if (!Enum.TryParse<SaveablePropName>(locationName.Substring(0, separator), out var propName)
                || !GourdRegistry.IsBigKey(propName))
                return false;

            locationId = Base + CutOffset + (long)propName * CutStride + index;
            return true;
        }

        internal static long DepositLocationId(int depositedCount)
        {
            return Base + DepositOffset + depositedCount;
        }

        // Item and location ids are separate namespaces: a Broadcast item reuses the station's id.
        internal static bool TryResolveRadioItem(long itemId, out SavableSystem system)
        {
            var value = itemId - Base - RadioOffset;
            system = SavableSystem.NotSavable;

            if (value < 0 || value > int.MaxValue)
                return false;

            if (!Enum.IsDefined(typeof(SavableSystem), (int)value))
                return false;

            var candidate = (SavableSystem)(int)value;
            if (!RadioStations.IsRealStation(candidate))
                return false;

            system = candidate;
            return true;
        }

        internal static bool TryResolveArchDoorItem(long itemId, out SavableSystem system)
        {
            var value = itemId - Base - ArchDoorOffset;
            system = SavableSystem.NotSavable;

            if (value < 0 || value > int.MaxValue || !Enum.IsDefined(typeof(SavableSystem), (int)value))
                return false;

            var candidate = (SavableSystem)(int)value;
            if (!ArchDoors.IsArchDoor(candidate))
                return false;

            system = candidate;
            return true;
        }

        // Returns the stage level 0..6, its position in GauntletStairways.Chambers.
        private const long ProgressiveGauntletValue = 60;

        internal static bool TryResolveGauntletItem(long itemId, out int level)
        {
            level = -1;
            var value = itemId - Base - GauntletOffset;
            if (value == ProgressiveGauntletValue)
            {
                level = GauntletStairways.LowestShut();
                return level >= 0;
            }

            if (value < 0 || value > int.MaxValue || !Enum.IsDefined(typeof(SavableSystem), (int)value))
                return false;

            level = Array.IndexOf(GauntletStairways.Chambers, (SavableSystem)(int)value);
            return level >= 0;
        }

        // Separate from TryResolveBigKeyItem on purpose: mixing the two would fail silently.
        internal static bool TryResolveKeyItem(long itemId, out SaveablePropName propName)
        {
            return TryResolveProp(itemId - Base - KeyItemOffset, out propName);
        }

        internal static bool TryResolveBigKeyItem(long itemId, out SaveablePropName propName)
        {
            return TryResolveProp(itemId - Base, out propName);
        }

        // Flat B + 9001..9005, in data.py's FILLER_ITEMS order, which this must match.
        private const long GadgetItemBaseOffset = 9_001;

        private static readonly GadgetKind[] GadgetItemOrder =
        {
            GadgetKind.Megaphone,
            GadgetKind.WalkieTalkie,
            GadgetKind.Backpack,
            GadgetKind.Belt,
            GadgetKind.FlareGun,
            GadgetKind.Laser,
            GadgetKind.Binoculars,
            GadgetKind.Compass,
            GadgetKind.FoldingMap,
            GadgetKind.Radio,
            GadgetKind.GourdCarton,
            GadgetKind.Torch,
            GadgetKind.Lamp,
            GadgetKind.XrayGoggles,
            GadgetKind.FlareGunBlue,
            GadgetKind.FlareGunGreen,
            GadgetKind.FlareGunYellow,
            GadgetKind.RainbowFlareGun,
        };

        internal static bool TryResolveGadgetItem(long itemId, out GadgetKind kind)
        {
            kind = default;

            var value = itemId - Base - GadgetItemBaseOffset;
            if (value < 0 || value >= GadgetItemOrder.Length)
                return false;

            kind = GadgetItemOrder[(int)value];
            return true;
        }

        private static bool TryResolveProp(long value, out SaveablePropName propName)
        {
            propName = SaveablePropName.notSavable;

            if (value < 0 || value > int.MaxValue)
                return false;

            if (!Enum.IsDefined(typeof(SaveablePropName), (int)value))
                return false;

            var candidate = (SaveablePropName)(int)value;
            if (!GourdRegistry.IsBigKey(candidate))
                return false;

            propName = candidate;
            return true;
        }
    }
}
