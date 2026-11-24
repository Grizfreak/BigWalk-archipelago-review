using System;
using System.Collections;
using System.Collections.Generic;

namespace BigWalkArchipelago.Core.Net
{
    // Typed view over slot_data; mirrors apworld/protocol.md, so change both together.
    internal sealed partial class ApSlotData
    {
        internal string WorldVersion { get; private set; } = "unknown";
        internal string Goal { get; private set; } = "gauntlet";

        // Empty for a seed that keeps the single `goal` above.
        internal string[] Objectives { get; private set; } = Array.Empty<string>();
        internal bool LockEndings { get; private set; }
        internal int KeysRequired { get; private set; } = 7;

        // `tower_mode`: "all_open", "in_order" or "by_item".
        internal string TowerMode { get; private set; } = "all_open";
        internal string[] TowerOrder { get; private set; } = Array.Empty<string>();
        internal bool FirstTowerOpen { get; private set; } = true;
        internal bool EveryoneGathers { get; private set; }
        internal int GreenDomeSlots { get; private set; } = 15;
        internal long MonumentItemId { get; private set; }
        internal int TowersRequired { get; private set; } = 7;

        internal string[] TowerReachedKeys { get; private set; } = Array.Empty<string>();
        internal int TowerReachedIdOffset { get; private set; } = 13_200;
        internal Dictionary<string, long[]> TowerReachHints { get; private set; } = new();
        internal int RadioStationsRequired { get; private set; } = 7;
        internal int DepositGoalAmount { get; private set; }
        internal bool RadioStationChecks { get; private set; } = true;

        // False by default: an older apworld has no Broadcast items, so the radio would be lost.
        internal bool RadioStationItems { get; private set; }

        // False by default: an older apworld has no feature items, so every door would stay shut.
        internal bool BigKeyFeatures { get; private set; }

        // False by default: an older apworld has no key items, so the keys would be unobtainable.
        internal bool BigKeyItems { get; private set; }
        internal int TotalMonumentSlots { get; private set; }
        internal int[] DepositLocationAmounts { get; private set; } = Array.Empty<int>();

        // Empty by default: an older apworld has no door items, so a held door would stay shut.
        internal string[] LockedArchDoors { get; private set; } = Array.Empty<string>();
        internal bool LinearArchDoors { get; private set; }

        internal long LocationIdBase { get; private set; } = ApLocationIds.DefaultBase;
        internal long RadioIdOffset { get; private set; } = ApLocationIds.DefaultRadioOffset;
        internal long DepositIdOffset { get; private set; } = ApLocationIds.DefaultDepositOffset;
        internal long CutIdOffset { get; private set; } = ApLocationIds.DefaultCutOffset;
        internal long KeyItemIdOffset { get; private set; } = ApLocationIds.DefaultKeyItemOffset;
        internal long GourdItemId { get; private set; } = ApLocationIds.DefaultGourdItemId;
        internal long ArchDoorIdOffset { get; private set; } = ApLocationIds.DefaultArchDoorOffset;

        // "vanilla" by default: an older apworld has no stairway items. Only "locked_stages" acts.
        internal string GauntletMode { get; private set; } = "vanilla";

        internal bool GauntletPuzzlesRequired { get; private set; } = true;

        // False by default, like every field that hides something.
        internal bool LockGauntletNeeds { get; private set; }
        internal long GauntletIdOffset { get; private set; } = ApLocationIds.DefaultGauntletOffset;

        internal string TeleportButtons { get; private set; } = "off";
        internal string[] TeleportDestinations { get; private set; } = new string[0];
        internal int TeleportIdOffset { get; private set; } = ApLocationIds.DefaultTeleportOffset;
        internal bool TeleportBackToHub { get; private set; } = true;

        // False on a seed older than 0.1.3.
        internal bool OpenBlackTower { get; private set; }

        internal string TileThief { get; private set; } = "vanilla";

        internal bool TowerResyncStations { get; private set; }
        internal bool GuestsCanResync { get; private set; }

