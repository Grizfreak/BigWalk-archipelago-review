"""Key, tower, deposit and monument rules."""

from __future__ import annotations

from typing import TYPE_CHECKING

from rule_builder.rules import Has, Rule

from .. import data, monuments
from .. import options as bigwalk_options
from .common import all_of, past_far_door
from .junk import exclude_as_junk

if TYPE_CHECKING:
    from ..world import BigWalkWorld


def set_key_deposit_rules(world: BigWalkWorld) -> None:
    """Placing a key in its receptacle needs that key, and the far door its tower lies past."""
    for tower in world.towers:
        world.set_rule(
            world.get_location(tower.location_name),
            past_far_door(world, data.TOWER_FAR_DOOR.get(tower.prop_name), Has(data.key_item_name(tower))),
        )


def set_tower_reached_rules(world: BigWalkWorld) -> None:
    """Reaching a tower means pressing its button: past its far door, and in its turn if ordered."""
    if not world.options.tower_sanity:
        return

    for tower in data.reached_towers():
        rules: list[Rule] = []
        door = data.TOWER_FAR_DOOR.get(tower.prop_name)
        if door is not None and world.options.require_arch_doors and door in world.locked_arch_doors:
            rules.append(Has(door.item_name))
        # The Black Tower's button is up the tower, past the door at its foot.
        if tower is data.BLACK_MONOLITH:
            inside = black_tower_door(world)
            if inside is not None:
                rules.append(inside)
        else:
            rules += tower_open(world, tower.prop_name)
        if rules:
            world.set_rule(world.get_location(data.tower_reached_location_name(tower)), all_of(rules))


def black_tower_door(world: BigWalkWorld) -> Rule | None:
    """What opens the door at the Black Tower's foot; None when `open_black_tower` opens it."""
    if world.options.open_black_tower:
        return None

    if world.tower_order:
        rules = tower_open(world, data.BLACK_MONOLITH.prop_name)
        return all_of(rules) if rules else None

    return five_monuments_full(world, 0)


def tower_open(world: BigWalkWorld, prop: str) -> list[Rule]:
    """In order, the mod holds a tower's button until every monument before it is full."""
    if not world.tower_order or prop not in world.tower_order:
        return []
    position = monuments.tower_index(world, prop) + 1
    capacity = monuments.slots_before(world, position - 1)
    rules = monuments_up_to(world, position)
    if capacity > 0:
        rules.append(Has(data.GOURD_ITEM_NAME, count=capacity))
    return rules


def five_monuments_full(world: BigWalkWorld, more: int) -> Rule:
    """The five monuments the game waits for full, plus `more` gourds for the Black Tower's."""
    five = [tower for tower in data.TOWERS if tower is not data.BLACK_MONOLITH and tower is not data.GREEN_DOME]
    rule: Rule = Has(data.GOURD_ITEM_NAME, count=sum(monuments.tower_slots(world, tower) for tower in five) + more)
    doors = []
    for tower in five:
        door = data.TOWER_FAR_DOOR.get(tower.prop_name)
        if (door is not None and world.options.require_arch_doors and door in world.locked_arch_doors
                and door not in doors):
            doors.append(door)
            rule = rule & Has(door.item_name)
    return rule


def set_key_cut_rules(world: BigWalkWorld) -> None:
    """Each cut needs its tower's key, as its deposit does."""
    if world.options.cut_key_sanity == bigwalk_options.CutKeySanity.option_off:
        return
    cuts = []
    for tower in world.towers:
        requirement = past_far_door(world, data.TOWER_FAR_DOOR.get(tower.prop_name), Has(data.key_item_name(tower)))
        for name in data.cut_locations(tower):
            location = world.get_location(name)
            world.set_rule(location, requirement)
            cuts.append(location)
    if world.options.cut_key_sanity == bigwalk_options.CutKeySanity.option_junk:
        exclude_as_junk(world, cuts, "cut_key_sanity junk")


def doors_up_to(world: BigWalkWorld, count: int) -> list[Rule]:
    """Far doors of the first `count` towers in the order, each once, when they are items."""
    doors = []
    for prop in world.tower_order[:count]:
        door = data.TOWER_FAR_DOOR.get(prop)
        if (door is not None and world.options.require_arch_doors and door in world.locked_arch_doors
                and door not in doors):
            doors.append(door)
    return [Has(door.item_name) for door in doors]


def monuments_up_to(world: BigWalkWorld, count: int) -> list[Rule]:
    """What the first `count` monuments of the order cost: far doors, and `by_item` items."""
    rules = doors_up_to(world, count)
    if world.options.tower_mode == bigwalk_options.TowerMode.option_by_item:
        needed = count - (1 if world.options.first_tower_open else 0)
        if needed > 0:
            rules.append(Has(data.PROGRESSIVE_MONUMENT_ITEM_NAME, count=needed))
    return rules


def set_gourd_deposit_rules(world: BigWalkWorld) -> None:
    for amount in world.deposit_amounts:
        rule: Rule = Has(data.GOURD_ITEM_NAME, count=amount)
        # In order, the amount-th gourd also needs what opens the monuments filled before it.
        for more in monuments_up_to(world, monuments.tower_count_needing(world, amount)) if world.tower_order else []:
            rule = rule & more
        world.set_rule(world.get_location(data.deposit_location_name(amount)), rule)


def set_monument_full_rules(world: BigWalkWorld) -> None:
    """A monument full: its slots in gourds, its far door, and in order every monument before."""
    for tower in monuments.monuments_full(world):
        world.set_rule(world.get_location(data.monument_full_location_name(tower)),
                       monument_holding(world, tower, monuments.tower_slots(world, tower)))
    # A monument key (`linear_towers: one_deposit_box`): the same, up to the gourd drawn for it.
    for tower in monuments.monument_keys(world):
        world.set_rule(world.get_location(data.monument_key_location_name(tower)),
                       monument_holding(world, tower, monuments.key_gourds(world, tower)))


def monument_holding(world: BigWalkWorld, tower: data.Tower, gourds: int) -> Rule:
    """This tower's monument holding `gourds`; in order, every one before it full as well."""
    if world.tower_order:
        position = monuments.tower_index(world, tower.prop_name) + 1
        capacity = monuments.slots_before(world, position - 1)
        rule: Rule = Has(data.GOURD_ITEM_NAME, count=capacity + gourds)
        for more in monuments_up_to(world, position):
            rule = rule & more
        return rule
    # Every monument open: the Black Tower's is past the door the other five open.
    if tower is data.BLACK_MONOLITH and not world.options.open_black_tower:
        return past_far_door(world, data.TOWER_FAR_DOOR.get(tower.prop_name), five_monuments_full(world, gourds))
    return past_far_door(world, data.TOWER_FAR_DOOR.get(tower.prop_name), Has(data.GOURD_ITEM_NAME, count=gourds))
