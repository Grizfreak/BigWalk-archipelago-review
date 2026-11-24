"""pre_fill: local items and the room at the start."""

from __future__ import annotations

import logging
from typing import TYPE_CHECKING

from BaseClasses import CollectionState, Item, LocationProgressType

from . import data, locations
from . import options as bigwalk_options

if TYPE_CHECKING:
    from .world import BigWalkWorld


def pre_fill(world: BigWalkWorld) -> None:
    # Local items first: they may take the starting zone's places, which Legs must then see.
    _place_local_items(world, "Objects", world.options.object_sanity_items.value, "object_sanity_items")
    _place_local_items(world, "Lights", world.options.light_sanity_items.value, "light_sanity_items")
    _make_room_at_the_start(world)
    _open_the_way_one_step_at_a_time(world)


def _open_the_way_one_step_at_a_time(world: BigWalkWorld) -> None:
    """Where two items are needed with one free location, the player starts with one of them."""
    mine = [item for item in world.multiworld.itempool if item.player == world.player and item.advancement]
    if not mine:
        return
    own = world.multiworld.get_locations(world.player)
    state = CollectionState(world.multiworld)
    state.sweep_for_advancements(own)
    used = 0

    def reach(with_state: CollectionState) -> set[str]:
        return {loc.name for loc in world.multiworld.get_unfilled_locations(world.player)
                if loc.progress_type != LocationProgressType.EXCLUDED and loc.can_reach(with_state)}

    def opens(names: tuple[str, ...], now: set[str]) -> int:
        trial = state.copy()
        for name in names:
            trial.collect(world.create_item(name), True)
        trial.sweep_for_advancements(own)
        return len(reach(trial) - now)

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
            logging.warning(f"Big Walk ({world.player_name}): {' and '.join(chosen)} needed with room for "
                            f"{max(free, 0)}; starting with {item.name}.")
            mine.remove(item)
            world.multiworld.itempool[:] = [other for other in world.multiworld.itempool if other is not item]
            local_early = world.multiworld.local_early_items[world.player]
            if local_early.get(item.name):
                local_early[item.name] -= 1
                if not local_early[item.name]:
                    del local_early[item.name]
            world.push_precollected(item)
            world.multiworld.itempool.append(world.create_filler())
            state.collect(item, True)
            state.sweep_for_advancements(own)
            continue
        for name in chosen:
            item = next(item for item in mine if item.name == name)
            mine.remove(item)
            state.collect(item, True)
            used += 1
        state.sweep_for_advancements(own)


def _make_room_at_the_start(world: BigWalkWorld) -> None:
    """When the starting zone cannot hold its early items, the player starts with some."""
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
    room = sum(1 for loc in world.multiworld.get_unfilled_locations(world.player)
               if loc.progress_type != LocationProgressType.EXCLUDED and loc.can_reach(state))
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


def _place_local_items(world: BigWalkWorld, group: str, contents: int, option: str) -> None:
    """Fills these checks with this world's non-progression items before the main fill."""
    if contents != bigwalk_options.ObjectSanityItems.option_local:
        return
    names = locations.LOCATION_NAME_GROUPS[group]
    spots = [loc for loc in world.multiworld.get_unfilled_locations(world.player) if loc.name in names]
    own = [item for item in world.multiworld.itempool if item.player == world.player and not item.advancement]
    world.random.shuffle(own)
    if len(own) < len(spots):
        logging.warning(f"Big Walk ({world.player_name}): {option} local: only {len(own)} items of this "
                        f"world for {len(spots)} checks; the others may hold anything.")
    placed = set()
    for location, item in zip(spots, own):
        location.place_locked_item(item)
        placed.add(id(item))
    # By identity: items compare equal by name and player, so remove() may take another copy.
    world.multiworld.itempool[:] = [item for item in world.multiworld.itempool if id(item) not in placed]
