"""Regions of the Big Walk world: one open map plus the zones behind big keys and doors."""

from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Region

from . import data

if TYPE_CHECKING:
    from .world import BigWalkWorld


STARTING_ZONE = "Starting Zone"
OVERWORLD = "Big Walk"
ENDING_ZONE = "Ending Zone"
CHAIRLIFT_ZONE = "Past the Chairlift"
TUNNEL_ZONE = "Past the Tunnels"
GREEN_DOME_ZONE = "Past the Spawn Secret Door"

STARTING_EXIT = f"{STARTING_ZONE} to {OVERWORLD}"
ENDING_ENTRANCE = f"{OVERWORLD} to {ENDING_ZONE}"
CHAIRLIFT_ENTRANCE = f"{OVERWORLD} to {CHAIRLIFT_ZONE}"
TUNNEL_ENTRANCE = f"{OVERWORLD} to {TUNNEL_ZONE}"
GREEN_DOME_ENTRANCE = f"{OVERWORLD} to {GREEN_DOME_ZONE}"


def create_and_connect_regions(world: BigWalkWorld) -> None:
    starting_zone = Region(STARTING_ZONE, world.player, world.multiworld)
    overworld = Region(OVERWORLD, world.player, world.multiworld)
    ending_zone = Region(ENDING_ZONE, world.player, world.multiworld)
    chairlift_zone = Region(CHAIRLIFT_ZONE, world.player, world.multiworld)
    tunnel_zone = Region(TUNNEL_ZONE, world.player, world.multiworld)
    green_dome_zone = Region(GREEN_DOME_ZONE, world.player, world.multiworld)
    world.multiworld.regions += [starting_zone, overworld, ending_zone, chairlift_zone, tunnel_zone, green_dome_zone]

    starting_zone.connect(overworld, STARTING_EXIT)
    overworld.connect(ending_zone, ENDING_ENTRANCE)
    overworld.connect(chairlift_zone, CHAIRLIFT_ENTRANCE)
    overworld.connect(tunnel_zone, TUNNEL_ENTRANCE)
    overworld.connect(green_dome_zone, GREEN_DOME_ENTRANCE)
