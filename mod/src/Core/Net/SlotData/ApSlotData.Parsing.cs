using System;
using System.Collections;
using System.Collections.Generic;

namespace BigWalkArchipelago.Core.Net
{
    internal sealed partial class ApSlotData
    {
        internal static ApSlotData From(Dictionary<string, object> raw)
        {
            var data = new ApSlotData();
            if (raw == null)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ApSlotData)}] No slot_data received; falling back to built-in defaults.");
                return data;
            }

            data.WorldVersion = GetString(raw, "world_version", data.WorldVersion);
            data.Goal = GetString(raw, "goal", data.Goal);
            data.Objectives = GetStringArray(raw, "objectives");
            data.LockEndings = GetBool(raw, "lock_endings", false);
            data.KeysRequired = GetInt(raw, "keys_required", data.KeysRequired);
            data.TowerMode = GetString(raw, "tower_mode", data.TowerMode);
            data.TowerOrder = GetStringArray(raw, "tower_order");
            data.FirstTowerOpen = GetBool(raw, "first_tower_open", true);
            data.EveryoneGathers = GetBool(raw, "everyone_gathers", false);
            data.GreenDomeSlots = GetInt(raw, "green_dome_slots", 15);
            data.MonumentItemId = GetInt(raw, "monument_item_id", 0);
            data.TowersRequired = GetInt(raw, "towers_required", data.TowersRequired);
            data.TowerReachedKeys = GetStringArray(raw, "tower_reached_keys");
            data.TowerReachedIdOffset = GetInt(raw, "tower_reached_id_offset", data.TowerReachedIdOffset);
            foreach (var pair in GetMap(raw, "tower_reach_hints"))
            {
                try
                {
                    var ids = new List<long>();
                    foreach (var id in Newtonsoft.Json.Linq.JArray.Parse(pair.Value.Text))
                        ids.Add(id.ToObject<long>());
                    data.TowerReachHints[pair.Key] = ids.ToArray();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(ApSlotData)}] slot_data['tower_reach_hints'][{pair.Key}] is unreadable: {ex.Message}");
                }
            }
            data.RadioStationsRequired = GetInt(raw, "radio_stations_required", data.RadioStationsRequired);
            data.DepositGoalAmount = GetInt(raw, "deposit_goal_amount", data.DepositGoalAmount);
            data.RadioStationChecks = GetBool(raw, "radio_station_checks", data.RadioStationChecks);
            data.RadioStationItems = GetBool(raw, "radio_station_items", data.RadioStationItems);
            data.BigKeyFeatures = GetBool(raw, "big_key_features", data.BigKeyFeatures);
            data.BigKeyItems = GetBool(raw, "big_key_items", data.BigKeyItems);
            data.TotalMonumentSlots = GetInt(raw, "total_monument_slots", data.TotalMonumentSlots);
            data.DepositLocationAmounts = GetIntArray(raw, "deposit_location_amounts");
            data.LockedArchDoors = GetStringArray(raw, "locked_arch_doors");
            data.LinearArchDoors = GetBool(raw, "linear_arch_doors", false);
            data.LockPuzzleNeeds = GetBool(raw, "lock_puzzle_needs", data.LockPuzzleNeeds);
            data.PuzzleNeedKeys = GetStringArray(raw, "puzzle_need_keys");
            data.LockAbilities = GetString(raw, "lock_abilities", data.LockAbilities);
            data.AbilityKeys = GetStringArray(raw, "ability_keys");
            data.AbilityIdOffset = GetInt(raw, "ability_id_offset", (int)data.AbilityIdOffset);
            data.LockRunning = GetBool(raw, "lock_running", data.LockRunning);
            data.RidesRequireSeat = GetBool(raw, "rides_require_seat", data.RidesRequireSeat);
            data.IncludeBigGamePuzzles = GetString(raw, "include_big_game_puzzles", data.IncludeBigGamePuzzles);
            data.MonumentFullKeys = GetStringArray(raw, "monument_full_keys");
            data.MonumentFullIdOffset = GetInt(raw, "monument_full_id_offset", data.MonumentFullIdOffset);
            data.MonumentKeyKeys = GetStringArray(raw, "monument_key_keys");
            // Not sorted: it follows monument_key_keys.
            data.MonumentKeyGourds = GetIntArray(raw, "monument_key_gourds", sorted: false);
            data.MonumentKeyIdOffset = GetInt(raw, "monument_key_id_offset", data.MonumentKeyIdOffset);
            data.LightSanity = GetString(raw, "light_sanity", data.LightSanity);
            data.LightKeys = GetStringArray(raw, "light_keys");
            data.LightIdOffset = GetInt(raw, "light_id_offset", data.LightIdOffset);
            data.LightsRequiredCount = GetInt(raw, "lights_required_count", 0);
            data.LightsCounted = GetStringArray(raw, "lights_counted");
            data.ZoneLocks = GetBool(raw, "zone_locks", false);
            foreach (var pair in GetMap(raw, "zone_locked_gadgets"))
            {
                try
                {
                    var guids = new List<string>();
                    foreach (var guid in Newtonsoft.Json.Linq.JArray.Parse(pair.Value.Text))
                        guids.Add(guid.ToObject<string>());
                    data.ZoneLockedGadgets[long.Parse(pair.Key)] = guids.ToArray();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(ApSlotData)}] slot_data['zone_locked_gadgets'][{pair.Key}] is unreadable: {ex.Message}");
                }
            }
            foreach (var pair in GetMap(raw, "tunnel_doors"))
            {
                try
                {
                    var paths = new List<string>();
                    foreach (var path in Newtonsoft.Json.Linq.JArray.Parse(pair.Value.Text))
                        paths.Add(path.ToObject<string>());
                    data.TunnelDoors[long.Parse(pair.Key)] = paths.ToArray();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(ApSlotData)}] slot_data['tunnel_doors'][{pair.Key}] is unreadable: {ex.Message}");
                }
            }
            data.ObjectsRequiredCount = GetInt(raw, "objects_required_count", 0);
            data.FastGolf = GetBool(raw, "fast_golf", false);
            data.BodyRequiredCount = GetInt(raw, "body_required_count", 0);
            data.GauntletMode = GetString(raw, "gauntlet_mode", data.GauntletMode);
            data.GauntletPuzzlesRequired = GetBool(raw, "gauntlet_puzzles_required", data.GauntletPuzzlesRequired);
            data.LockGauntletNeeds = GetBool(raw, "lock_gauntlet_needs", data.LockGauntletNeeds);

            data.LocationIdBase = GetInt(raw, "location_id_base", (int)data.LocationIdBase);
            data.RadioIdOffset = GetInt(raw, "radio_id_offset", (int)data.RadioIdOffset);
            data.DepositIdOffset = GetInt(raw, "deposit_id_offset", (int)data.DepositIdOffset);
            data.CutIdOffset = GetInt(raw, "cut_id_offset", (int)data.CutIdOffset);
            data.KeyItemIdOffset = GetInt(raw, "key_item_id_offset", (int)data.KeyItemIdOffset);
            data.GourdItemId = GetInt(raw, "gourd_item_id", (int)data.GourdItemId);
            data.ArchDoorIdOffset = GetInt(raw, "arch_door_id_offset", (int)data.ArchDoorIdOffset);
            data.PuzzleNeedIdOffset = GetInt(raw, "puzzle_need_id_offset", (int)data.PuzzleNeedIdOffset);
            data.GauntletIdOffset = GetInt(raw, "gauntlet_id_offset", (int)data.GauntletIdOffset);
            data.TeleportButtons = GetString(raw, "teleport_buttons", data.TeleportButtons);
            data.TeleportDestinations = GetStringArray(raw, "teleport_destinations");
            data.TeleportIdOffset = GetInt(raw, "teleport_id_offset", data.TeleportIdOffset);
            data.TeleportBackToHub = GetBool(raw, "teleport_back_to_hub", true);
            data.OpenBlackTower = GetBool(raw, "open_black_tower", false);
            data.TileThief = GetString(raw, "tile_thief", data.TileThief);
            data.TowerResyncStations = GetBool(raw, "tower_resync_stations", false);
            data.GuestsCanResync = GetBool(raw, "guests_can_resync", false);
            data.CabinFeverMode = GetString(raw, "cabin_fever_mode", data.CabinFeverMode);
            data.CabinFeverSeconds = GetInt(raw, "cabin_fever_seconds", 0);
            data.CabinFeverHelp = GetBool(raw, "cabin_fever_help", false);
            data.CabinFeverLongMode = GetString(raw, "cabin_fever_long_mode", data.CabinFeverLongMode);
            data.CabinFeverLongSeconds = GetInt(raw, "cabin_fever_long_seconds", 0);
            data.CabinFeverLongHelp = GetBool(raw, "cabin_fever_long_help", false);
            data.CabinFeverHideTimer = GetBool(raw, "cabin_fever_hide_timer", false);
            data.TrackerBoard = GetBool(raw, "tracker_board", false);
            data.HintBoard = GetBool(raw, "hint_board", true);
            data.MapTrackerMode = GetString(raw, "map_tracker", "off");
            foreach (var pair in GetMap(raw, "hint_items"))
                data.HintItems[pair.Key] = pair.Value.Number;
            if ((data.TrackerBoard || data.MapTracker) && raw.TryGetValue("logic", out var logic) && logic is Newtonsoft.Json.Linq.JObject logicObject)
                data.Logic = ApLogic.From(logicObject);
            data.CabinFeverLongHideTimer = GetBool(raw, "cabin_fever_long_hide_timer", false);

            foreach (var pair in GetMap(raw, "effect_items"))
            {
                // A JSON key is always a string.
                if (long.TryParse(pair.Key, out var offset))
                    data.EffectItems[offset] = pair.Value.Text;
            }

            data.PackSanity = GetString(raw, "pack_sanity", "off");
            data.PickupGuids = GetStringArray(raw, "pickup_guids");
            data.PickupIdOffset = GetInt(raw, "pickup_id_offset", data.PickupIdOffset);
            data.FireworkSanity = GetBool(raw, "firework_sanity", false);
            data.FireworkKeys = GetStringArray(raw, "firework_keys");
            data.FireworkIdOffset = GetInt(raw, "firework_id_offset", data.FireworkIdOffset);
            data.FlareGunSanity = GetBool(raw, "flare_gun_sanity", false);
            data.FlareGunGuids = GetStringArray(raw, "flare_gun_guids");
            data.FlareGunIdOffset = GetInt(raw, "flare_gun_id_offset", data.FlareGunIdOffset);
            data.ObjectSanity = GetString(raw, "object_sanity", data.ObjectSanity);
            data.ObjectGuids = GetStringArray(raw, "object_guids");
            data.ObjectIdOffset = GetInt(raw, "object_id_offset", data.ObjectIdOffset);
            data.ObjectKinds = GetStringArray(raw, "object_kinds");
            data.ObjectKindIdOffset = GetInt(raw, "object_kind_id_offset", data.ObjectKindIdOffset);
            data.ObjectSanityKinds = raw.ContainsKey("object_sanity_kinds") ? GetStringArray(raw, "object_sanity_kinds") : null;
            // Spelt "random_colours" and "colour_palette" in the first 0.4 builds.
            data.RandomColours = GetBool(raw, "random_colors", GetBool(raw, "random_colours", false));
            // In drawn order: the first four paint the flare guns.
            data.ColourPalette = raw.ContainsKey("color_palette")
                ? GetIntArray(raw, "color_palette", sorted: false)
                : GetIntArray(raw, "colour_palette", sorted: false);
            data.PegTiles = GetStringArray(raw, "peg_tiles");
            data.PegDrawings = GetStringArray(raw, "peg_drawings");
            data.GourdName = GetString(raw, "gourd_name", data.GourdName);
            data.TrapLink = GetBool(raw, "trap_link", false);

            data.TrapDuration = GetInt(raw, "trap_duration", data.TrapDuration);
            data.TrapsSpareTheGauntlet = GetBool(raw, "traps_spare_the_gauntlet", data.TrapsSpareTheGauntlet);
            data.DeathLink = GetString(raw, "death_link", data.DeathLink);
            data.DeathLinkTriggers = GetStringArray(raw, "death_link_triggers");
            // Was "death_link_tolerance" before it took the usual name.
            data.DeathLinkTolerance = GetInt(raw, "death_link_amnesty", GetInt(raw, "death_link_tolerance", 0));
            data.DeathLinkTrapOnSend = GetBool(raw, "death_link_trap_on_send", false);
            data.DeathLinkTarget = GetString(raw, "death_link_target", data.DeathLinkTarget);
            data.DeathLinkEffect = GetString(raw, "death_link_effect", data.DeathLinkEffect);
            foreach (var pair in GetMap(raw, "death_link_roulette"))
            {
                if (pair.Value.Number > 0)
                    data.DeathLinkRoulette[pair.Key] = pair.Value.Number;
            }

            return data;
        }

        private static string GetString(Dictionary<string, object> raw, string key, string fallback)
        {
            return raw.TryGetValue(key, out var value) && value != null ? value.ToString() : fallback;
        }

        private static int GetInt(Dictionary<string, object> raw, string key, int fallback)
        {
            if (!raw.TryGetValue(key, out var value) || value == null)
                return fallback;

            try
            {
                return Convert.ToInt32(value);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApSlotData)}] slot_data['{key}'] is not a number ({ex.Message}); using {fallback}.");
                return fallback;
            }
        }

        private static bool GetBool(Dictionary<string, object> raw, string key, bool fallback)
        {
            if (!raw.TryGetValue(key, out var value) || value == null)
                return fallback;

            try
            {
                return Convert.ToBoolean(value);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApSlotData)}] slot_data['{key}'] is not a boolean ({ex.Message}); using {fallback}.");
                return fallback;
            }
        }

        private static string[] GetStringArray(Dictionary<string, object> raw, string key)
        {
            if (!raw.TryGetValue(key, out var value) || value is not IEnumerable sequence || value is string)
                return Array.Empty<string>();

            var result = new List<string>();
            foreach (var element in sequence)
            {
                if (element != null)
                    result.Add(element.ToString());
            }

            return result.ToArray();
        }

        internal struct MapValue
        {
            internal string Text;
            internal int Number;
        }

        // Each value as text, and as a number when it is one (0 otherwise).
        private static List<KeyValuePair<string, MapValue>> GetMap(Dictionary<string, object> raw, string key)
        {
            var result = new List<KeyValuePair<string, MapValue>>();
            if (!raw.TryGetValue(key, out var value) || value is not Newtonsoft.Json.Linq.JObject map)
                return result;

            foreach (var property in map.Properties())
            {
                var entry = new MapValue { Text = property.Value?.ToString() ?? string.Empty };
                if (property.Value != null && property.Value.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
                    entry.Number = property.Value.ToObject<int>();
                result.Add(new KeyValuePair<string, MapValue>(property.Name, entry));
            }

            return result;
        }

        private static int[] GetIntArray(Dictionary<string, object> raw, string key, bool sorted = true)
        {
            if (!raw.TryGetValue(key, out var value) || value is not IEnumerable sequence || value is string)
                return Array.Empty<int>();

            var result = new List<int>();
            foreach (var element in sequence)
            {
                try
                {
                    result.Add(Convert.ToInt32(element));
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(ApSlotData)}] slot_data['{key}'] holds a non-numeric entry, skipped ({ex.Message}).");
                }
            }

            if (sorted)
                result.Sort();
            return result.ToArray();
        }
    }
}
