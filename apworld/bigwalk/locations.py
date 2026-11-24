"""Location definitions for the Big Walk world."""

from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Location

from . import checks, data, monuments, regions
from .items import BigWalkItem
from .options import CutKeySanity, GauntletMode, Goal, ObjectSanity

if TYPE_CHECKING:
    from .world import BigWalkWorld


# Every location any options could create: Archipelago reads it before options are known.
LOCATION_NAME_TO_ID: dict[str, int] = {
    **{puzzle.location_name: data.puzzle_location_id(puzzle) for puzzle in data.PUZZLES},
    **{tower.location_name: data.key_deposit_location_id(tower) for tower in data.TOWERS},
    **{station.location_name: data.radio_location_id(station) for station in data.RADIO_STATIONS},
    **{data.cut_location_name(tower, index): data.cut_location_id(tower, index)
       for tower in data.TOWERS for index in range(tower.segments)},
    **{data.deposit_location_name(amount): data.deposit_location_id(amount)
       for amount in range(1, data.MAX_MONUMENT_SLOTS + 1)},
    **{stage.location_name: data.gauntlet_stage_id(stage) for stage in data.GAUNTLET_STAGES},
    **{pickup.location_name: data.pickup_location_id(i) for i, pickup in enumerate(data.PICKUPS)},
    **{firework.location_name: data.firework_location_id(i) for i, firework in enumerate(data.FIREWORKS)},
    **{pickup.location_name: data.flare_gun_location_id(i) for i, pickup in enumerate(data.FLARE_GUN_PICKUPS)},
    **{data.tower_reached_location_name(tower): data.tower_reached_location_id(tower)
       for tower in data.reached_towers()},
    **{obj.location_name: data.object_location_id(obj) for obj in data.ISLAND_OBJECTS},
    **{data.monument_full_location_name(tower): data.monument_full_location_id(tower) for tower in data.TOWERS},
    **{data.monument_key_location_name(tower): data.monument_key_location_id(tower) for tower in data.TOWERS},
    **{light.location_name: data.light_location_id(light) for light in data.LIGHTS},
    **{data.object_kind_location_name(label): data.object_kind_location_id(i)
       for i, (_, label) in enumerate(data.OBJECT_KINDS)},
}

LOCATION_NAME_GROUPS: dict[str, set[str]] = {
    "Puzzles": {puzzle.location_name for puzzle in data.PUZZLES},
    "Key Deposits": {tower.location_name for tower in data.TOWERS},
    "Key Cuts": {data.cut_location_name(tower, index)
                 for tower in data.TOWERS for index in range(tower.segments)},
    "Radio Stations": {station.location_name for station in data.RADIO_STATIONS},
    "Gourd Deposits": {data.deposit_location_name(amount)
                       for amount in range(1, data.MAX_MONUMENT_SLOTS + 1)},
    "Gauntlet": {stage.location_name for stage in data.GAUNTLET_STAGES},
    "Packs": {pickup.location_name for pickup in data.PICKUPS},
    "Fireworks": {firework.location_name for firework in data.FIREWORKS},
    "Flare Guns": {pickup.location_name for pickup in data.FLARE_GUN_PICKUPS},
    "Towers Reached": {data.tower_reached_location_name(tower) for tower in data.reached_towers()},
    "Lights": {light.location_name for light in data.LIGHTS},
    "Monuments Full": {data.monument_full_location_name(tower) for tower in data.TOWERS},
    "Monument Keys": {data.monument_key_location_name(tower) for tower in data.TOWERS},
    "Objects": {obj.location_name for obj in data.ISLAND_OBJECTS}
               | {data.object_kind_location_name(label) for _, label in data.OBJECT_KINDS},
}

OBJECT_ZONE_REGIONS: dict[str, str] = {
    "start": regions.STARTING_ZONE,
    "chairlift": regions.CHAIRLIFT_ZONE,
    "purple": regions.CHAIRLIFT_ZONE,
    "tunnels": regions.TUNNEL_ZONE,
    "big_wall": regions.ENDING_ZONE,
}
"""An island object's region by its zone; other zones are in the overworld."""

VICTORY_EVENT_NAME = "Victory"


class BigWalkLocation(Location):
    game = "Big Walk"


def gated_regions() -> dict[str, str]:
    """Location name -> region for locations behind a big key; the rest are in the overworld."""
    mapping = {name: regions.CHAIRLIFT_ZONE for name in data.chairlift_locations()}
    mapping.update({name: regions.TUNNEL_ZONE for name in data.tunnel_locations()})
    mapping.update({name: regions.STARTING_ZONE for name in starting_zone_locations()})
    mapping.update({stage.location_name: regions.ENDING_ZONE for stage in data.GAUNTLET_STAGES})
    mapping.update({pickup.location_name: regions.CHAIRLIFT_ZONE for pickup in data.PICKUPS if pickup.past_chairlift})
    mapping.update({pickup.location_name: regions.TUNNEL_ZONE for pickup in data.PICKUPS if pickup.past_tunnels})
    mapping.update({firework.location_name: regions.CHAIRLIFT_ZONE
                    for firework in data.FIREWORKS if firework.past_chairlift})
    mapping.update({obj.location_name: OBJECT_ZONE_REGIONS[obj.zone]
                    for obj in data.ISLAND_OBJECTS if obj.zone in OBJECT_ZONE_REGIONS})
    # A kind with one in the tutorial (the cowbells) is a starting-zone check.
    mapping.update({data.object_kind_location_name(label): regions.STARTING_ZONE
                    for kind, label in data.OBJECT_KINDS
                    if any(obj.kind == kind and obj.zone == "start" for obj in data.ISLAND_OBJECTS)})
    return mapping


