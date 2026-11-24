"""What generate_early draws and checks."""

from __future__ import annotations

import logging
import random
from typing import TYPE_CHECKING

from Options import OptionError

from . import checks, data, gourd_name, items, monuments, tiles
from . import options as bigwalk_options

if TYPE_CHECKING:
    from .world import BigWalkWorld


def _cabin_fever_waits(world: BigWalkWorld) -> dict[str, dict[str, object]]:
    """The two Cabin Fever waits by puzzle; the game's own wait is sent as `vanilla`."""
    waits: dict[str, dict[str, object]] = {}
    for key, game_wait in (("cabin_fever", 300), ("cabin_fever_long", 1800)):
        seconds = getattr(world.options, f"{key}_seconds").value
        mode = "vanilla" if seconds == game_wait else "fixed"
        waits[key] = {
            "mode": mode,
            "seconds": 0 if mode == "vanilla" else seconds,
            "help": bool(getattr(world.options, f"{key}_help")),
            "hide_timer": bool(getattr(world.options, f"{key}_hide_timer")),
        }
    return waits


def generate_early(world: BigWalkWorld) -> None:
    # Only the mod shows this name; the item stays "Gourd" for Archipelago.
    world.gourd_name = gourd_name.clean(world.options.gourd_name.value, world.player_name)

    # `linear_keys` puts the Tunnels on the Yellow key's deposit: no doors to shuffle then.
    if world.options.linear_keys and world.options.tunnel_sanity:
        logging.warning(f"Big Walk ({world.player_name}): tunnel_sanity is off with linear_keys, whose Yellow key "
                        f"opens every tunnel door at once.")
        world.options.tunnel_sanity.value = 0

    # The first four colors drawn paint the four flare guns.
    world.color_palette = []
    if world.options.random_colors:
        count = world.random.randint(5, data.PALETTE_SIZE)
        world.color_palette = world.random.sample(range(data.PALETTE_SIZE), count)

    world.towers = data.TOWERS
    world.gourd_count = data.MAX_MONUMENT_SLOTS

    world.peg_tiles = _draw_the_tiles(world)
    world.peg_drawings = {}
    if world.options.custom_drawings:
        own = random.Random(f"{world.multiworld.seed}-{world.player}-drawings")
        world.peg_drawings = tiles.draw_drawings(own, [drawing.key for drawing in tiles.CUSTOM_DRAWINGS])
    if world.options.mirrored_poses.value:
        poses = random.Random(f"{world.multiworld.seed}-{world.player}-poses")
        world.peg_drawings.update(tiles.draw_poses(poses, world.options.mirrored_poses.value))
    world.deposit_amounts = _pick_deposit_amounts(world, world.gourd_count)
    world.cabin_fever = _cabin_fever_waits(world)
    world.deposit_goal = world.options.gourds_required.value
    _check_the_objectives(world)
    monuments.draw_the_tower_order(world)
    monuments.draw_the_monument_keys(world)

    _pick_a_random_puzzle_need(world)
    _ask_for_buttons_early(world)
    opened = world.options.start_with_arch_doors_open.value
    # With `linear_arch_doors` no door is an item; the game's buttons open them.
    world.locked_arch_doors = () if world.options.linear_arch_doors else tuple(
        door for door in data.ARCH_DOORS if door.item_name not in opened)
    _ask_for_an_early_way_out(world)
    _ask_for_the_first_monument_early(world)
    _ask_for_the_far_doors_early(world)
    _hint_the_keys(world)
    _keep_the_gauntlet_stages_local(world)
    if world.options.zone_locks_local:
        world.options.local_items.value |= {lock.item_name for lock in checks.zone_locks(world)}


def _draw_the_tiles(world: BigWalkWorld) -> tuple[str, ...]:
    """Own generator, so the tiles do not change what the rest of the seed draws."""
    mode = world.options.custom_tiles
    if mode == bigwalk_options.CustomTiles.option_off:
        return tiles.VANILLA_TILES
    drawings = [tile.key for tile in tiles.CUSTOM_TILES]
    preferred: list[str] = []
    if mode == bigwalk_options.CustomTiles.option_custom_only:
        candidates = drawings
    elif mode == bigwalk_options.CustomTiles.option_all:
        candidates = [*tiles.VANILLA_TILES, *drawings]
    else:
        matching = tiles.matching(world.multiworld.game.values())
        candidates = [*tiles.VANILLA_TILES, *matching]
        if mode == bigwalk_options.CustomTiles.option_matching_first:
            preferred = matching
    own = random.Random(f"{world.multiworld.seed}-{world.player}-tiles")
    return tiles.draw(own, candidates, world.options.custom_tile_weights.value,
                      sorted(world.options.custom_tile_choices.value), preferred)


