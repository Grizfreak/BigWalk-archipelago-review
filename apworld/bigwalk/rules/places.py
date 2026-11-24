"""Puzzle, object, firework and light rules."""

from __future__ import annotations

from typing import TYPE_CHECKING

from rule_builder.rules import CanReachRegion, Has, HasAll, Or, Rule

from .. import checks, data, locations, regions
from .. import options as bigwalk_options
from .common import (abilities_locked, all_of, body_rule, past_far_door, place_rule, purple_hole, with_all,
                     with_zone)
from .junk import exclude_as_junk
from .towers import tower_open

if TYPE_CHECKING:
    from ..world import BigWalkWorld


def set_puzzle_rules(world: BigWalkWorld) -> None:
    """With `lock_puzzle_needs`, a puzzle is in logic once every item it is built from arrived."""
    for puzzle in checks.puzzles(world):
        rules: list[Rule] = []
        if world.options.lock_puzzle_needs:
            required = [need.item_name for need in data.puzzle_needs(puzzle)]
            if required:
                rules.append(HasAll(*required))
        body = body_rule(world, data.ABILITY_PUZZLES.get(puzzle.prop_name, data.Body()))
        if body is not None:
            rules.append(body)
        # The purple hole (SouthPeakTunnel) splits the chairlift zone; the Tunnels open it.
        if puzzle.prop_name in data.CHAIRLIFT_PUZZLES:
            rules.append(purple_hole(world))
        # `when_unlocked`: the purple puzzles open once an ending is reached, as in the game.
        when_unlocked = bigwalk_options.IncludeBigGamePuzzles.option_when_unlocked
        if puzzle.prop_name in data.CHAIRLIFT_PUZZLES and world.options.include_big_game_puzzles == when_unlocked:
            rules.append(CanReachRegion(regions.ENDING_ZONE))
        if rules:
            world.set_rule(world.get_location(puzzle.location_name), all_of(rules))


def into_the_tunnels(world: BigWalkWorld) -> Rule:
    """The Tunnels, or with `tunnel_sanity` any outlet's door and the way to its tower."""
    if not world.options.tunnel_sanity:
        return past_far_door(world, data.ZONE_FAR_DOOR["tunnels"], Has(data.YELLOW_TWIST.item_name))
    ways = [past_far_door(world, data.TOWER_FAR_DOOR.get(door.tower), Has(door.item_name))
            for door in data.TUNNEL_DOORS if door.tower is not None]
    rule = ways[0]
    for way in ways[1:]:
        rule = rule | way
    return rule


def object_rule(world: BigWalkWorld, obj: data.IslandObject) -> Rule | None:
    """What an island object asks for beyond its region (locations.OBJECT_ZONE_REGIONS)."""
    lamp = obj.kind in data.LAMP_KINDS
    rule = place_rule(world, obj.zone)
    # A tower's lamps stand at its foot; its other objects are on the roof, past the opening button.
    if obj.zone.startswith("tower:") and not lamp:
        rule = with_all(rule, tower_open(world, obj.zone[len("tower:"):]))
    for body_need in (data.OBJECT_ABILITIES.get(obj.kind), data.OBJECT_GUID_ABILITIES.get(obj.guid)):
        body = body_rule(world, body_need) if body_need is not None else None
        if body is not None:
            rule = with_all(rule, [body])
    return with_zone(world, rule, obj.guid, obj.zone, lamp)


def set_firework_rules(world: BigWalkWorld) -> None:
    """A tower's fireworks are on its roof, past its opening button."""
    if not world.options.firework_sanity:
        return
    for firework in data.FIREWORKS:
        if firework.tower is None:
            continue
        rule = with_all(place_rule(world, f"tower:{firework.tower}"), tower_open(world, firework.tower))
        if rule is not None:
            world.set_rule(world.get_location(firework.location_name), rule)


def light_rule(world: BigWalkWorld, light: data.Light) -> Rule | None:
    """What a light asks for beyond its region; the train's lights need the Train."""
    if light.zone == "train":
        train: Rule = Has(data.tower_by_prop("bigKeyBlueZone").item_name)
        # With `rides_require_seat` the train's seats are locked: no ride to its cars before Seat.
        if world.options.rides_require_seat and abilities_locked(world):
            train = train & Has(data.SEAT_ITEM_NAME)
        return train
    return with_zone(world, place_rule(world, light.zone), light.key, light.zone, True)


def set_light_rules(world: BigWalkWorld) -> None:
    light_checks = {light.key for light in checks.lights(world)}
    for light in checks.lights_counted(world):
        rule = light_rule(world, light)
        if rule is None:
            continue
        if light.key in light_checks:
            world.set_rule(world.get_location(light.location_name), rule)
        world.set_rule(world.get_location(data.light_event_name(light)), rule)


def set_object_event_rules(world: BigWalkWorld) -> None:
    """big_objects' events: each asks what its object asks."""
    if "big_objects" not in world.objectives:
        return
    for name in checks.object_location_names(world):
        world.get_location(data.object_event_name(name)).access_rule = world.get_location(name).access_rule


def restrict_object_items(world: BigWalkWorld) -> None:
    """`object_sanity_items` and `light_sanity_items`: those checks hold junk or local items."""
    for group, contents, percentage, option in (
            ("Objects", world.options.object_sanity_items.value, world.options.object_sanity_junk_percentage.value,
             "object_sanity_items"),
            ("Lights", world.options.light_sanity_items.value, world.options.light_sanity_junk_percentage.value,
             "light_sanity_items")):
        if contents == bigwalk_options.ObjectSanityItems.option_local:
            continue
        names = locations.LOCATION_NAME_GROUPS[group]
        chosen = [location for location in world.multiworld.get_locations(world.player) if location.name in names]
        if contents == bigwalk_options.ObjectSanityItems.option_any:
            chosen = world.random.sample(chosen, round(len(chosen) * percentage / 100))
        exclude_as_junk(world, chosen, f"{option} junk")


def set_object_rules(world: BigWalkWorld) -> None:
    """`object_sanity`: each object by its own rule, or its kind's easiest for `first_pickup`."""
    mode = world.options.object_sanity
    restrict_object_items(world)
    if mode == bigwalk_options.ObjectSanity.option_every:
        for obj in data.ISLAND_OBJECTS:
            if not checks.object_kind_counted(world, obj.kind):
                continue
            rule = object_rule(world, obj)
            if rule is not None:
                world.set_rule(world.get_location(obj.location_name), rule)
    elif mode == bigwalk_options.ObjectSanity.option_first_pickup:
        for kind, label in data.OBJECT_KINDS:
            if not checks.object_kind_counted(world, kind):
                continue
            objects = [obj for obj in data.ISLAND_OBJECTS if obj.kind == kind]
            # Free: one in the tutorial or the overworld that asks for nothing more.
            if any(obj.zone not in locations.OBJECT_ZONE_REGIONS and object_rule(world, obj) is None
                   or obj.zone == "start" for obj in objects):
                continue
            ways: list[Rule] = []
            for obj in objects:
                way: Rule | None = object_rule(world, obj)
                region = locations.OBJECT_ZONE_REGIONS.get(obj.zone)
                if region is not None:
                    way = CanReachRegion(region) if way is None else CanReachRegion(region) & way
                if way is not None:
                    ways.append(way)
            world.set_rule(world.get_location(data.object_kind_location_name(label)), Or(*ways))
