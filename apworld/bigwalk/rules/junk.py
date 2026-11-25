"""Junk exclusions shared by the `*_sanity: junk` options."""

from __future__ import annotations

import logging
from typing import TYPE_CHECKING

from BaseClasses import LocationProgressType

from .. import data

if TYPE_CHECKING:
    from ..world import BigWalkWorld


def set_pickup_junk(world: BigWalkWorld) -> None:
    """`pack_sanity: junk` and `flare_gun_sanity: junk`: their checks hold junk."""
    for option, pickups in (("pack_sanity", data.PICKUPS), ("flare_gun_sanity", data.FLARE_GUN_PICKUPS)):
        if getattr(world.options, option).current_key == "junk":
            exclude_as_junk(world, [world.get_location(pickup.location_name) for pickup in pickups], f"{option} junk")


def exclude_as_junk(world: BigWalkWorld, chosen: list, what: str) -> None:
    """Excludes these locations, within one junk budget shared by every junk option."""
    budget = world.__dict__.get("junk_budget")
    if budget is None:
        budget = sum(1 for item in world.multiworld.itempool
                     if item.player == world.player and not item.advancement and not item.useful)
    if budget < len(chosen):
        logging.warning(f"Big Walk ({world.player_name}): {what}: only {budget} junk items left for "
                        f"{len(chosen)} locations; the others may hold anything.")
        chosen = world.random.sample(chosen, budget)
    room = room_for_progression(world)
    if room < len(chosen):
        logging.warning(f"Big Walk ({world.player_name}): {what}: excluding {len(chosen)} locations would leave too "
                        f"few for this world's progression; {room} excluded, the others may hold anything.")
        chosen = world.random.sample(chosen, room)
    for location in chosen:
        location.progress_type = LocationProgressType.EXCLUDED
    world.__dict__["junk_budget"] = budget - len(chosen)


PROGRESSION_MARGIN = 5
"""Locations kept free beyond progression: half the progression count, at least this many."""


def room_for_progression(world: BigWalkWorld) -> int:
    """How many more locations the junk options may exclude and still fit the progression."""
    local_names = {name for names, _ in world.local_placements for name in names}
    open_locations = sum(1 for location in world.multiworld.get_locations(world.player)
                         if location.address is not None and location.item is None
                         and location.progress_type != LocationProgressType.EXCLUDED
                         and location.name not in local_names)
    progression = sum(1 for item in world.multiworld.itempool if item.player == world.player and item.advancement)
    return max(0, open_locations - progression - max(PROGRESSION_MARGIN, progression // 2))