def _keep_the_gauntlet_stages_local(world: BigWalkWorld) -> None:
    locked = world.options.gauntlet_mode == bigwalk_options.GauntletMode.option_locked_stages
    if locked and world.options.gauntlet_stages_local:
        world.options.local_items.value |= set(items.GAUNTLET_ITEM_NAMES) | {data.PROGRESSIVE_GAUNTLET_ITEM_NAME}


def _hint_the_keys(world: BigWalkWorld) -> None:
    if world.options.hint_keys != bigwalk_options.HintKeys.option_from_start:
        return
    world.options.start_hints.value |= {data.key_item_name(tower) for tower in world.towers}


def _pick_a_random_puzzle_need(world: BigWalkWorld) -> None:
    """The pick goes into the option, so slot_data carries it and the tracker draws nothing."""
    if not world.options.lock_puzzle_needs:
        return

    candidates = data.tutorial_footholds(world.options.start_with_puzzle_needs.value)
    if candidates and not world.options.start_with_random_puzzle_need:
        # Generation would never end here rather than fail.
        raise OptionError(
            f"Big Walk: {world.player_name} starts with nothing to do. With lock_puzzle_needs on, list "
            "buttons or sync_buttons in start_with_puzzle_needs, or turn start_with_random_puzzle_need "
            "on, so that one of the tutorial's puzzles can be solved from the start.")
    if candidates:
        picked = world.random.choice(candidates)
        world.options.start_with_puzzle_needs.value = set(world.options.start_with_puzzle_needs.value) | {picked.key}


def _ask_for_buttons_early(world: BigWalkWorld) -> None:
    """The most shared puzzle needs come early, or the start has too few locations to fill."""
    if not world.options.lock_puzzle_needs:
        return
    for need in data.EARLY_NEEDS:
        if need.key not in world.options.start_with_puzzle_needs.value:
            world.multiworld.early_items[world.player][need.item_name] = 1


def _ask_for_an_early_way_out(world: BigWalkWorld) -> None:
    """The starting zone holds few checks, so its way out comes as an early local item."""
    world.way_out = ()
    if world.options.start_with_drawbridge_open:
        return
    # With the game's own doors the First Arch Door's button is outside: the Drawbridge it is.
    if world.options.linear_arch_doors:
        if not world.options.linear_keys:
            world.way_out = (data.DRAWBRIDGE_ITEM_NAME,)
        elif not monuments.key_in_its_monument(world, data.TOWERS[0]):
            # The Drawbridge sits on its key's deposit in the tutorial.
            world.way_out = (data.key_item_name(data.TOWERS[0]),)
        else:
            # The tutorial's monument holds the key, so its gourds must be found there.
            gourds = 0 if world.options.linear_puzzles else monuments.key_gourds(world, data.TOWERS[0])
            world.way_out = (data.GOURD_ITEM_NAME,) * gourds
            by_item = world.options.tower_mode == bigwalk_options.TowerMode.option_by_item
            if by_item and not world.options.first_tower_open:
                world.way_out += (data.PROGRESSIVE_MONUMENT_ITEM_NAME,)
    elif data.FIRST_ARCH_DOOR in world.locked_arch_doors:
        world.way_out = (data.FIRST_ARCH_DOOR.item_name if world.options.linear_keys
                        else world.random.choice((data.DRAWBRIDGE_ITEM_NAME, data.FIRST_ARCH_DOOR.item_name)),)
    for name in world.way_out:
        world.multiworld.local_early_items[world.player][name] = world.way_out.count(name)


def _ask_for_the_first_monument_early(world: BigWalkWorld) -> None:
    """Nearly everything waits for this item; left to the fill it can find nowhere to go."""
    if (world.options.tower_mode != bigwalk_options.TowerMode.option_by_item or world.options.first_tower_open
            or data.PROGRESSIVE_MONUMENT_ITEM_NAME in world.way_out):
        return
    world.multiworld.local_early_items[world.player][data.PROGRESSIVE_MONUMENT_ITEM_NAME] = 1


