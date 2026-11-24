"""Gauntlet, entrance and completion rules."""

from __future__ import annotations

from typing import TYPE_CHECKING

from itertools import combinations

from rule_builder.rules import And, CanReachLocation, HasFromList, CanReachRegion, Has, HasAll, HasAny, Or, Rule

from .. import checks, data, items, locations, monuments, regions
from ..options import GauntletMode, GauntletStageItems, Goal
from .common import abilities_locked, gauntlet_body_through, past_far_door
from .places import into_the_tunnels
from .towers import monuments_up_to

if TYPE_CHECKING:
    from ..world import BigWalkWorld


def gauntlet_parts_are_items(world: BigWalkWorld) -> bool:
    """Whether the Gauntlet's stages ask for the parts their puzzles are built from."""
    return bool(world.options.lock_puzzle_needs and world.options.lock_gauntlet_needs)


def stairways_up_to(world: BigWalkWorld, count: int) -> list[Rule]:
    """The stairways of the first `count` stages: that many progressive doors, or those named."""
    if count <= 0:
        return []
    if world.options.gauntlet_stage_items == GauntletStageItems.option_progressive:
        return [Has(data.PROGRESSIVE_GAUNTLET_ITEM_NAME, count=count)]
    return [HasAll(*[stage.item_name for stage in data.GAUNTLET_STAGES[:count]])]


def set_gauntlet_rules(world: BigWalkWorld) -> None:
    """With `locked_stages`, a stage's puzzle needs the stairways (and parts) before it."""
    if world.options.gauntlet_mode != GauntletMode.option_locked_stages:
        return

    for stage in data.GAUNTLET_STAGES:
        rules = stairways_up_to(world, stage.index)
        body = gauntlet_body_through(world, stage.index + 1)
        if body is not None:
            rules.append(body)
        if gauntlet_parts_are_items(world):
            needs = (data.gauntlet_needs_through(stage) if world.options.gauntlet_puzzles_required
                     else data.gauntlet_needs_of(stage))
            if needs:
                rules.append(HasAll(*[need.item_name for need in needs]))
        if rules:
            rule = rules[0]
            for more in rules[1:]:
                rule = rule & more
            world.set_rule(world.get_location(stage.location_name), rule)


def set_entrance_rules(world: BigWalkWorld) -> None:
    # Placing `bigKeyBoss` in `bigKeyPlinthEnding` opens the chapel and everything past it.
    world.set_rule(
        world.get_entrance(regions.ENDING_ENTRANCE),
        past_far_door(world, data.ZONE_FAR_DOOR["ending"], Has(data.BLACK_MONOLITH.item_name)),
    )

    # The chairlift and the tunnels are doors: their keys must not land behind them.
    chairlift: Rule = Has(data.GREEN_CUP.item_name)
    # `rides_require_seat`: the mod locks the chairlift seat like any other.
    if world.options.rides_require_seat and abilities_locked(world):
        chairlift = chairlift & Has(data.SEAT_ITEM_NAME)
    world.set_rule(
        world.get_entrance(regions.CHAIRLIFT_ENTRANCE),
        past_far_door(world, data.ZONE_FAR_DOOR["chairlift"], chairlift),
    )
    world.set_rule(
        world.get_entrance(regions.TUNNEL_ENTRANCE),
        into_the_tunnels(world),
    )

    # Gated only while the first arch door stays closed; otherwise the mod opens it.
    exit_rule: Rule | None = None
    if world.options.linear_arch_doors:
        # The First Arch Door's button is outside, so the way out is the drawbridge.
        exit_rule = Has(data.DRAWBRIDGE_ITEM_NAME)
    elif data.FIRST_ARCH_DOOR in world.locked_arch_doors:
        exit_rule = Has(data.DRAWBRIDGE_ITEM_NAME) | Has(data.FIRST_ARCH_DOOR.item_name)
    # With `lock_abilities`, Legs too: past the tutorial the island needs jumping and crouching.
    if abilities_locked(world):
        body = Has(data.LEGS_ITEM_NAME)
        exit_rule = body if exit_rule is None else exit_rule & body
    if exit_rule is not None:
        world.set_rule(world.get_entrance(regions.STARTING_EXIT), exit_rule)

    # The Spawn Secret Door, and the second ending behind it.
    world.set_rule(
        world.get_entrance(regions.GREEN_DOME_ENTRANCE),
        past_far_door(world, data.ZONE_FAR_DOOR["green_dome"], Has(data.GREEN_DOME.item_name)),
    )


