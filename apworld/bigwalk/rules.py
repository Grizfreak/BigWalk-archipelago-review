"""Access rules for the Big Walk world."""

from __future__ import annotations

from typing import TYPE_CHECKING

import logging
from itertools import combinations

from BaseClasses import LocationProgressType

from rule_builder.rules import And, CanReachLocation, HasFromList, CanReachRegion, Has, HasAll, HasAny, Or, Rule

from . import data, items, locations, regions
from . import options as bigwalk_options
from .options import GauntletMode, GauntletStageItems, Goal

if TYPE_CHECKING:
    from .world import BigWalkWorld


def set_all_rules(world: BigWalkWorld) -> None:
    set_key_deposit_rules(world)
    set_tower_reached_rules(world)
    set_key_cut_rules(world)
    set_pickup_junk(world)
    set_gourd_deposit_rules(world)
    set_puzzle_rules(world)
    set_gauntlet_rules(world)
    set_object_rules(world)
    set_firework_rules(world)
    set_object_event_rules(world)
    set_monument_full_rules(world)
    set_light_rules(world)
    set_entrance_rules(world)
    set_completion_rule(world)


def past_far_door(world: BigWalkWorld, door: data.ArchDoor | None, rule):
    """`rule`, plus the far door it lies past when `require_arch_doors` locks that door."""
    if door is None or not world.options.require_arch_doors or door not in world.locked_arch_doors:
        return rule

    return rule & Has(door.item_name)


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
    position = world.tower_index(prop) + 1
    capacity = sum(world.tower_slots(data.tower_by_prop(before)) for before in world.tower_order[:position - 1])
    rules = monuments_up_to(world, position)
    if capacity > 0:
        rules.append(Has(data.GOURD_ITEM_NAME, count=capacity))
    return rules


def five_monuments_full(world: BigWalkWorld, more: int) -> Rule:
    """The five monuments the game waits for full, plus `more` gourds for the Black Tower's."""
    five = [tower for tower in data.TOWERS if tower is not data.BLACK_MONOLITH and tower is not data.GREEN_DOME]
    rule: Rule = Has(data.GOURD_ITEM_NAME, count=sum(world.tower_slots(tower) for tower in five) + more)
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


def set_pickup_junk(world: BigWalkWorld) -> None:
    """`pack_sanity: junk` and `flare_gun_sanity: junk`: their checks hold junk."""
    for option, pickups in (("pack_sanity", data.PICKUPS), ("flare_gun_sanity", data.FLARE_GUN_PICKUPS)):
        if getattr(world.options, option).current_key == "junk":
            exclude_as_junk(world, [world.get_location(pickup.location_name) for pickup in pickups], f"{option} junk")


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


def tower_count_needing(world: BigWalkWorld, amount: int) -> int:
    """How many towers of the order the `amount`-th gourd placed reaches into."""
    total = 0
    for index, prop in enumerate(world.tower_order, start=1):
        total += world.tower_slots(data.tower_by_prop(prop))
        if amount <= total:
            return index
    return len(world.tower_order)


def set_gourd_deposit_rules(world: BigWalkWorld) -> None:
    for amount in world.deposit_amounts:
        rule: Rule = Has(data.GOURD_ITEM_NAME, count=amount)
        # In order, the amount-th gourd also needs what opens the monuments filled before it.
        for more in monuments_up_to(world, tower_count_needing(world, amount)) if world.tower_order else []:
            rule = rule & more
        world.set_rule(world.get_location(data.deposit_location_name(amount)), rule)


def set_puzzle_rules(world: BigWalkWorld) -> None:
    """With `lock_puzzle_needs`, a puzzle is in logic once every item it is built from arrived."""
    for puzzle in world.puzzles():
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
        if (puzzle.prop_name in data.CHAIRLIFT_PUZZLES
                and world.options.include_big_game_puzzles == bigwalk_options.IncludeBigGamePuzzles.option_when_unlocked):
            rules.append(CanReachRegion(regions.ENDING_ZONE))
        if rules:
            world.set_rule(world.get_location(puzzle.location_name), all_of(rules))


def purple_hole(world: BigWalkWorld) -> Rule:
    if world.options.tunnel_sanity:
        return Has(data.PURPLE_TUNNEL_ITEM_NAME)
    return Has(data.YELLOW_TWIST.item_name)


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


def with_all(rule: Rule | None, more: list[Rule]) -> Rule | None:
    """`rule` and every one of `more`; None when there is nothing at all."""
    rules = ([rule] if rule is not None else []) + more
    return all_of(rules) if rules else None


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
    if lock is None or lock not in world.zone_locks():
        return rule
    return Has(lock.item_name) if rule is None else rule & Has(lock.item_name)


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
    checks = {light.key for light in world.lights()}
    for light in world.lights_counted():
        rule = light_rule(world, light)
        if rule is None:
            continue
        if light.key in checks:
            world.set_rule(world.get_location(light.location_name), rule)
        world.set_rule(world.get_location(data.light_event_name(light)), rule)


