"""The Big Walk Archipelago world."""

from __future__ import annotations

from collections.abc import Mapping
from typing import Any, TextIO

from BaseClasses import Entrance, Item, Location
from rule_builder.rules import Rule
from worlds.AutoWorld import World

from . import (checks, data, fill, generation, items, locations, logic_export, monuments, regions, rules, tiles,
               web_world)
from . import options as bigwalk_options

WORLD_VERSION = "1.0.15"
"""Kept in step with archipelago.json; sent in slot_data so the mod can check it."""

TRACKER_OPTIONS = {
    "goal": "goal",
    "objectives": "objectives",
    "lock_endings_until_objectives": "lock_endings_until_objectives",
    "keys_required": "keys_required",
    "tower_sanity": "tower_sanity",
    "tower_mode": "tower_mode",
    "first_tower_open": "first_tower_open",
    "everyone_gathers": "everyone_gathers",
    "towers_required": "towers_required",
    "radio_stations_required": "radio_stations_required",
    "deposit_goal_amount": "gourds_required",
    "deposit_locations": "gourd_sanity",
    "radio_station_checks": "radio_sanity",
    "radio_station_items": "shuffle_radio_music",
    "start_with_arch_doors_open": "start_with_arch_doors_open",
    "require_arch_doors": "require_arch_doors",
    "gauntlet_mode": "gauntlet_mode",
    "gauntlet_puzzles_required": "gauntlet_puzzles_required",
    "lock_gauntlet_needs": "lock_gauntlet_needs",
    "gauntlet_stage_items": "gauntlet_stage_items",
    "teleport_buttons": "teleport_buttons",
    "lock_puzzle_needs": "lock_puzzle_needs",
    "start_with_puzzle_needs": "start_with_puzzle_needs",
    "start_with_random_puzzle_need": "start_with_random_puzzle_need",
    "lock_abilities": "lock_abilities",
    "start_with_abilities": "start_with_abilities",
    "include_big_game_puzzles": "include_big_game_puzzles",
    "monument_sanity": "monument_sanity",
    "linear_towers": "linear_towers",
    "cut_key_sanity": "cut_key_sanity",
    "lock_running": "lock_running",
    "rides_require_seat": "rides_require_seat",
    "pack_sanity": "pack_sanity",
    "firework_sanity": "firework_sanity",
    "flare_gun_sanity": "flare_gun_sanity",
    "object_sanity": "object_sanity",
    "object_sanity_kind_keys": "object_sanity_kinds",
    "light_sanity": "light_sanity",
    "zone_locks": "zone_locks",
    "tunnel_sanity": "tunnel_sanity",
    "fast_golf": "fast_golf",
    "lights_required": "lights_required",
    "objects_required": "objects_required",
    "body_required": "body_required",
    "random_colors": "random_colors",
    "trap_link": "trap_link",
    # The vanilla options move items into locations, so the tracker needs them to rebuild the seed.
    "linear_keys": "linear_keys",
    "linear_puzzles": "linear_puzzles",
    "linear_arch_doors": "linear_arch_doors",
    "start_with_drawbridge_open": "start_with_drawbridge_open",
    "start_with_map_room_open": "start_with_map_room_open",
}
"""slot_data field -> option: everything a re-generation needs to rebuild the same world."""

# Options sent to the mod under their own name: toggles as bools, sets as sorted lists.
SLOT_OPTIONS = (
    "lock_endings_until_objectives", "keys_required", "first_tower_open", "everyone_gathers", "towers_required",
    "tower_sanity", "radio_stations_required", "start_with_arch_doors_open", "require_arch_doors",
    "gauntlet_puzzles_required", "lock_gauntlet_needs", "teleport_back_to_hub", "open_black_tower",
    "tower_resync_stations", "tracker_board", "hint_board", "firework_sanity",
    "lights_required", "zone_locks", "tunnel_sanity", "fast_golf", "objects_required", "body_required",
    "random_colors", "trap_link", "trap_duration", "traps_spare_the_gauntlet", "death_link_triggers",
    "death_link_amnesty", "death_link_trap_on_send", "lock_puzzle_needs", "start_with_puzzle_needs",
    "start_with_random_puzzle_need", "rides_require_seat", "start_with_abilities", "linear_keys",
    "linear_arch_doors", "linear_puzzles", "start_with_drawbridge_open", "start_with_map_room_open",
    "monument_sanity",
)