        internal string CabinFeverMode { get; private set; } = "vanilla";
        internal int CabinFeverSeconds { get; private set; }
        internal bool CabinFeverHelp { get; private set; }
        internal string CabinFeverLongMode { get; private set; } = "vanilla";
        internal int CabinFeverLongSeconds { get; private set; }
        internal bool CabinFeverLongHelp { get; private set; }
        internal bool CabinFeverHideTimer { get; private set; }

        // The tracker board and the logic it evaluates (null without the board).
        internal bool TrackerBoard { get; private set; }
        internal bool HintBoard { get; private set; } = true;
        // "off", "in_logic" or "all".
        internal string MapTrackerMode { get; private set; } = "off";
        internal bool MapTracker => MapTrackerMode != "off";
        internal ApLogic Logic { get; private set; }

        // This slot's items and how many of each, for the hint sign's suggestions.
        internal Dictionary<string, int> HintItems { get; private set; } = new();
        internal bool CabinFeverLongHideTimer { get; private set; }

        // Off by default: an older apworld has no need items, so the objects would stay hidden.
        internal bool LockPuzzleNeeds { get; private set; }
        internal string[] PuzzleNeedKeys { get; private set; } = Array.Empty<string>();
        internal long PuzzleNeedIdOffset { get; private set; } = 11_000;
        internal string LockAbilities { get; private set; } = "disabled";
        internal string[] AbilityKeys { get; private set; } = Array.Empty<string>();
        internal long AbilityIdOffset { get; private set; } = 13_400;
        internal bool LockRunning { get; private set; }
        // The chairlift's and the train's seats stay locked until Seat arrives.
        internal bool RidesRequireSeat { get; private set; }
        // The purple puzzles are on the map only with from_start (the default, and older seeds).
        internal string IncludeBigGamePuzzles { get; private set; } = "from_start";
        // The towers whose full monument is a check, by their big key's prop, in id order.
        internal string[] MonumentFullKeys { get; private set; } = Array.Empty<string>();
        internal int MonumentFullIdOffset { get; private set; } = 13_800;
        // The towers whose monument key is a location, and the gourds each monument needs.
        internal string[] MonumentKeyKeys { get; private set; } = Array.Empty<string>();
        internal int[] MonumentKeyGourds { get; private set; } = Array.Empty<int>();
        internal int MonumentKeyIdOffset { get; private set; } = 13_850;
        internal string LightSanity { get; private set; } = "off";
        internal string[] LightKeys { get; private set; } = Array.Empty<string>();
        internal int LightIdOffset { get; private set; } = 14_000;
        internal int LightsRequiredCount { get; private set; }
        // light_sanity's lights, or the lamps without it (counted, no check).
        internal string[] LightsCounted { get; private set; } = Array.Empty<string>();
        // The gadgets each zone item keeps off the map until it arrives, by item id.
        internal bool ZoneLocks { get; private set; }
        internal Dictionary<long, string[]> ZoneLockedGadgets { get; } = new();
        // The GateLogic paths each door item opens, by item id; empty when off.
        internal Dictionary<long, string[]> TunnelDoors { get; } = new();
        internal int ObjectsRequiredCount { get; private set; }
        internal bool FastGolf { get; private set; }
        internal int BodyRequiredCount { get; private set; }

        // Empty on a seed older than 0.4.0, which has none of these items.
        internal Dictionary<long, string> EffectItems { get; private set; } = new();
        internal int TrapDuration { get; private set; } = 30;
        internal bool TrapsSpareTheGauntlet { get; private set; } = true;

        internal string DeathLink { get; private set; } = "off";
        internal string[] DeathLinkTriggers { get; private set; } = Array.Empty<string>();
        internal int DeathLinkTolerance { get; private set; }
        internal bool DeathLinkTrapOnSend { get; private set; }
        internal string DeathLinkTarget { get; private set; } = "everyone";
        internal string DeathLinkEffect { get; private set; } = "drop";
        internal Dictionary<string, int> DeathLinkRoulette { get; private set; } = new();