def set_monument_full_rules(world: BigWalkWorld) -> None:
    """A monument full: its slots in gourds, its far door, and in order every monument before."""
    for tower in world.monuments_full():
        world.set_rule(world.get_location(data.monument_full_location_name(tower)),
                       monument_holding(world, tower, world.tower_slots(tower)))
    # A monument key (`linear_towers: one_deposit_box`): the same, up to the gourd drawn for it.
    for tower in world.monument_keys():
        world.set_rule(world.get_location(data.monument_key_location_name(tower)),
                       monument_holding(world, tower, world.key_gourds(tower)))


def monument_holding(world: BigWalkWorld, tower: data.Tower, gourds: int) -> Rule:
    """This tower's monument holding `gourds`; in order, every one before it full as well."""
    if world.tower_order:
        position = world.tower_index(tower.prop_name) + 1
        capacity = sum(world.tower_slots(data.tower_by_prop(prop)) for prop in world.tower_order[:position - 1])
        rule: Rule = Has(data.GOURD_ITEM_NAME, count=capacity + gourds)
        for more in monuments_up_to(world, position):
            rule = rule & more
        return rule
    # Every monument open: the Black Tower's is past the door the other five open.
    if tower is data.BLACK_MONOLITH and not world.options.open_black_tower:
        return past_far_door(world, data.TOWER_FAR_DOOR.get(tower.prop_name), five_monuments_full(world, gourds))
    return past_far_door(world, data.TOWER_FAR_DOOR.get(tower.prop_name), Has(data.GOURD_ITEM_NAME, count=gourds))


def set_object_event_rules(world: BigWalkWorld) -> None:
    """big_objects' events: each asks what its object asks."""
    if "big_objects" not in world.objectives:
        return
    for name in world.object_location_names():
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


def exclude_as_junk(world: BigWalkWorld, chosen: list, what: str) -> None:
    """Excludes these locations, within one junk budget shared by every junk option."""
    budget = world.__dict__.get("junk_budget")
    if budget is None:
        budget = sum(1 for item in world.multiworld.itempool
                     if item.player == world.player and not item.advancement and not item.useful)
        # Checks of a `local` option take this world's non-progression items, junk included.
        for group, contents in (("Objects", world.options.object_sanity_items.value),
                                ("Lights", world.options.light_sanity_items.value)):
            if contents == bigwalk_options.ObjectSanityItems.option_local:
                names = locations.LOCATION_NAME_GROUPS[group]
                budget -= sum(1 for location in world.multiworld.get_locations(world.player) if location.name in names)
        budget = max(0, budget)
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
    local_groups = [group for group, contents in (("Objects", world.options.object_sanity_items.value),
                                                  ("Lights", world.options.light_sanity_items.value))
                    if contents == bigwalk_options.ObjectSanityItems.option_local]
    local_names = set().union(*(locations.LOCATION_NAME_GROUPS[group] for group in local_groups))
    open_locations = sum(1 for location in world.multiworld.get_locations(world.player)
                         if location.address is not None and location.item is None
                         and location.progress_type != LocationProgressType.EXCLUDED
                         and location.name not in local_names)
    progression = sum(1 for item in world.multiworld.itempool if item.player == world.player and item.advancement)
    return max(0, open_locations - progression - max(PROGRESSION_MARGIN, progression // 2))


def set_object_rules(world: BigWalkWorld) -> None:
    """`object_sanity`: each object by its own rule, or its kind's easiest for `first_pickup`."""
    mode = world.options.object_sanity
    restrict_object_items(world)
    if mode == bigwalk_options.ObjectSanity.option_every:
        for obj in data.ISLAND_OBJECTS:
            if not world.object_kind_counted(obj.kind):
                continue
            rule = object_rule(world, obj)
            if rule is not None:
                world.set_rule(world.get_location(obj.location_name), rule)
    elif mode == bigwalk_options.ObjectSanity.option_first_pickup:
        for kind, label in data.OBJECT_KINDS:
            if not world.object_kind_counted(kind):
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
        for key in world.objective_keys():
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
                capacity = sum(world.tower_slots(data.tower_by_prop(prop)) for prop in world.tower_order[:count])
                this = Has(data.GOURD_ITEM_NAME, count=capacity)
                for more in monuments_up_to(world, count):
                    this = this & more
            elif key == "big_objects":
                this = Has(data.OBJECT_EVENT_ITEM_NAME, count=world.objects_target())
            elif key == "big_body":
                this = HasFromList(*world.ability_item_names(), count=world.body_target())
            elif key == "big_lights":
                this = Has(data.LIGHT_EVENT_ITEM_NAME, count=world.lights_target())
            elif key == "big_keys":
                this = reach_at_least([tower.location_name for tower in data.TOWERS], world.options.keys_required.value)
            elif key == "big_radio":
                this = reach_at_least(
                    [station.location_name for station in data.RADIO_STATIONS], world.options.radio_stations_required.value)
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