def _ask_for_the_far_doors_early(world: BigWalkWorld) -> None:
    """Skipped when the start is too small: two more early items would make generation fail."""
    if (not world.options.require_arch_doors or data.FIRST_ARCH_DOOR in world.locked_arch_doors
            or world.options.lock_puzzle_needs):
        return

    for door in (data.LEFT_ARCH_DOOR, data.RIGHT_ARCH_DOOR):
        if door in world.locked_arch_doors:
            world.multiworld.local_early_items[world.player][door.item_name] = 1


def _check_the_objectives(world: BigWalkWorld) -> None:
    """The objectives besides the Goal: those listed, or a few drawn (`random_objectives`)."""
    passthrough = getattr(world.multiworld, "re_gen_passthrough", None)
    slot_data = passthrough.get(world.game) if passthrough else None
    if slot_data and "objectives" in slot_data:
        world.objectives = frozenset(slot_data["objectives"]) - {world.options.goal.current_key}
        return

    if (world.options.goal == bigwalk_options.Goal.option_big_wall
            and world.options.gauntlet_mode == bigwalk_options.GauntletMode.option_locked_stages):
        raise OptionError(
            f"Big Walk ({world.player_name}): the goal big_wall ends the run at the chapel, before the Silent "
            f"Gauntlet, whose locked stages would then hold checks for nothing. Set "
            f"gauntlet_mode to vanilla, or the goal to big_goodbye or big_game.")

    listed = {bigwalk_options.OBJECTIVE_ALIASES.get(key, key) for key in world.options.objectives.value}
    endings = sorted(listed & set(bigwalk_options.OBJECTIVE_ENDINGS))
    if endings:
        raise OptionError(
            f"Big Walk ({world.player_name}): {', '.join(endings)} is an ending: it is the goal, not an objective. "
            f"Set `goal` to the ending, and list the other things to do in `objectives`.")

    count = world.options.random_objectives.value
    if count:
        pool = listed or {key for key in bigwalk_options.OBJECTIVE_KEYS
                          if key not in bigwalk_options.OBJECTIVE_ENDINGS and _objective_allowed(world, key)}
        if count > len(pool):
            logging.warning(f"Big Walk ({world.player_name}): random_objectives asks for {count}, "
                            f"only {len(pool)} can be drawn.")
            count = len(pool)
        listed = set(world.random.sample(sorted(pool), count))

    if "big_objects" in listed and world.options.object_sanity == bigwalk_options.ObjectSanity.option_off:
        raise OptionError(
            f"Big Walk ({world.player_name}): the Objectives hold big_objects, whose objects are the Object Sanity "
            f"checks. Set Object Sanity to first_pickup or every.")
    if "big_body" in listed and world.options.lock_abilities == bigwalk_options.LockAbilities.option_disabled:
        raise OptionError(
            f"Big Walk ({world.player_name}): the Objectives hold big_body, whose abilities are the Lock Abilities "
            f"items. Set Lock Abilities to handed or ambidextrous.")
    if "big_radio" in listed and not world.options.radio_sanity:
        raise OptionError(
            f"Big Walk ({world.player_name}): the Objectives hold big_radio, whose stations are the "
            f"Radio Sanity checks. Turn Radio Sanity on.")
    world.objectives = frozenset(listed)


def _objective_allowed(world: BigWalkWorld, key: str) -> bool:
    """Whether the other options let this objective be drawn."""
    if key == "big_radio":
        return bool(world.options.radio_sanity)
    if key == "big_objects":
        return world.options.object_sanity != bigwalk_options.ObjectSanity.option_off
    if key == "big_body":
        return world.options.lock_abilities != bigwalk_options.LockAbilities.option_disabled
    if key == "big_towers":
        return world.options.tower_mode != bigwalk_options.TowerMode.option_all_open
    return True


def _pick_deposit_amounts(world: BigWalkWorld, total_slots: int) -> tuple[int, ...]:
    choice = world.options.gourd_sanity
    if choice == bigwalk_options.GourdSanity.option_off:
        return ()
    if choice == bigwalk_options.GourdSanity.option_every_gourd:
        return tuple(range(1, total_slots + 1))

    # Every fifth deposit, plus the last one in case the total is not a multiple of five.
    milestones = set(range(5, total_slots + 1, 5))
    milestones.add(total_slots)
    return tuple(sorted(milestones))