        // In location id order; off and empty on a seed older than 0.4.0.
        internal string PackSanity { get; private set; } = "off";
        internal string[] PickupGuids { get; private set; } = Array.Empty<string>();
        internal int PickupIdOffset { get; private set; } = 13_000;
        internal bool FireworkSanity { get; private set; }
        internal string[] FireworkKeys { get; private set; } = Array.Empty<string>();
        internal int FireworkIdOffset { get; private set; } = 13_100;

        internal bool FlareGunSanity { get; private set; }
        internal string[] FlareGunGuids { get; private set; } = Array.Empty<string>();
        internal int FlareGunIdOffset { get; private set; } = 13_050;
        internal string ObjectSanity { get; private set; } = "off";
        internal string[] ObjectGuids { get; private set; } = Array.Empty<string>();
        internal int ObjectIdOffset { get; private set; } = 13_500;
        internal string[] ObjectKinds { get; private set; } = Array.Empty<string>();
        internal int ObjectKindIdOffset { get; private set; } = 13_700;
        // Without the field (the first 0.6 builds), every kind.
        internal string[] ObjectSanityKinds { get; private set; }
        internal bool RandomColours { get; private set; }
        internal int[] ColourPalette { get; private set; } = Array.Empty<int>();
        // PegTileA1 to PegTileA9 (`peg_tiles`); empty keeps the game's.
        internal string[] PegTiles { get; private set; } = Array.Empty<string>();
        // As "<tile kind>=<drawing>".
        internal string[] PegDrawings { get; private set; } = Array.Empty<string>();

        // "Gourd" on older seeds.
        internal string GourdName { get; private set; } = GourdNames.Default;

        internal bool TrapLink { get; private set; }

        internal bool SendsDeathLink => DeathLink is "send" or "both";
        internal bool ReceivesDeathLink => DeathLink is "receive" or "both";

        internal string Describe()
        {
            return $"apworld v{WorldVersion}, goal={(Objectives.Length > 0 ? "[" + string.Join(", ", Objectives) + "]" : Goal)}"
                   + (Goal == "deposits" ? $" ({DepositGoalAmount} deposits)" : string.Empty)
                   + $", {TotalMonumentSlots} monument slots"
                   + $", {DepositLocationAmounts.Length} deposit check(s)"
                   + $", radio checks {(RadioStationChecks ? "on" : "off")}"
                   + $", radio items {(RadioStationItems ? "on" : "off")}"
                   + $", big key features {(BigKeyFeatures ? "on" : "off")}"
                   + $", big keys as items {(BigKeyItems ? "on" : "off")}"
                   + $", {LockedArchDoors.Length} arch door(s) locked"
                   + $", puzzle needs {(LockPuzzleNeeds ? "locked" : "free")}"
                   + $", gauntlet {GauntletMode}{(GauntletMode == "locked_stages" && !GauntletPuzzlesRequired ? " (puzzles optional)" : string.Empty)}"
                   + $", teleport buttons {TeleportButtons}"
                   + $", black tower {(OpenBlackTower ? "open" : "vanilla")}"
                   + $", cabin fever {CabinFeverMode} {CabinFeverSeconds}s{(CabinFeverHelp ? " +help" : string.Empty)}{(CabinFeverHideTimer ? " +hidden" : string.Empty)}"
                   + $", cabin fever long {CabinFeverLongMode} {CabinFeverLongSeconds}s{(CabinFeverLongHelp ? " +help" : string.Empty)}{(CabinFeverLongHideTimer ? " +hidden" : string.Empty)}"
                   + $", packs {PackSanity}, fireworks {(FireworkSanity ? FireworkKeys.Length.ToString() : "off")}"
                   + $", flare guns {(FlareGunSanity ? FlareGunGuids.Length.ToString() : "off")}, colours {(RandomColours ? ColourPalette.Length.ToString() : "off")}"
                   + $", {EffectItems.Count} trap/bonus item(s), {TrapDuration}s"
                   + $", deathlink {DeathLink}"
                   + (DeathLink != "off" ? $" [{string.Join(",", DeathLinkTriggers)}] tolerance {DeathLinkTolerance}, {DeathLinkEffect}" : string.Empty);
        }
    }
}
