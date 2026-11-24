"""Room at the start of the world, and the items of the `local` options."""

from __future__ import annotations

import logging
from typing import TYPE_CHECKING

from BaseClasses import CollectionState, Item, LocationProgressType

from . import checks, data
from . import options as bigwalk_options

if TYPE_CHECKING:
    from .world import BigWalkWorld


def set_local_items_aside(world: BigWalkWorld, pool: list[Item]) -> list[Item]:
    """Keeps back the junk pre_fill places on the checks of the `local` options; returns the rest."""
    world.local_placements = []
    # Universal Tracker never reaches pre_fill.
    if getattr(world.multiworld, "generation_is_fake", False):
        return pool
    groups = (("object_sanity_items", checks.object_location_names(world)),
              ("light_sanity_items", [light.location_name for light in checks.lights(world)]))
    # Archipelago takes these out of the itempool, so they must be in it.
    asked_for = set(world.options.start_inventory_from_pool.value)
    asked_for.update(name for block in world.options.plando_items.value for name in block.items)
    for option, names in groups:
        if getattr(world.options, option).value != bigwalk_options.ObjectSanityItems.option_local:
            continue
        own = [item for item in pool if not item.advancement and item.name not in asked_for]
        world.random.shuffle(own)
        if len(own) < len(names):
            logging.warning(f"Big Walk ({world.player_name}): {option} local: only {len(own)} items of this "
                            f"world for {len(names)} checks; the others may hold anything.")
        own = own[:len(names)]
        world.local_placements.append((names, own))
        # By identity: items compare equal by name and player.
        kept = {id(item) for item in own}
        pool = [item for item in pool if id(item) not in kept]
    return pool


def make_room_at_the_start(world: BigWalkWorld) -> None:
    """When the starting zone cannot hold its early items, the player starts with some."""
    if getattr(world.multiworld, "generation_is_fake", False):
        return
    local_early = world.multiworld.local_early_items[world.player]
    mine = [item for item in world.multiworld.itempool if item.player == world.player]
    if any(item.name == data.LEGS_ITEM_NAME for item in mine):
        local_early[data.LEGS_ITEM_NAME] = 1
    # Given up from the end, Legs first. By identity: items compare equal by name and player.
    needed: list[Item] = []
    monument = () if data.PROGRESSIVE_MONUMENT_ITEM_NAME in world.way_out else (data.PROGRESSIVE_MONUMENT_ITEM_NAME,)
    for name in (*world.way_out, *monument, data.LEGS_ITEM_NAME):
        item = next((item for item in mine if item.name == name and all(item is not n for n in needed)), None)
        if item is not None and name in local_early:
            needed.append(item)
    if not needed:
        return

    # The early fill's own reach: start items and the events they open.
    state = CollectionState(world.multiworld)
    state.sweep_for_advancements(locations=[loc for loc in world.multiworld.get_filled_locations(world.player)
                                            if loc.address is None])
    closed = {name for names, _ in world.local_placements for name in names} | world.options.exclude_locations.value
    room = sum(1 for loc in world.multiworld.get_unfilled_locations(world.player)
               if loc.name not in closed and loc.progress_type != LocationProgressType.EXCLUDED
               and loc.can_reach(state))
    if room < sum(local_early.values()):
        for name in [name for name in local_early if all(item.name != name for item in needed)]:
            del local_early[name]
    while len(needed) > room:
        item = needed.pop()
        logging.warning(f"Big Walk ({world.player_name}): no room in the starting zone for {item.name}; "
                        f"starting with it.")
        local_early[item.name] -= 1
        if not local_early[item.name]:
            del local_early[item.name]
        world.multiworld.itempool[:] = [other for other in world.multiworld.itempool if other is not item]
        world.push_precollected(item)
        world.multiworld.itempool.append(world.create_filler())  # the location it leaves


def place_local_items(world: BigWalkWorld) -> None:
    """Places the items set aside for the `local` options, where plando left room."""
    for names, own in world.local_placements:
        free = [location for location in world.multiworld.get_unfilled_locations(world.player)
                if location.name in names]
        for location, item in zip(free, own):
            location.place_locked_item(item)


def local_items_left(world: BigWalkWorld) -> list[Item]:
    return [item for _, own in world.local_placements for item in own if item.location is None]