def starting_zone_locations() -> list[str]:
    """Locations reachable from the hub without the drawbridge or the first arch door."""
    by_prop = {puzzle.prop_name: puzzle.location_name for puzzle in data.PUZZLES}
    drawbridge = next(tower for tower in data.TOWERS if tower.item_name == data.DRAWBRIDGE_ITEM_NAME)
    names = [by_prop[prop_name] for prop_name in data.START_ZONE_PUZZLES]
    names += list(data.cut_locations(drawbridge))
    names.append(drawbridge.location_name)
    names.append(data.tower_reached_location_name(drawbridge))
    names += [data.deposit_location_name(amount) for amount in range(1, drawbridge.slots + 1)]
    return names


def create_all_locations(world: BigWalkWorld) -> None:
    gated = gated_regions()
    if world.tower_order:
        # The tutorial's monument comes first only if the order says so.
        for amount in range(1, data.MAX_MONUMENT_SLOTS + 1):
            name = data.deposit_location_name(amount)
            gated.pop(name, None)
            if world.tower_order[monuments.tower_count_needing(world, amount) - 1] == "bigKeyIntro":
                gated[name] = regions.STARTING_ZONE

    def place(location_names: list[str]) -> None:
        for name in location_names:
            region = world.get_region(gated.get(name, regions.OVERWORLD))
            region.add_locations({name: LOCATION_NAME_TO_ID[name]}, BigWalkLocation)

    place([puzzle.location_name for puzzle in checks.puzzles(world)])

    # Towers are all open: the gourd count in rules.py gates the deposits, not the region.
    place([tower.location_name for tower in world.towers])

    # Every cutting trail is reachable without a big key.
    if world.options.cut_key_sanity != CutKeySanity.option_off:
        place([name for tower in world.towers for name in data.cut_locations(tower)])

    if world.options.radio_sanity:
        place([station.location_name for station in data.RADIO_STATIONS])

    place([data.deposit_location_name(amount) for amount in world.deposit_amounts])

    if world.options.gauntlet_mode == GauntletMode.option_locked_stages:
        place([stage.location_name for stage in data.GAUNTLET_STAGES])

    if world.options.pack_sanity:
        place([pickup.location_name for pickup in data.PICKUPS])

    if world.options.firework_sanity:
        place([firework.location_name for firework in data.FIREWORKS])

    if world.options.flare_gun_sanity:
        place([pickup.location_name for pickup in data.FLARE_GUN_PICKUPS])

    if world.options.tower_sanity:
        place([data.tower_reached_location_name(tower) for tower in data.reached_towers()])

    # Each light is also an event for big_lights, even when light_sanity gives it no check.
    light_checks = {light.key for light in checks.lights(world)}
    for light in checks.lights_counted(world):
        region = world.get_region(OBJECT_ZONE_REGIONS.get(light.zone, regions.OVERWORLD))
        if light.key in light_checks:
            region.add_locations({light.location_name: LOCATION_NAME_TO_ID[light.location_name]}, BigWalkLocation)
        region.add_event(data.light_event_name(light), data.LIGHT_EVENT_ITEM_NAME,
                         location_type=BigWalkLocation, item_type=BigWalkItem)

    # Like its deposits, the tutorial monument's checks are in the starting zone if it fills first.
    full = [(data.monument_full_location_name(tower), tower) for tower in monuments.monuments_full(world)]
    keys = [(data.monument_key_location_name(tower), tower) for tower in monuments.monument_keys(world)]
    for name, tower in full + keys:
        in_tutorial = tower is data.TOWERS[0] and monuments.tutorial_monument_first(world)
        region = regions.STARTING_ZONE if in_tutorial else regions.OVERWORLD
        world.get_region(region).add_locations({name: LOCATION_NAME_TO_ID[name]}, BigWalkLocation)

    place(checks.object_location_names(world))
    # big_objects counts events beside them: the logic cannot count reachable locations.
    if "big_objects" in world.objectives:
        for name in checks.object_location_names(world):
            world.get_location(name).parent_region.add_event(
                data.object_event_name(name), data.OBJECT_EVENT_ITEM_NAME,
                location_type=BigWalkLocation, item_type=BigWalkItem)

    create_victory_event(world)


def create_victory_event(world: BigWalkWorld) -> None:
    """The goal is an event: the mod reports it with a GOAL ClientStatus, not a location."""
    goal = world.options.goal
    if world.objectives:
        # Several objectives: rules.set_completion_rule asks for each one's place or count.
        region_name = regions.OVERWORLD
    elif goal == Goal.option_big_collection:
        region_name = regions.OVERWORLD
    elif goal == Goal.option_big_game:
        region_name = regions.GREEN_DOME_ZONE
    else:
        region_name = regions.ENDING_ZONE

    world.get_region(region_name).add_event(
        VICTORY_EVENT_NAME,
        VICTORY_EVENT_NAME,
        location_type=BigWalkLocation,
        item_type=BigWalkItem,
    )
