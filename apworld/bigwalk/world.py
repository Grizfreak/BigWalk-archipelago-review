"""The Big Walk Archipelago world."""

from __future__ import annotations

import logging
import random
from collections import Counter
from collections.abc import Mapping
from typing import TYPE_CHECKING, Any, TextIO

from BaseClasses import CollectionState, Entrance, Item, Location, LocationProgressType, MultiWorld
from Options import OptionError
from rule_builder.rules import Rule
from worlds.AutoWorld import World

from . import data, gourd_name, items, locations, logic_export, regions, rules, tiles, web_world
from . import options as bigwalk_options

if TYPE_CHECKING:
    from NetUtils import MultiData

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

    locked_arch_doors: tuple[data.ArchDoor, ...]
    """Arch doors held closed until their item arrives."""

    gourd_name: str = data.GOURD_ITEM_NAME
    """Display name of the gourds, shared by every Big Walk slot."""

    way_out: tuple[str, ...] = ()
    """Items asked for early as the way out of the starting zone."""

    @staticmethod
    def interpret_slot_data(slot_data: Mapping[str, Any]) -> Mapping[str, Any]:
        """Non-None asks Universal Tracker to re-generate with it in `re_gen_passthrough`."""
        return slot_data

    def _cabin_fever_waits(self) -> dict[str, dict[str, object]]:
        """The two Cabin Fever waits by puzzle; the game's own wait is sent as `vanilla`."""
        waits: dict[str, dict[str, object]] = {}
        for key, game_wait in (("cabin_fever", 300), ("cabin_fever_long", 1800)):
            seconds = getattr(self.options, f"{key}_seconds").value
            mode = "vanilla" if seconds == game_wait else "fixed"
            waits[key] = {
                "mode": mode,
                "seconds": 0 if mode == "vanilla" else seconds,
                "help": bool(getattr(self.options, f"{key}_help")),
                "hide_timer": bool(getattr(self.options, f"{key}_hide_timer")),
            }
        return waits

    def generate_early(self) -> None:
        self._take_options_from_tracker()

        # `linear_keys` puts the Tunnels on the Yellow key's deposit: no doors to shuffle then.
        if self.options.linear_keys and self.options.tunnel_sanity:
            logging.warning(f"Big Walk ({self.player_name}): tunnel_sanity is off with linear_keys, whose Yellow key "
                            f"opens every tunnel door at once.")
            self.options.tunnel_sanity.value = 0

        # The first four colors drawn paint the four flare guns.
        self.color_palette: list[int] = []
        if self.options.random_colors:
            count = self.random.randint(5, data.PALETTE_SIZE)
            self.color_palette = self.random.sample(range(data.PALETTE_SIZE), count)

        self.towers = data.TOWERS
        self.gourd_count = data.MAX_MONUMENT_SLOTS

        self.peg_tiles = self._draw_the_tiles()
        self.peg_drawings: dict[str, str] = {}
        if self.options.custom_drawings:
            own = random.Random(f"{self.multiworld.seed}-{self.player}-drawings")
            self.peg_drawings = tiles.draw_drawings(own, [drawing.key for drawing in tiles.CUSTOM_DRAWINGS])
        if self.options.mirrored_poses.value:
            poses = random.Random(f"{self.multiworld.seed}-{self.player}-poses")
            self.peg_drawings.update(tiles.draw_poses(poses, self.options.mirrored_poses.value))
        self.deposit_amounts = self._pick_deposit_amounts(self.gourd_count)
        self.cabin_fever = self._cabin_fever_waits()
        self.deposit_goal = self.options.gourds_required.value
        self._check_the_objectives()
        self._draw_the_tower_order()
        self._draw_the_monument_keys()

        self._pick_a_random_puzzle_need()
        self._ask_for_buttons_early()
        opened = self.options.start_with_arch_doors_open.value
        # With `linear_arch_doors` no door is an item; the game's buttons open them.
        self.locked_arch_doors = () if self.options.linear_arch_doors else tuple(
            door for door in data.ARCH_DOORS if door.item_name not in opened)
        self._ask_for_an_early_way_out()
        self._ask_for_the_first_monument_early()
        self._ask_for_the_far_doors_early()
        self._hint_the_keys()
        self._keep_the_gauntlet_stages_local()
        if self.options.zone_locks_local:
            self.options.local_items.value |= {lock.item_name for lock in self.zone_locks()}

    @classmethod
    def stage_generate_early(cls, multiworld: MultiWorld) -> None:
        # The data package is per game, so one name for all Big Walk slots.
        worlds = [multiworld.worlds[player] for player in multiworld.get_game_players(cls.game)]
        taken = (*cls.item_name_to_id, *cls.item_name_groups)
        name = gourd_name.shared({
            world.player_name: gourd_name.clean(world.options.gourd_name.value, taken, world.player_name)
            for world in worlds
        })
        for world in worlds:
            world.gourd_name = name

    def modify_multidata(self, multidata: MultiData) -> None:
        package = multidata["datapackage"].get(self.game)
        if self.gourd_name == data.GOURD_ITEM_NAME or package is None:
            return
        if data.GOURD_ITEM_NAME in package["item_name_to_id"]:
            multidata["datapackage"][self.game] = gourd_name.renamed_package(package, self.gourd_name)

    def write_spoiler_header(self, spoiler_handle: TextIO) -> None:
        if self.gourd_name != self.options.gourd_name.value:
            spoiler_handle.write(f"{'Gourds called:':33}{self.gourd_name}\n")
        if self.options.random_objectives.value:
            spoiler_handle.write(f"{'Objectives drawn:':33}{', '.join(self.objective_keys())}\n")
        if self.peg_tiles != tiles.VANILLA_TILES:
            spoiler_handle.write(f"{'Basic tiles shown as:':33}{', '.join(self.peg_tiles)}\n")
        mirrors = {place: name for place, name in self.peg_drawings.items() if place in tiles.POSE_PLACES}
        if mirrors:
            spoiler_handle.write(f"{'Mirrored poses:':33}"
                                 f"{', '.join(f'{place}={name}' for place, name in sorted(mirrors.items()))}\n")

    def _draw_the_tiles(self) -> tuple[str, ...]:
        """Own generator, so the tiles do not change what the rest of the seed draws."""
        mode = self.options.custom_tiles
        if mode == bigwalk_options.CustomTiles.option_off:
            return tiles.VANILLA_TILES
        drawings = [tile.key for tile in tiles.CUSTOM_TILES]
        preferred: list[str] = []
        if mode == bigwalk_options.CustomTiles.option_custom_only:
            candidates = drawings
        elif mode == bigwalk_options.CustomTiles.option_all:
            candidates = [*tiles.VANILLA_TILES, *drawings]
        else:
            matching = tiles.matching(self.multiworld.game.values())
            candidates = [*tiles.VANILLA_TILES, *matching]
            if mode == bigwalk_options.CustomTiles.option_matching_first:
                preferred = matching
        own = random.Random(f"{self.multiworld.seed}-{self.player}-tiles")
        return tiles.draw(own, candidates, self.options.custom_tile_weights.value,
                          sorted(self.options.custom_tile_choices.value), preferred)

    def _keep_the_gauntlet_stages_local(self) -> None:
        locked = self.options.gauntlet_mode == bigwalk_options.GauntletMode.option_locked_stages
        if locked and self.options.gauntlet_stages_local:
            self.options.local_items.value |= set(items.GAUNTLET_ITEM_NAMES) | {data.PROGRESSIVE_GAUNTLET_ITEM_NAME}

    def _hint_the_keys(self) -> None:
        if self.options.hint_keys != bigwalk_options.HintKeys.option_from_start:
            return
        self.options.start_hints.value |= {data.key_item_name(tower) for tower in self.towers}

    def _pick_a_random_puzzle_need(self) -> None:
        """The pick goes into the option, so slot_data carries it and the tracker draws nothing."""
        if not self.options.lock_puzzle_needs:
            return

        candidates = data.tutorial_footholds(self.options.start_with_puzzle_needs.value)
        if candidates and not self.options.start_with_random_puzzle_need:
            # Generation would never end here rather than fail.
            raise OptionError(
                f"Big Walk: {self.player_name} starts with nothing to do. With lock_puzzle_needs on, list "
                "buttons or sync_buttons in start_with_puzzle_needs, or turn start_with_random_puzzle_need "
                "on, so that one of the tutorial's puzzles can be solved from the start.")
        if candidates:
            picked = self.random.choice(candidates)
            self.options.start_with_puzzle_needs.value = set(self.options.start_with_puzzle_needs.value) | {picked.key}

    def _ask_for_buttons_early(self) -> None:
        """The most shared puzzle needs come early, or the start has too few locations to fill."""
        if not self.options.lock_puzzle_needs:
            return
        for need in data.EARLY_NEEDS:
            if need.key not in self.options.start_with_puzzle_needs.value:
                self.multiworld.early_items[self.player][need.item_name] = 1

    def _ask_for_an_early_way_out(self) -> None:
        """The starting zone holds few checks, so its way out comes as an early local item."""
        self.way_out = ()
        if self.options.start_with_drawbridge_open:
            return
        # With the game's own doors the First Arch Door's button is outside: the Drawbridge it is.
        if self.options.linear_arch_doors:
            if not self.options.linear_keys:
                self.way_out = (data.DRAWBRIDGE_ITEM_NAME,)
            elif not self.key_in_its_monument(data.TOWERS[0]):
                # The Drawbridge sits on its key's deposit in the tutorial.
                self.way_out = (data.key_item_name(data.TOWERS[0]),)
            else:
                # The tutorial's monument holds the key, so its gourds must be found there.
                gourds = 0 if self.options.linear_puzzles else self.key_gourds(data.TOWERS[0])
                self.way_out = (data.GOURD_ITEM_NAME,) * gourds
                if self.options.tower_mode == bigwalk_options.TowerMode.option_by_item and not self.options.first_tower_open:
                    self.way_out += (data.PROGRESSIVE_MONUMENT_ITEM_NAME,)
        elif data.FIRST_ARCH_DOOR in self.locked_arch_doors:
            self.way_out = (data.FIRST_ARCH_DOOR.item_name if self.options.linear_keys
                            else self.random.choice((data.DRAWBRIDGE_ITEM_NAME, data.FIRST_ARCH_DOOR.item_name)),)
        for name in self.way_out:
            self.multiworld.local_early_items[self.player][name] = self.way_out.count(name)

    def _ask_for_the_first_monument_early(self) -> None:
        """Nearly everything waits for this item; left to the fill it can find nowhere to go."""
        if (self.options.tower_mode != bigwalk_options.TowerMode.option_by_item or self.options.first_tower_open
                or data.PROGRESSIVE_MONUMENT_ITEM_NAME in self.way_out):
            return
        self.multiworld.local_early_items[self.player][data.PROGRESSIVE_MONUMENT_ITEM_NAME] = 1

    def _ask_for_the_far_doors_early(self) -> None:
        """Skipped when the start is too small: two more early items would make generation fail."""
        if (not self.options.require_arch_doors or data.FIRST_ARCH_DOOR in self.locked_arch_doors
                or self.options.lock_puzzle_needs):
            return

        for door in (data.LEFT_ARCH_DOOR, data.RIGHT_ARCH_DOOR):
            if door in self.locked_arch_doors:
                self.multiworld.local_early_items[self.player][door.item_name] = 1

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

    def _check_the_objectives(self) -> None:
        """The objectives besides the Goal: those listed, or a few drawn (`random_objectives`)."""
        passthrough = getattr(self.multiworld, "re_gen_passthrough", None)
        slot_data = passthrough.get(self.game) if passthrough else None
        if slot_data and "objectives" in slot_data:
            self.objectives = frozenset(slot_data["objectives"]) - {self.options.goal.current_key}
            return

        if (self.options.goal == bigwalk_options.Goal.option_big_wall
                and self.options.gauntlet_mode == bigwalk_options.GauntletMode.option_locked_stages):
            raise OptionError(
                f"Big Walk ({self.player_name}): the goal big_wall ends the run at the chapel, before the Silent "
                f"Gauntlet, whose locked stages would then hold checks for nothing. Set "
                f"gauntlet_mode to vanilla, or the goal to big_goodbye or big_game.")

        listed = {bigwalk_options.OBJECTIVE_ALIASES.get(key, key) for key in self.options.objectives.value}
        endings = sorted(listed & set(bigwalk_options.OBJECTIVE_ENDINGS))
        if endings:
            raise OptionError(
                f"Big Walk ({self.player_name}): {', '.join(endings)} is an ending: it is the goal, not an objective. "
                f"Set `goal` to the ending, and list the other things to do in `objectives`.")

        count = self.options.random_objectives.value
        if count:
            pool = listed or {key for key in bigwalk_options.OBJECTIVE_KEYS
                              if key not in bigwalk_options.OBJECTIVE_ENDINGS and self._objective_allowed(key)}
            if count > len(pool):
                logging.warning(f"Big Walk ({self.player_name}): random_objectives asks for {count}, "
                                f"only {len(pool)} can be drawn.")
                count = len(pool)
            listed = set(self.random.sample(sorted(pool), count))

        if "big_objects" in listed and self.options.object_sanity == bigwalk_options.ObjectSanity.option_off:
            raise OptionError(
                f"Big Walk ({self.player_name}): the Objectives hold big_objects, whose objects are the Object Sanity "
                f"checks. Set Object Sanity to first_pickup or every.")
        if "big_body" in listed and self.options.lock_abilities == bigwalk_options.LockAbilities.option_disabled:
            raise OptionError(
                f"Big Walk ({self.player_name}): the Objectives hold big_body, whose abilities are the Lock Abilities "
                f"items. Set Lock Abilities to handed or ambidextrous.")
        if "big_radio" in listed and not self.options.radio_sanity:
            raise OptionError(
                f"Big Walk ({self.player_name}): the Objectives hold big_radio, whose stations are the "
                f"Radio Sanity checks. Turn Radio Sanity on.")
        self.objectives = frozenset(listed)

    def _objective_allowed(self, key: str) -> bool:
        """Whether the other options let this objective be drawn."""
        if key == "big_radio":
            return bool(self.options.radio_sanity)
        if key == "big_objects":
            return self.options.object_sanity != bigwalk_options.ObjectSanity.option_off
        if key == "big_body":
            return self.options.lock_abilities != bigwalk_options.LockAbilities.option_disabled
        if key == "big_towers":
            return self.options.tower_mode != bigwalk_options.TowerMode.option_all_open
        return True

    def _tower_reach_hints(self) -> dict[str, list[int]]:
        """Location ids to hint by tower once it is reached."""
        hints: dict[str, list[int]] = {}
        for tower in data.TOWERS:
            ids: list[int] = []
            if self.options.hint_discovered_key_locations:
                if self.options.cut_key_sanity != bigwalk_options.CutKeySanity.option_off:
                    ids += [data.cut_location_id(tower, i) for i in range(tower.segments)]
                ids.append(data.key_deposit_location_id(tower))
            if self.options.hint_discovered_deposit_boxes and self.tower_order:
                ids += self._deposit_ids_of(tower)
            if ids:
                hints[tower.prop_name] = ids
        return hints

    def _deposit_ids_of(self, tower: data.Tower) -> list[int]:
        """The gourd deposit checks in this tower's monument, in the seed's order of towers."""
        start = 0
        for prop in self.tower_order:
            if prop == tower.prop_name:
                break
            start += self.tower_slots(data.tower_by_prop(prop))
        return [data.deposit_location_id(amount) for amount in self.deposit_amounts
                if start < amount <= start + self.tower_slots(tower)]

    def tower_slots(self, tower: data.Tower) -> int:
        """Slots the seed's gourds can fill; the Green Dome gives up any shortfall."""
        missing = max(0, data.MAX_MONUMENT_SLOTS - self.gourd_count)
        if tower.prop_name == "bigKeyOverflow":
            return max(0, tower.slots - missing)
        return tower.slots

    def _draw_the_tower_order(self) -> None:
        """The order the monuments open in; the seed's own while Universal Tracker re-generates."""
        self.tower_order: tuple[str, ...] = ()
        if self.options.tower_mode == bigwalk_options.TowerMode.option_all_open:
            if "big_towers" in self.objectives:
                raise OptionError(
                    f"Big Walk ({self.player_name}): the Objectives hold big_towers, which needs Tower Mode "
                    f"in_order or by_item.")
            return

        if not self.options.first_tower_open and self.options.tower_mode != bigwalk_options.TowerMode.option_by_item:
            raise OptionError(
                f"Big Walk ({self.player_name}): first_tower_open is off, which needs Tower Mode by_item: in_order has "
                f"nothing to open the first monument.")

        passthrough = getattr(self.multiworld, "re_gen_passthrough", None)
        slot_data = passthrough.get(self.game) if passthrough else None
        known = tuple(slot_data.get("tower_order", ())) if slot_data else ()
        props = [tower.prop_name for tower in self.towers]
        if sorted(known) == sorted(props):
            self.tower_order = known
            return

        order = list(props)
        self.random.shuffle(order)
        self.tower_order = tuple(order)

    def tower_index(self, prop_name: str) -> int:
        return self.tower_order.index(prop_name)

    def _goal_for_older_mods(self) -> str:
        """The single goal read by mods older than the objectives."""
        return self.options.goal.current_key

    def objective_keys(self) -> tuple[str, ...]:
        """The objectives and the Goal, in the frozen order."""
        listed = self.objectives | {self.options.goal.current_key}
        return tuple(key for key in bigwalk_options.OBJECTIVE_KEYS if key in listed)

    def _pick_deposit_amounts(self, total_slots: int) -> tuple[int, ...]:
        choice = self.options.gourd_sanity
        if choice == bigwalk_options.GourdSanity.option_off:
            return ()
        if choice == bigwalk_options.GourdSanity.option_every_gourd:
            return tuple(range(1, total_slots + 1))

        # Every fifth deposit, plus the last one in case the total is not a multiple of five.
        milestones = set(range(5, total_slots + 1, 5))
        milestones.add(total_slots)
        return tuple(sorted(milestones))

    def create_regions(self) -> None:
        regions.create_and_connect_regions(self)
        locations.create_all_locations(self)

    def create_items(self) -> None:
        items.create_all_items(self)
        # `lock_running: early`: the fill places it in sphere 1, in any world.
        if self.options.lock_running == bigwalk_options.LockRunning.option_early and any(
                item.player == self.player and item.name == data.RUNNING_ITEM_NAME for item in self.multiworld.itempool):
            self.multiworld.early_items[self.player][data.RUNNING_ITEM_NAME] = 1

    def set_rules(self) -> None:
        rules.set_all_rules(self)

    def set_rule(self, spot: Location | Entrance, rule: Any) -> None:
        """Also remembers the built rule, which logic_export sends to the mod as data."""
        super().set_rule(spot, rule)
        if isinstance(rule, Rule):
            if not hasattr(self, "_built_rules"):
                self._built_rules: dict[int, tuple[Any, Rule]] = {}
            self._built_rules[id(spot.access_rule)] = (spot.access_rule, rule)

    def _hint_items(self) -> dict[str, int]:
        """Items `!hint` can find, by name and count; start items are left out."""
        placed = [location.item for location in self.multiworld.get_locations() if location.item is not None]
        counts = Counter(item.name for item in {id(item): item for item in [*self.multiworld.itempool, *placed]}.values()
                         if item.player == self.player and item.code is not None)
        return dict(sorted(counts.items()))

    def exported_rule(self, access_rule: Any) -> Rule | None:
        """The built rule behind a resolved one; None when there is none (always open)."""
        found = getattr(self, "_built_rules", {}).get(id(access_rule))
        if found is not None and found[0] is access_rule:
            return found[1]
        if access_rule is Location.access_rule or access_rule is Entrance.access_rule:
            return None
        raise TypeError("Big Walk: a rule set without the rule builder cannot travel to the mod")

    def pre_fill(self) -> None:
        # Local items first: they may take the starting zone's places, which Legs must then see.
        self._place_local_items("Objects", self.options.object_sanity_items.value, "object_sanity_items")
        self._place_local_items("Lights", self.options.light_sanity_items.value, "light_sanity_items")
        self._make_room_at_the_start()
        self._open_the_way_one_step_at_a_time()

    def _open_the_way_one_step_at_a_time(self) -> None:
        """Where two items are needed with one free location, the player starts with one of them."""
        mine = [item for item in self.multiworld.itempool if item.player == self.player and item.advancement]
        if not mine:
            return
        own = self.multiworld.get_locations(self.player)
        state = CollectionState(self.multiworld)
        state.sweep_for_advancements(own)
        used = 0

        def reach(with_state: CollectionState) -> set[str]:
            return {loc.name for loc in self.multiworld.get_unfilled_locations(self.player)
                    if loc.progress_type != LocationProgressType.EXCLUDED and loc.can_reach(with_state)}

        def opens(names: tuple[str, ...], now: set[str]) -> int:
            player = self.player
            saved = (state.prog_items[player].copy(), state.reachable_regions[player].copy(),
                     state.blocked_connections[player].copy(), state.stale[player],
                     state.advancements.copy(), state.locations_checked.copy())
            try:
                for name in names:
                    state.collect(self.create_item(name), True)
                state.sweep_for_advancements(own)
                return len(reach(state) - now)
            finally:
                (state.prog_items[player], state.reachable_regions[player], state.blocked_connections[player],
                 state.stale[player], state.advancements, state.locations_checked) = saved

        for _ in range(len(mine)):
            now = reach(state)
            free = len(now) - used
            names = sorted({item.name for item in mine})
            if not names:
                return
            singles = [(opens((name,), now), name) for name in names]
            best = max(singles)
            if best[0] > 0:
                chosen: tuple[str, ...] = (best[1],)
            else:
                pairs = [(opens((a, b), now), (a, b)) for i, a in enumerate(names) for b in names[i:]
                         if a != b or sum(1 for item in mine if item.name == a) > 1]
                pairs = [pair for pair in pairs if pair[0] > 0]
                if not pairs:
                    return
                chosen = max(pairs)[1]
            if free < len(chosen):
                item = next(item for item in mine if item.name == chosen[0])
                logging.warning(f"Big Walk ({self.player_name}): {' and '.join(chosen)} needed with room for "
                                f"{max(free, 0)}; starting with {item.name}.")
                mine.remove(item)
                self.multiworld.itempool[:] = [other for other in self.multiworld.itempool if other is not item]
                local_early = self.multiworld.local_early_items[self.player]
                if local_early.get(item.name):
                    local_early[item.name] -= 1
                    if not local_early[item.name]:
                        del local_early[item.name]
                self.push_precollected(item)
                self.multiworld.itempool.append(self.create_filler())
                state.collect(item, True)
                state.sweep_for_advancements(own)
                continue
            for name in chosen:
                item = next(item for item in mine if item.name == name)
                mine.remove(item)
                state.collect(item, True)
                used += 1
            state.sweep_for_advancements(own)

    def _make_room_at_the_start(self) -> None:
        """When the starting zone cannot hold its early items, the player starts with some."""
        local_early = self.multiworld.local_early_items[self.player]
        mine = [item for item in self.multiworld.itempool if item.player == self.player]
        if any(item.name == data.LEGS_ITEM_NAME for item in mine):
            local_early[data.LEGS_ITEM_NAME] = 1
        # Given up from the end, Legs first. By identity: items compare equal by name and player.
        needed: list[Item] = []
        monument = () if data.PROGRESSIVE_MONUMENT_ITEM_NAME in self.way_out else (data.PROGRESSIVE_MONUMENT_ITEM_NAME,)
        for name in (*self.way_out, *monument, data.LEGS_ITEM_NAME):
            item = next((item for item in mine if item.name == name and all(item is not n for n in needed)), None)
            if item is not None and name in local_early:
                needed.append(item)
        if not needed:
            return

        # The early fill's own reach: start items and the events they open.
        state = CollectionState(self.multiworld)
        state.sweep_for_advancements(locations=[loc for loc in self.multiworld.get_filled_locations(self.player)
                                                if loc.address is None])
        room = sum(1 for loc in self.multiworld.get_unfilled_locations(self.player)
                   if loc.progress_type != LocationProgressType.EXCLUDED and loc.can_reach(state))
        if room < sum(local_early.values()):
            for name in [name for name in local_early if all(item.name != name for item in needed)]:
                del local_early[name]
        while len(needed) > room:
            item = needed.pop()
            logging.warning(f"Big Walk ({self.player_name}): no room in the starting zone for {item.name}; "
                            f"starting with it.")
            local_early[item.name] -= 1
            if not local_early[item.name]:
                del local_early[item.name]
            self.multiworld.itempool[:] = [other for other in self.multiworld.itempool if other is not item]
            self.push_precollected(item)
            self.multiworld.itempool.append(self.create_filler())  # the location it leaves

    def _place_local_items(self, group: str, contents: int, option: str) -> None:
        """Fills these checks with this world's non-progression items before the main fill."""
        if contents != bigwalk_options.ObjectSanityItems.option_local:
            return
        names = locations.LOCATION_NAME_GROUPS[group]
        spots = [loc for loc in self.multiworld.get_unfilled_locations(self.player) if loc.name in names]
        own = [item for item in self.multiworld.itempool if item.player == self.player and not item.advancement]
        self.random.shuffle(own)
        if len(own) < len(spots):
            logging.warning(f"Big Walk ({self.player_name}): {option} local: only {len(own)} items of this "
                            f"world for {len(spots)} checks; the others may hold anything.")
        placed = set()
        for location, item in zip(spots, own):
            location.place_locked_item(item)
            placed.add(id(item))
        # By identity: items compare equal by name and player, so remove() may take another copy.
        self.multiworld.itempool[:] = [item for item in self.multiworld.itempool if id(item) not in placed]

    def create_item(self, name: str) -> items.BigWalkItem:
        return items.create_item(self, name)

    def get_filler_item_name(self) -> str:
        return items.get_random_filler_item_name(self)

    def _death_link_roulette(self) -> dict[str, int]:
        weights = self.options.death_link_trap_weights.value
        allow_hard = bool(self.options.death_link_roulette_hard_traps)
        return {
            trap.key: weights[trap.item_name]
            for trap in data.TRAPS
            if weights.get(trap.item_name, 0) > 0 and (allow_hard or not trap.hard)
        }

    def tutorial_monument_first(self) -> bool:
        """Whether the tutorial's monument takes the first gourds."""
        return not self.tower_order or self.tower_order[0] == data.TOWERS[0].prop_name

    def key_in_its_monument(self, tower: data.Tower) -> bool:
        """Not the Drawbridge Key when its monument would lie past the drawbridge it opens."""
        if not self.options.linear_towers or self.tower_slots(tower) <= 0:
            return False
        return not (tower is data.TOWERS[0] and self.options.linear_arch_doors and self.options.linear_keys
                    and not self.options.start_with_drawbridge_open and not self.tutorial_monument_first())

    def key_location_name(self, tower: data.Tower) -> str:
        """Where `linear_towers` puts this tower's key: its monument full, or its monument key."""
        if self.options.linear_towers == bigwalk_options.LinearTowers.option_one_deposit_box:
            return data.monument_key_location_name(tower)
        return data.monument_full_location_name(tower)

    def key_gourds(self, tower: data.Tower) -> int:
        """How many gourds this tower's monument holds when its key comes (`linear_towers`)."""
        return self.monument_key_gourds.get(tower.prop_name, self.tower_slots(tower))

    def monuments_full(self) -> list[data.Tower]:
        """The towers whose monument full is a check."""
        if not (self.options.monument_sanity
                or self.options.linear_towers == bigwalk_options.LinearTowers.option_all_deposit_boxes):
            return []
        return [tower for tower in data.TOWERS if self.tower_slots(tower) > 0]

    def monument_keys(self) -> list[data.Tower]:
        """The towers whose monument key is a location (`linear_towers: one_deposit_box`)."""
        if self.options.linear_towers != bigwalk_options.LinearTowers.option_one_deposit_box:
            return []
        return [tower for tower in data.TOWERS if self.key_in_its_monument(tower)]

    def _draw_the_monument_keys(self) -> None:
        """For each tower, which gourd of its monument frees its key (`one_deposit_box`)."""
        self.monument_key_gourds: dict[str, int] = {}
        if self.options.linear_towers != bigwalk_options.LinearTowers.option_one_deposit_box:
            return
        passthrough = getattr(self.multiworld, "re_gen_passthrough", None)
        slot_data = passthrough.get(self.game) if passthrough else None
        if slot_data and "monument_key_gourds" in slot_data:
            self.monument_key_gourds = dict(zip(slot_data["monument_key_keys"], slot_data["monument_key_gourds"]))
            return
        for tower in data.TOWERS:
            if self.tower_slots(tower) > 0:
                self.monument_key_gourds[tower.prop_name] = self.random.randint(1, self.tower_slots(tower))

    def puzzles(self) -> list[data.Puzzle]:
        """The puzzles that are checks."""
        puzzles = list(data.PUZZLES)
        if self.options.include_big_game_puzzles == bigwalk_options.IncludeBigGamePuzzles.option_disabled:
            puzzles = [p for p in puzzles if p.prop_name not in data.CHAIRLIFT_PUZZLES]
        return puzzles

    def lights(self) -> list[data.Light]:
        """The lights that are checks (`light_sanity`)."""
        if self.options.light_sanity == bigwalk_options.LightSanity.option_off:
            return []
        return self.lights_counted()

    def lights_counted(self) -> list[data.Light]:
        """The lights `big_lights` counts; with `light_sanity` off, the lamps, without checks."""
        mode = self.options.light_sanity
        if mode == bigwalk_options.LightSanity.option_off and "big_lights" not in self.objectives:
            return []
        return [light for light in data.LIGHTS
                if not light.switch or mode == bigwalk_options.LightSanity.option_lamps_and_switches]

    def object_location_names(self) -> list[str]:
        """Object Sanity's checks: every object counted, or every kind counted."""
        if self.options.object_sanity == bigwalk_options.ObjectSanity.option_every:
            return [obj.location_name for obj in data.ISLAND_OBJECTS if self.object_kind_counted(obj.kind)]
        if self.options.object_sanity == bigwalk_options.ObjectSanity.option_first_pickup:
            return [data.object_kind_location_name(label) for kind, label in data.OBJECT_KINDS if self.object_kind_counted(kind)]
        return []

    def objects_target(self) -> int:
        """`objects_required` percent of the object checks, rounded up, at least one."""
        count = len(self.object_location_names())
        return max(1, -(-count * self.options.objects_required.value // 100)) if count else 0

    def ability_item_names(self) -> list[str]:
        """Ability items the logic counts, once per copy; Running is only useful, so not counted."""
        return [ability.item_name for ability in items.abilities_in_pool(self) for _ in range(ability.copies)
                if ability.item_name != data.RUNNING_ITEM_NAME]

    def body_target(self) -> int:
        """`body_required` percent of the ability items, rounded up, at least one."""
        count = len(self.ability_item_names())
        return max(1, -(-count * self.options.body_required.value // 100)) if count else 0

    def lights_target(self) -> int:
        """`lights_required` percent of the lights counted, rounded up, at least one."""
        lights = len(self.lights_counted())
        return max(1, -(-lights * self.options.lights_required.value // 100)) if lights else 0

    def zone_locks(self) -> list[data.ZoneLock]:
        """The zone items of this seed."""
        if not self.options.zone_locks:
            return []
        objects = self.options.object_sanity != bigwalk_options.ObjectSanity.option_off
        lamps = self.options.light_sanity != bigwalk_options.LightSanity.option_off or objects and any(
            self.object_kind_counted(kind) for kind in data.LAMP_KINDS)
        gadgets = objects and any(self.object_kind_counted(kind) for kind, _ in data.OBJECT_KINDS
                                  if kind not in data.LAMP_KINDS)
        return [lock for lock in data.ZONE_LOCKS if (lamps if lock.lamps else gadgets)]

    def object_kind_counted(self, kind: str) -> bool:
        """With `light_sanity` the lamps count by their light, so no lamp is an object."""
        if kind in data.LAMP_KINDS and self.options.light_sanity != bigwalk_options.LightSanity.option_off:
            return False
        return data.OBJECT_KIND_KEYS[kind] in self.options.object_sanity_kinds.value

    def fill_slot_data(self) -> Mapping[str, Any]:
        return {
            "world_version": WORLD_VERSION,

            # Choices travel as frozen strings, so reordering or renaming one never breaks the mod.
            "goal": bigwalk_options.GOAL_ON_THE_WIRE[self._goal_for_older_mods()],
            "objectives": list(self.objective_keys()) if self.objectives else [],
            "lock_endings": bool(self.objectives and self.options.lock_endings_until_objectives),
            # For Universal Tracker: lock_endings is false without objectives.
            "lock_endings_until_objectives": bool(self.options.lock_endings_until_objectives),
            "keys_required": self.options.keys_required.value,
            "tower_mode": self.options.tower_mode.current_key,
            "tower_order": list(self.tower_order),
            "green_dome_slots": self.tower_slots(data.tower_by_prop("bigKeyOverflow")),
            "first_tower_open": bool(self.options.first_tower_open),
            "everyone_gathers": bool(self.options.everyone_gathers),
            "monument_item_id": data.progressive_monument_id(),
            "towers_required": self.options.towers_required.value,
            "tower_sanity": bool(self.options.tower_sanity),
            "tower_reached_keys": [tower.prop_name for tower in data.reached_towers()] if self.options.tower_sanity else [],
            "tower_reached_id_offset": data.TOWER_REACHED_ID_OFFSET,
            "tower_reach_hints": self._tower_reach_hints(),
            "radio_stations_required": self.options.radio_stations_required.value,
            "deposit_goal_amount": self.deposit_goal,
            "deposit_locations": bigwalk_options.GOURD_SANITY_ON_THE_WIRE[
                self.options.gourd_sanity.current_key],
            "radio_station_checks": bool(self.options.radio_sanity),

            "radio_station_items": bool(self.options.shuffle_radio_music),

            # `SavableSystem` names of the doors held closed until their item arrives.
            "start_with_arch_doors_open": sorted(self.options.start_with_arch_doors_open.value),
            "locked_arch_doors": [door.system_name for door in self.locked_arch_doors],
            "require_arch_doors": bool(self.options.require_arch_doors),
            "arch_door_id_offset": data.ARCH_DOOR_ID_OFFSET,

            # `gauntlet_stage_systems` is in the order of the stage items.
            "gauntlet_mode": self.options.gauntlet_mode.current_key,
            "gauntlet_stage_items": self.options.gauntlet_stage_items.current_key,
            "gauntlet_puzzles_required": bool(self.options.gauntlet_puzzles_required),
            "lock_gauntlet_needs": bool(self.options.lock_gauntlet_needs),
            "gauntlet_stage_systems": [stage.system_name for stage in data.GAUNTLET_STAGES],
            "gauntlet_id_offset": data.GAUNTLET_ID_OFFSET,

            # Teleport destinations in the order of their item ids.
            "teleport_buttons": self.options.teleport_buttons.current_key,
            "teleport_destinations": [key for key, _ in data.TELEPORT_DESTINATIONS],
            "teleport_id_offset": data.TELEPORT_ID_OFFSET,
            "teleport_back_to_hub": bool(self.options.teleport_back_to_hub),

            "open_black_tower": bool(self.options.open_black_tower),
            "tile_thief": self.options.tile_thief.current_key,

            "tower_resync_stations": bool(self.options.tower_resync_stations),

            "tracker_board": bool(self.options.tracker_board),
            "hint_board": bool(self.options.hint_board),
            # For the hint sign's suggestions while a player writes on it.
            "hint_items": self._hint_items(),
            "map_tracker": self.options.map_tracker.current_key,
            # Always sent: outside trackers (PopTracker) evaluate it instead of porting the rules.
            "logic": logic_export.export(self),
            "guests_can_resync": bool(self.options.guests_can_resync),

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
            "pickup_id_offset": data.PICKUP_ID_OFFSET,
            "firework_sanity": bool(self.options.firework_sanity),
            "firework_keys": [firework.key for firework in data.FIREWORKS],
            "firework_id_offset": data.FIREWORK_ID_OFFSET,
            "flare_gun_sanity": bool(self.options.flare_gun_sanity),
            "flare_gun_guids": [pickup.guid for pickup in data.FLARE_GUN_PICKUPS],
            "flare_gun_id_offset": data.FLARE_GUN_ID_OFFSET,
            "object_sanity": self.options.object_sanity.current_key,
            "object_guids": [obj.guid for obj in data.ISLAND_OBJECTS],
            "object_id_offset": data.OBJECT_ID_OFFSET,
            "object_kinds": [kind for kind, _ in data.OBJECT_KINDS],
            "object_kind_id_offset": data.OBJECT_KIND_ID_OFFSET,
            # The mod hides the other kinds while object_sanity is on.
            "object_sanity_kinds": [kind for kind, _ in data.OBJECT_KINDS if self.object_kind_counted(kind)],
            "object_sanity_kind_keys": sorted(self.options.object_sanity_kinds.value),
            # Light keys in id order: a lamp by guid, a switch by its state's path.
            "light_sanity": self.options.light_sanity.current_key,
            "light_keys": [light.key for light in data.LIGHTS],
            "light_id_offset": data.LIGHT_ID_OFFSET,
            "lights_required": self.options.lights_required.value,
            "lights_required_count": self.lights_target(),
            "lights_counted": [light.key for light in self.lights_counted()],
            # Gadgets each zone item keeps off the map; lamps and switches are only out of logic.
            "zone_locks": bool(self.options.zone_locks),
            "zone_locked_gadgets": {
                str(data.zone_lock_item_id(lock)): [
                    obj.guid for obj in data.ISLAND_OBJECTS
                    if obj.kind not in data.LAMP_KINDS and self.object_kind_counted(obj.kind)
                    and data.zone_lock_for(obj.guid, obj.zone, False) == lock]
                for lock in self.zone_locks() if not lock.lamps},
            "tunnel_sanity": bool(self.options.tunnel_sanity),
            "tunnel_doors": {
                str(data.tunnel_door_item_id(door)): list(door.gates)
                for door in data.TUNNEL_DOORS if self.options.tunnel_sanity},
            # The mod takes the golf ball's (the cannonball's) "too heavy" off.
            "fast_golf": bool(self.options.fast_golf),
            "objects_required": self.options.objects_required.value,
            "objects_required_count": self.objects_target(),
            "body_required": self.options.body_required.value,
            "body_required_count": self.body_target(),

            # Indices into the mod's palette; empty keeps the game's own colors.
            "random_colors": bool(self.options.random_colors),
            "color_palette": self.color_palette,
            # The drawing each basic tile shows, PegTileA1 to PegTileA9.
            "peg_tiles": list(self.peg_tiles),
            "peg_drawings": [f"{place}={name}" for place, name in sorted(self.peg_drawings.items())],
            "trap_link": bool(self.options.trap_link),

            "effect_items": {
                str(effect.offset): effect.key for effect in (*data.TRAPS, *data.BONUSES)},
            "trap_duration": self.options.trap_duration.value,
            "traps_spare_the_gauntlet": bool(self.options.traps_spare_the_gauntlet),

            "death_link": self.options.death_link.current_key,
            "death_link_triggers": sorted(self.options.death_link_triggers.value),
            "death_link_amnesty": self.options.death_link_amnesty.value,
            "death_link_trap_on_send": bool(self.options.death_link_trap_on_send),
            "death_link_target": self.options.death_link_target.current_key,
            "death_link_effect": self.options.death_link_effect.current_key,
            "death_link_roulette": self._death_link_roulette(),

            "lock_puzzle_needs": bool(self.options.lock_puzzle_needs),
            "start_with_puzzle_needs": sorted(self.options.start_with_puzzle_needs.value),
            "start_with_random_puzzle_need": bool(self.options.start_with_random_puzzle_need),

            # Need keys in the order of their item ids.
            "puzzle_need_keys": [need.key for need in data.PUZZLE_NEEDS],
            "puzzle_need_id_offset": data.PUZZLE_NEED_ID_OFFSET,

            "lock_abilities": self.options.lock_abilities.current_key,
            "ability_keys": [ability.key for ability in data.ABILITIES],
            "ability_id_offset": data.ABILITY_ID_OFFSET,
            "lock_running": bool(self.options.lock_running),
            "rides_require_seat": bool(self.options.rides_require_seat),
            "start_with_abilities": sorted(self.options.start_with_abilities.value),

            # Of these, only the purple puzzles change what the mod does.
            "include_big_game_puzzles": self.options.include_big_game_puzzles.current_key,
            "cut_key_sanity": self.options.cut_key_sanity.current_key,
            "linear_keys": bool(self.options.linear_keys),
            "linear_arch_doors": bool(self.options.linear_arch_doors),
            "linear_puzzles": bool(self.options.linear_puzzles),
            # For the tracker only: the server hands these doors over as start items either way.
            "start_with_drawbridge_open": bool(self.options.start_with_drawbridge_open),
            "start_with_map_room_open": bool(self.options.start_with_map_room_open),
            "monument_sanity": bool(self.options.monument_sanity),
            "linear_towers": self.options.linear_towers.current_key,
            "monument_full_keys": [tower.prop_name for tower in self.monuments_full()],
            "monument_full_id_offset": data.MONUMENT_FULL_ID_OFFSET,
            # Each monument key is sent once its monument holds the gourds at the same position.
            "monument_key_keys": [tower.prop_name for tower in self.monument_keys()],
            "monument_key_gourds": [self.key_gourds(tower) for tower in self.monument_keys()],
            "monument_key_id_offset": data.MONUMENT_KEY_ID_OFFSET,

            # Not options: sent so a mod too old to read them keeps the vanilla keys and doors.
            "big_key_features": True,

            "big_key_items": True,

            "total_monument_slots": self.gourd_count,
            "deposit_location_amounts": list(self.deposit_amounts),
            "big_keys_in_play": [tower.prop_name for tower in self.towers],

            # Id arithmetic, so the mod computes ids instead of keeping a copy of data.py.
            "location_id_base": data.BASE_ID,
            "radio_id_offset": data.RADIO_ID_OFFSET,
            "deposit_id_offset": data.DEPOSIT_ID_OFFSET,
            "cut_id_offset": data.CUT_ID_OFFSET,
            "key_item_id_offset": data.KEY_ITEM_ID_OFFSET,
            "gourd_item_id": items.ITEM_NAME_TO_ID[data.GOURD_ITEM_NAME],
            "gourd_name": self.gourd_name,
        }
