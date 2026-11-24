"""Rule helpers shared by the other rule modules."""

from __future__ import annotations

from typing import TYPE_CHECKING

from rule_builder.rules import Has, Rule

from .. import checks, data
from .. import options as bigwalk_options

if TYPE_CHECKING:
    from ..world import BigWalkWorld


def past_far_door(world: BigWalkWorld, door: data.ArchDoor | None, rule):
    """`rule`, plus the far door it lies past when `require_arch_doors` locks that door."""
    if door is None or not world.options.require_arch_doors or door not in world.locked_arch_doors:
        return rule

    return rule & Has(door.item_name)


def purple_hole(world: BigWalkWorld) -> Rule:
    if world.options.tunnel_sanity:
        return Has(data.PURPLE_TUNNEL_ITEM_NAME)
    return Has(data.YELLOW_TWIST.item_name)


def all_of(rules: list[Rule]) -> Rule:
    rule = rules[0]
    for more in rules[1:]:
        rule = rule & more
    return rule


def abilities_locked(world: BigWalkWorld) -> bool:
    return world.options.lock_abilities != bigwalk_options.LockAbilities.option_disabled


def body_rule(world: BigWalkWorld, body: data.Body) -> Rule | None:
    """What a place asks of the body with `lock_abilities`; `arms` counts for each arm."""
    if not abilities_locked(world):
        return None
    rules: list[Rule] = []
    if body.seat:
        rules.append(Has(data.SEAT_ITEM_NAME))
    if body.throwing:
        rules.append(Has(data.THROWING_ITEM_NAME))
    if body.carrying:
        rules.append(Has(data.CARRYING_ITEM_NAME))
    arms = body.arms
    if arms:
        if world.options.lock_abilities == bigwalk_options.LockAbilities.option_handed:
            rules.append(Has(data.LEFT_ARM_ITEM_NAME, count=arms) & Has(data.RIGHT_ARM_ITEM_NAME, count=arms))
        else:
            rules.append(Has(data.ARMS_ITEM_NAME, count=arms))
    return all_of(rules) if rules else None


def gauntlet_body_through(world: BigWalkWorld, count: int) -> Rule | None:
    """The body the first `count` Gauntlet stages ask for: the most of each need."""
    stages = data.GAUNTLET_STAGE_ABILITIES[:count]
    if not stages:
        return None
    return body_rule(world, data.Body(
        seat=any(s.seat for s in stages), arms=max(s.arms for s in stages),
        throwing=any(s.throwing for s in stages), carrying=any(s.carrying for s in stages)))


def with_all(rule: Rule | None, more: list[Rule]) -> Rule | None:
    """`rule` and every one of `more`; None when there is nothing at all."""
    rules = ([rule] if rule is not None else []) + more
    return all_of(rules) if rules else None


def place_rule(world: BigWalkWorld, zone: str) -> Rule | None:
    """What a place of the island asks for beyond its region, as object_rule says."""
    if zone == "map_room":
        return Has(data.tower_by_prop("bigKeyRedZone").item_name)
    if zone == "purple":
        return purple_hole(world)
    if zone.startswith("tower:"):
        door = data.TOWER_FAR_DOOR.get(zone[len("tower:"):])
        if door is not None and world.options.require_arch_doors and door in world.locked_arch_doors:
            return Has(door.item_name)
    return None


def with_zone(world: BigWalkWorld, rule: Rule | None, key: str, zone: str, lamps: bool) -> Rule | None:
    """The rule, plus the `zone_locks` item of the zone this lamp or gadget is in, if any."""
    lock = data.zone_lock_for(key, zone, lamps)
    if lock is None or lock not in checks.zone_locks(world):
        return rule
    return Has(lock.item_name) if rule is None else rule & Has(lock.item_name)