def gauntlet_goal_rule(world: BigWalkWorld) -> Rule | None:
    """What leaving the Gauntlet asks for: its stairways and puzzle parts, when they are items."""
    locked = world.options.gauntlet_mode == GauntletMode.option_locked_stages
    rules = stairways_up_to(world, len(data.GAUNTLET_STAGES)) if locked else []
    if gauntlet_parts_are_items(world) and (not locked or world.options.gauntlet_puzzles_required):
        needs = data.gauntlet_needs_through(data.GAUNTLET_STAGES[-1])
        if needs:
            rules.append(HasAll(*[need.item_name for need in needs]))
    body = gauntlet_body_through(world, len(data.GAUNTLET_STAGES))
    if body is not None:
        rules.append(body)
    if not rules:
        return None
    rule = rules[0]
    for more in rules[1:]:
        rule = rule & more
    return rule


def reach_at_least(names: list[str], count: int) -> Rule:
    """At least `count` reachable, as an Or of combinations: the rule builder cannot count."""
    if count >= len(names):
        return And(*[CanReachLocation(name) for name in names])
    return Or(*[And(*[CanReachLocation(name) for name in choice]) for choice in combinations(names, count)])


def set_completion_rule(world: BigWalkWorld) -> None:
    # Every listed objective; places are regions, since the event sits in none of them.
    if world.objectives:
        rule: Rule | None = None
        for key in checks.objective_keys(world):
            if key == "big_collection":
                this: Rule = Has(data.GOURD_ITEM_NAME, count=world.deposit_goal)
            elif key == "big_climb":
                # A place the host sees a player at; nothing but the island itself to reach.
                this = CanReachRegion(regions.OVERWORLD)
            elif key == "big_party":
                # A radio plugged into the beach speakers, plus a station when they are items.
                this = CanReachRegion(regions.OVERWORLD) & Has(items.RADIO_ITEM_NAME)
                if world.options.shuffle_radio_music:
                    this = this & HasAny(*items.RADIO_ITEM_NAMES)
            elif key in ("big_doof", "big_makeover"):
                # The Doof Chamber and the Secret Salon are both in the tunnels.
                this = CanReachRegion(regions.TUNNEL_ZONE)
            elif key == "big_towers":
                count = world.options.towers_required.value
                capacity = monuments.slots_before(world, count)
                this = Has(data.GOURD_ITEM_NAME, count=capacity)
                for more in monuments_up_to(world, count):
                    this = this & more
            elif key == "big_objects":
                this = Has(data.OBJECT_EVENT_ITEM_NAME, count=checks.objects_target(world))
            elif key == "big_body":
                this = HasFromList(*items.ability_item_names(world), count=items.body_target(world))
            elif key == "big_lights":
                this = Has(data.LIGHT_EVENT_ITEM_NAME, count=checks.lights_target(world))
            elif key == "big_keys":
                this = reach_at_least([tower.location_name for tower in data.TOWERS], world.options.keys_required.value)
            elif key == "big_radio":
                this = reach_at_least(
                    [station.location_name for station in data.RADIO_STATIONS],
                    world.options.radio_stations_required.value)
            elif key == "big_game":
                this = CanReachRegion(regions.GREEN_DOME_ZONE)
            else:
                this = CanReachRegion(regions.ENDING_ZONE)
                if key == "big_goodbye":
                    gauntlet = gauntlet_goal_rule(world)
                    if gauntlet is not None:
                        this = this & gauntlet
            rule = this if rule is None else rule & this
        world.set_rule(world.get_location(locations.VICTORY_EVENT_NAME), rule)
        world.set_completion_rule(Has(locations.VICTORY_EVENT_NAME))
        return

    if world.options.goal == Goal.option_big_collection:
        world.set_rule(
            world.get_location(locations.VICTORY_EVENT_NAME),
            Has(data.GOURD_ITEM_NAME, count=world.deposit_goal),
        )

    # Other goals are places, carried by the Victory event's region; the Gauntlet adds its own.
    if world.options.goal == Goal.option_big_goodbye:
        rule = gauntlet_goal_rule(world)
        if rule is not None:
            world.set_rule(world.get_location(locations.VICTORY_EVENT_NAME), rule)

    world.set_completion_rule(Has(locations.VICTORY_EVENT_NAME))