# Choices sent to the mod as their option key.
SLOT_CHOICES = (
    "tower_mode", "gauntlet_mode", "gauntlet_stage_items", "teleport_buttons", "tile_thief", "map_tracker",
    "object_sanity", "light_sanity", "death_link", "death_link_target", "death_link_effect", "lock_abilities",
    "include_big_game_puzzles", "cut_key_sanity", "linear_towers",
)


class BigWalkWorld(World):
    """
    Big Walk is a co-op walking game: two or more players wander a large
    open island, solve puzzles together for gourds, carry those gourds to the
    towers' monuments, and unlock the big keys that open up the map.
    """

    game = "Big Walk"
    # Empty for a world that never runs generate_early.
    objectives: frozenset[str] = frozenset()

    web = web_world.BigWalkWebWorld()

    options_dataclass = bigwalk_options.BigWalkOptions
    options: bigwalk_options.BigWalkOptions

    item_name_to_id = items.ITEM_NAME_TO_ID
    location_name_to_id = locations.LOCATION_NAME_TO_ID

    item_name_groups = items.ITEM_NAME_GROUPS
    location_name_groups = locations.LOCATION_NAME_GROUPS

    origin_region_name = regions.STARTING_ZONE
    topology_present = True

    # The mod needs slot_data and the modern data package.
    required_client_version = (0, 6, 0)

    # Universal Tracker rebuilds the world from slot_data (TRACKER_OPTIONS), not from a YAML.
    ut_can_gen_without_yaml = True

    towers: tuple[data.Tower, ...]
    """Towers in play this slot: always all seven."""

    gourd_count: int
    """`Gourd` items in the pool; exactly enough to fill every monument."""

    deposit_amounts: tuple[int, ...]
    """Deposit counts that are locations, e.g. (5, 10, ..., 45)."""

    deposit_goal: int
    """Gourds required to win when the goal is `big_collection`."""

    tower_order: tuple[str, ...]
    """Prop names in the order the monuments open; empty when all are open."""

    monument_key_gourds: dict[str, int]
    """Which gourd of its monument frees a tower's key, by prop name."""

    color_palette: list[int]
    """Indices into the mod's palette; empty keeps the game's own colors."""

    peg_tiles: tuple[str, ...]
    """The drawing each basic tile shows."""

    peg_drawings: dict[str, str]
    """The other drawings replaced, by place."""

    cabin_fever: dict[str, dict[str, object]]
    """The two Cabin Fever waits, by puzzle."""

    locked_arch_doors: tuple[data.ArchDoor, ...]
    """Arch doors held closed until their item arrives."""

    gourd_name: str = data.GOURD_ITEM_NAME
    """Display name of the gourds, shared by every Big Walk slot."""

    local_placements: list[tuple[list[str], list[Item]]]
    """Checks of the `local` options, and the items pre_fill places on them."""

    way_out: tuple[str, ...] = ()
    """Items asked for early as the way out of the starting zone."""

    @staticmethod
    def interpret_slot_data(slot_data: Mapping[str, Any]) -> Mapping[str, Any]:
        """Non-None asks Universal Tracker to re-generate with it in `re_gen_passthrough`."""
        return slot_data

    def write_spoiler_header(self, spoiler_handle: TextIO) -> None:
        if self.gourd_name != data.GOURD_ITEM_NAME:
            spoiler_handle.write(f"{'Gourds shown in game as:':33}{self.gourd_name}\n")
        if self.options.random_objectives.value:
            spoiler_handle.write(f"{'Objectives drawn:':33}{', '.join(checks.objective_keys(self))}\n")
        if self.peg_tiles != tiles.VANILLA_TILES:
            spoiler_handle.write(f"{'Basic tiles shown as:':33}{', '.join(self.peg_tiles)}\n")
        mirrors = {place: name for place, name in self.peg_drawings.items() if place in tiles.POSE_PLACES}
        if mirrors:
            spoiler_handle.write(f"{'Mirrored poses:':33}"
                                 f"{', '.join(f'{place}={name}' for place, name in sorted(mirrors.items()))}\n")

    def _take_options_from_tracker(self) -> None:
        """While Universal Tracker re-generates, use the options the seed was generated from."""
        passthrough = getattr(self.multiworld, "re_gen_passthrough", None)
        slot_data = passthrough.get(self.game) if passthrough else None
        if not slot_data:
            return

        for field, name in TRACKER_OPTIONS.items():
            if field in slot_data:
                option = getattr(self.options, name)
                setattr(self.options, name, option.from_any(slot_data[field]))

        # Older seeds without the field had one item per stage.
        if "gauntlet_stage_items" not in slot_data:
            self.options.gauntlet_stage_items = bigwalk_options.GauntletStageItems(
                bigwalk_options.GauntletStageItems.option_individual)

    def generate_early(self) -> None:
        self._take_options_from_tracker()
        generation.generate_early(self)

    def create_regions(self) -> None:
        regions.create_and_connect_regions(self)
        locations.create_all_locations(self)

    def create_items(self) -> None:
        items.create_all_items(self)
        # Before set_rules: the start can only be given items while the pool may still change.
        rules.set_all_rules(self)
        fill.make_room_at_the_start(self)
        # `lock_running: early`: the fill places it in sphere 1, in any world.
        early = self.options.lock_running == bigwalk_options.LockRunning.option_early
        if early and any(item.player == self.player and item.name == data.RUNNING_ITEM_NAME
                         for item in self.multiworld.itempool):
            self.multiworld.early_items[self.player][data.RUNNING_ITEM_NAME] = 1

    def pre_fill(self) -> None:
        fill.place_local_items(self)

    def fill_hook(self, progitempool: list[Item], usefulitempool: list[Item], filleritempool: list[Item],
                  fill_locations: list[Location]) -> None:
        # Local items whose check plando took go through the main fill.
        for item in fill.local_items_left(self):
            (usefulitempool if item.useful else filleritempool).append(item)

    def set_rule(self, spot: Location | Entrance, rule: Any) -> None:
        """Also remembers the built rule, which logic_export sends to trackers as data."""
        super().set_rule(spot, rule)
        if isinstance(rule, Rule):
            if not hasattr(self, "_built_rules"):
                self._built_rules: dict[int, tuple[Any, Rule]] = {}
            self._built_rules[id(spot.access_rule)] = (spot.access_rule, rule)

    def exported_rule(self, access_rule: Any) -> Rule | None:
        """The built rule behind a resolved one; None when there is none (always open)."""
        found = getattr(self, "_built_rules", {}).get(id(access_rule))
        if found is not None and found[0] is access_rule:
            return found[1]
        if access_rule is Location.access_rule or access_rule is Entrance.access_rule:
            return None
        raise TypeError("Big Walk: a rule set without the rule builder cannot travel to trackers")

    def create_item(self, name: str) -> items.BigWalkItem:
        return items.create_item(self, name)

    def get_filler_item_name(self) -> str:
        return items.get_random_filler_item_name(self)

    def fill_slot_data(self) -> Mapping[str, Any]:
        return {
            **self.options.as_dict(*SLOT_OPTIONS, toggles_as_bools=True),
            # Choices travel as frozen strings, so reordering or renaming one never breaks the mod.
            **{name: getattr(self.options, name).current_key for name in SLOT_CHOICES},
            "world_version": WORLD_VERSION,
            # The pre-rename goal names, so an older mod still reads a new seed.
            "goal": bigwalk_options.GOAL_ON_THE_WIRE[self.options.goal.current_key],
            "objectives": list(checks.objective_keys(self)) if self.objectives else [],
            "lock_endings": bool(self.objectives and self.options.lock_endings_until_objectives),
            "tower_order": list(self.tower_order),
            "green_dome_slots": monuments.tower_slots(self, data.tower_by_prop("bigKeyOverflow")),
            "monument_item_id": data.progressive_monument_id(),
            "tower_reached_keys": [tower.prop_name for tower in data.reached_towers() if self.options.tower_sanity],
            "tower_reach_hints": monuments.tower_reach_hints(self),
            "deposit_goal_amount": self.deposit_goal,
            "deposit_locations": bigwalk_options.GOURD_SANITY_ON_THE_WIRE[
                self.options.gourd_sanity.current_key],
            "radio_station_checks": bool(self.options.radio_sanity),

            "radio_station_items": bool(self.options.shuffle_radio_music),

            # `SavableSystem` names of the doors held closed until their item arrives.
            "locked_arch_doors": [door.system_name for door in self.locked_arch_doors],


            # Teleport destinations in the order of their item ids.
            "teleport_destinations": [key for key, _ in data.TELEPORT_DESTINATIONS],

            # Outside trackers (PopTracker) evaluate it instead of porting the rules.
            "logic": logic_export.export(self),

            # Cabin Fever waits, flat: mode, seconds, help, hide_timer.
            **{
                f"{puzzle}_{field}": value
                for puzzle, wait in self.cabin_fever.items()
                for field, value in wait.items()
            },

            # The mod's "vanilla" means the packs free on the map, which the apworld calls off.
            "pack_sanity": "self" if self.options.pack_sanity == bigwalk_options.PackSanity.option_vanilla
            else bool(self.options.pack_sanity),
            "pickup_guids": [pickup.guid for pickup in data.PICKUPS],
            "firework_keys": [firework.key for firework in data.FIREWORKS],
            "flare_gun_sanity": bool(self.options.flare_gun_sanity),
            "flare_gun_guids": [pickup.guid for pickup in data.FLARE_GUN_PICKUPS],
            "object_guids": [obj.guid for obj in data.ISLAND_OBJECTS] if checks.object_location_names(self) else [],
            "object_kinds": [kind for kind, _ in data.OBJECT_KINDS],
            # The mod hides the other kinds while object_sanity is on.
            "object_sanity_kinds": [kind for kind, _ in data.OBJECT_KINDS if checks.object_kind_counted(self, kind)],
            "object_sanity_kind_keys": sorted(self.options.object_sanity_kinds.value),
            # Light keys in id order: a lamp by guid, a switch by its state's path.
            "light_keys": [light.key for light in data.LIGHTS] if checks.lights(self) else [],
            "lights_required_count": checks.lights_target(self),
            "lights_counted": [light.key for light in checks.lights_counted(self)],
            # Gadgets each zone item keeps off the map; lamps and switches are only out of logic.
            "zone_locked_gadgets": {
                str(data.zone_lock_item_id(lock)): [
                    obj.guid for obj in data.ISLAND_OBJECTS
                    if obj.kind not in data.LAMP_KINDS and checks.object_kind_counted(self, obj.kind)
                    and data.zone_lock_for(obj.guid, obj.zone, False) == lock]
                for lock in checks.zone_locks(self) if not lock.lamps},
            "tunnel_doors": {
                str(data.tunnel_door_item_id(door)): list(door.gates)
                for door in data.TUNNEL_DOORS if self.options.tunnel_sanity},
            "objects_required_count": checks.objects_target(self),
            "body_required_count": items.body_target(self),

            # Indices into the mod's palette; empty keeps the game's own colors.
            "color_palette": self.color_palette,
            # The drawing each basic tile shows, PegTileA1 to PegTileA9.
            "peg_tiles": list(self.peg_tiles),
            "peg_drawings": [f"{place}={name}" for place, name in sorted(self.peg_drawings.items())],

            "effect_items": {
                str(effect.offset): effect.key for effect in (*data.TRAPS, *data.BONUSES)},

            "death_link_roulette": checks.death_link_roulette(self),

            # Need keys in the order of their item ids.
            "puzzle_need_keys": [need.key for need in data.PUZZLE_NEEDS],

            "ability_keys": [ability.key for ability in data.ABILITIES],
            "lock_running": bool(self.options.lock_running),

            "monument_full_keys": [tower.prop_name for tower in monuments.monuments_full(self)],
            # Each monument key is sent once its monument holds the gourds at the same position.
            "monument_key_keys": [tower.prop_name for tower in monuments.monument_keys(self)],
            "monument_key_gourds": [monuments.key_gourds(self, tower) for tower in monuments.monument_keys(self)],

            # Not options: sent so a mod too old to read them keeps the vanilla keys and doors.
            "big_key_features": True,

            "big_key_items": True,

            "total_monument_slots": self.gourd_count,
            "deposit_location_amounts": list(self.deposit_amounts),

            "gourd_name": self.gourd_name,
        }
