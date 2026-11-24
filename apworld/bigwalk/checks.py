"""Which checks and counts a seed has, and the slot_data helpers that use them."""

from __future__ import annotations

from collections import Counter
from typing import TYPE_CHECKING

from . import data
from . import options as bigwalk_options

if TYPE_CHECKING:
    from .world import BigWalkWorld


def objective_keys(world: BigWalkWorld) -> tuple[str, ...]:
    """The objectives and the Goal, in the frozen order."""
    listed = world.objectives | {world.options.goal.current_key}
    return tuple(key for key in bigwalk_options.OBJECTIVE_KEYS if key in listed)


def hint_items(world: BigWalkWorld) -> dict[str, int]:
    """Items `!hint` can find, by name and count; start items are left out."""
    placed = [location.item for location in world.multiworld.get_locations() if location.item is not None]
    counts = Counter(item.name for item in {id(item): item for item in [*world.multiworld.itempool, *placed]}.values()
                     if item.player == world.player and item.code is not None)
    return dict(sorted(counts.items()))


def death_link_roulette(world: BigWalkWorld) -> dict[str, int]:
    weights = world.options.death_link_trap_weights.value
    allow_hard = bool(world.options.death_link_roulette_hard_traps)
    return {
        trap.key: weights[trap.item_name]
        for trap in data.TRAPS
        if weights.get(trap.item_name, 0) > 0 and (allow_hard or not trap.hard)
    }


def puzzles(world: BigWalkWorld) -> list[data.Puzzle]:
    """The puzzles that are checks."""
    puzzles = list(data.PUZZLES)
    if world.options.include_big_game_puzzles == bigwalk_options.IncludeBigGamePuzzles.option_disabled:
        puzzles = [p for p in puzzles if p.prop_name not in data.CHAIRLIFT_PUZZLES]
    return puzzles


def lights(world: BigWalkWorld) -> list[data.Light]:
    """The lights that are checks (`light_sanity`)."""
    if world.options.light_sanity == bigwalk_options.LightSanity.option_off:
        return []
    return lights_counted(world)


def lights_counted(world: BigWalkWorld) -> list[data.Light]:
    """The lights `big_lights` counts; with `light_sanity` off, the lamps, without checks."""
    mode = world.options.light_sanity
    if mode == bigwalk_options.LightSanity.option_off and "big_lights" not in world.objectives:
        return []
    return [light for light in data.LIGHTS
            if not light.switch or mode == bigwalk_options.LightSanity.option_lamps_and_switches]


def object_location_names(world: BigWalkWorld) -> list[str]:
    """Object Sanity's checks: every object counted, or every kind counted."""
    if world.options.object_sanity == bigwalk_options.ObjectSanity.option_every:
        return [obj.location_name for obj in data.ISLAND_OBJECTS if object_kind_counted(world, obj.kind)]
    if world.options.object_sanity == bigwalk_options.ObjectSanity.option_first_pickup:
        return [data.object_kind_location_name(label) for kind, label in data.OBJECT_KINDS
                if object_kind_counted(world, kind)]
    return []


def objects_target(world: BigWalkWorld) -> int:
    """`objects_required` percent of the object checks, rounded up, at least one."""
    count = len(object_location_names(world))
    return max(1, -(-count * world.options.objects_required.value // 100)) if count else 0


def lights_target(world: BigWalkWorld) -> int:
    """`lights_required` percent of the lights counted, rounded up, at least one."""
    lights = len(lights_counted(world))
    return max(1, -(-lights * world.options.lights_required.value // 100)) if lights else 0


def zone_locks(world: BigWalkWorld) -> list[data.ZoneLock]:
    """The zone items of this seed."""
    if not world.options.zone_locks:
        return []
    objects = world.options.object_sanity != bigwalk_options.ObjectSanity.option_off
    lamps = world.options.light_sanity != bigwalk_options.LightSanity.option_off or objects and any(
        object_kind_counted(world, kind) for kind in data.LAMP_KINDS)
    gadgets = objects and any(object_kind_counted(world, kind) for kind, _ in data.OBJECT_KINDS
                              if kind not in data.LAMP_KINDS)
    return [lock for lock in data.ZONE_LOCKS if (lamps if lock.lamps else gadgets)]


def object_kind_counted(world: BigWalkWorld, kind: str) -> bool:
    """With `light_sanity` the lamps count by their light, so no lamp is an object."""
    if kind in data.LAMP_KINDS and world.options.light_sanity != bigwalk_options.LightSanity.option_off:
        return False
    return data.OBJECT_KIND_KEYS[kind] in world.options.object_sanity_kinds.value
