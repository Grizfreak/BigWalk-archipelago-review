"""Item definitions and pool construction for the Big Walk world."""

from __future__ import annotations

from collections import Counter
from typing import TYPE_CHECKING

from BaseClasses import Item, ItemClassification
from Options import OptionError

from . import checks, data, generation, monuments
from . import options as bigwalk_options

if TYPE_CHECKING:
    from .world import BigWalkWorld


# Item ids follow the game's own enum values where one exists, so the mod can compute them.
ITEM_NAME_TO_ID: dict[str, int] = {
    data.GOURD_ITEM_NAME: data.BASE_ID + 1,
    **{tower.item_name: data.feature_item_id(tower) for tower in data.TOWERS},
    **{data.key_item_name(tower): data.key_item_id(tower) for tower in data.TOWERS},
    **{station.item_name: data.radio_item_id(station) for station in data.RADIO_STATIONS},
    **{door.item_name: data.arch_door_item_id(door) for door in data.ARCH_DOORS},
    **{need.item_name: data.puzzle_need_item_id(need) for need in data.PUZZLE_NEEDS},
    **{stage.item_name: data.gauntlet_stage_id(stage) for stage in data.GAUNTLET_STAGES},
    data.PROGRESSIVE_GAUNTLET_ITEM_NAME: data.progressive_gauntlet_id(),
    data.PROGRESSIVE_MONUMENT_ITEM_NAME: data.progressive_monument_id(),
    **{ability.item_name: data.ability_item_id(ability) for ability in data.ABILITIES},
    **{name: data.BASE_ID + offset for name, offset in data.FILLER_ITEMS},
    **{name: data.BASE_ID + offset for name, offset in data.TRAP_ITEMS},
    **{name: data.BASE_ID + offset for name, offset in data.BONUS_ITEMS},
    **{data.teleport_item_name(name): data.teleport_item_id(i)
       for i, (_, name) in enumerate(data.TELEPORT_DESTINATIONS)},
    **{lock.item_name: data.zone_lock_item_id(lock) for lock in data.ZONE_LOCKS},
    **{door.item_name: data.tunnel_door_item_id(door) for door in data.TUNNEL_DOORS},
}

FILLER_ITEM_NAMES: tuple[str, ...] = tuple(name for name, _ in data.FILLER_ITEMS)
TRAP_ITEM_NAMES: tuple[str, ...] = tuple(name for name, _ in data.TRAP_ITEMS)
BONUS_ITEM_NAMES: tuple[str, ...] = tuple(name for name, _ in data.BONUS_ITEMS)
TELEPORT_ITEM_NAMES: tuple[str, ...] = tuple(data.teleport_item_name(name) for _, name in data.TELEPORT_DESTINATIONS)

# The gear the pool always holds, and the option that sets how many of each.
GUARANTEED_GEAR: dict[str, str] = {
    "Backpack": "backpacks_in_pool",
    "Belt": "belts_in_pool",
    data.GOURD_CARTON_ITEM_NAME: "gourd_cartons_in_pool",
}

# Never drawn as filler: create_all_items adds the flare guns itself.
FLARE_GUN_NAMES: frozenset[str] = frozenset((*data.FLARE_GUN_ITEM_NAMES, data.RAINBOW_FLARE_GUN_ITEM_NAME))

RADIO_ITEM_NAMES: tuple[str, ...] = tuple(station.item_name for station in data.RADIO_STATIONS)

PUZZLE_NEED_ITEM_NAMES: tuple[str, ...] = tuple(need.item_name for need in data.PUZZLE_NEEDS)

GAUNTLET_ITEM_NAMES: tuple[str, ...] = tuple(stage.item_name for stage in data.GAUNTLET_STAGES)

# A Feature is a door that opens on receipt; a Big Key is the physical key, cut and placed.
ITEM_NAME_GROUPS: dict[str, set[str]] = {
    "Features": {tower.item_name for tower in data.TOWERS},
    "Big Keys": {data.key_item_name(tower) for tower in data.TOWERS},
    "Radio Music": set(RADIO_ITEM_NAMES),
    "Arch Doors": {door.item_name for door in data.ARCH_DOORS},
    "Puzzle Needs": set(PUZZLE_NEED_ITEM_NAMES),
    "Gauntlet Doors": set(GAUNTLET_ITEM_NAMES) | {data.PROGRESSIVE_GAUNTLET_ITEM_NAME},
    "Monuments": {data.PROGRESSIVE_MONUMENT_ITEM_NAME},
    "Abilities": {ability.item_name for ability in data.ABILITIES},
    "Zone Locks": {lock.item_name for lock in data.ZONE_LOCKS},
    "Tunnel Doors": {door.item_name for door in data.TUNNEL_DOORS},
    "Filler": set(FILLER_ITEM_NAMES),
    "Teleporters": set(TELEPORT_ITEM_NAMES),
    "Traps": set(TRAP_ITEM_NAMES),
    "Hard Traps": {trap.item_name for trap in data.TRAPS if trap.hard},
    "Bonuses": set(BONUS_ITEM_NAMES),
    # What players call the carton.
    "Gourd Carrier": {data.GOURD_CARTON_ITEM_NAME},
}


def island_object_limits(world: BigWalkWorld) -> dict[str, int]:
    """`island_object_limits`, reading the old "X-Ray Goggles" name as the Scanner."""
    limits = dict(world.options.island_object_limits.value)
    if "X-Ray Goggles" in limits:
        limits.setdefault("Scanner", limits.pop("X-Ray Goggles"))
    return limits


class BigWalkItem(Item):
    game = "Big Walk"


def classification_for(name: str, require_arch_doors: bool = False) -> ItemClassification:
    if name == data.GOURD_ITEM_NAME:
        # Gourds fill the monuments, which release the big keys.
        return ItemClassification.progression
    if name in ITEM_NAME_GROUPS["Features"]:
        # Each opens part of the island; four gate regions outright.
        return ItemClassification.progression
    if name in ITEM_NAME_GROUPS["Big Keys"]:
        # Each unlocks six locations of its own: five cuts and a placement.
        return ItemClassification.progression
    if name == data.FIRST_ARCH_DOOR.item_name:
        # One of the two ways out of the starting zone when it is locked.
        return ItemClassification.progression
    if name in ITEM_NAME_GROUPS["Arch Doors"]:
        # Shortcuts, unless `require_arch_doors` puts places behind them in logic.
        return ItemClassification.progression if require_arch_doors else ItemClassification.useful
    if name in ITEM_NAME_GROUPS["Teleporters"]:
        # A comfort the logic does not count on: a teleport can only shorten a walk.
        return ItemClassification.useful
    if name in ITEM_NAME_GROUPS["Gauntlet Doors"]:
        # Only with `gauntlet_mode: locked_stages`: each is the way up out of its stage.
        return ItemClassification.progression
    if name in ITEM_NAME_GROUPS["Monuments"]:
        # Only with `tower_mode: by_item`: the k-th one opens the k-th tower after the first.
        return ItemClassification.progression
    if name in ITEM_NAME_GROUPS["Zone Locks"]:
        # Only ever created with `zone_locks`, for the checks it locks.
        return ItemClassification.progression
    if name in ITEM_NAME_GROUPS["Tunnel Doors"]:
        # Only ever created with `tunnel_sanity`, in place of the Tunnels.
        return ItemClassification.progression
    if name in ITEM_NAME_GROUPS["Puzzle Needs"]:
        # Only with lock_puzzle_needs: every puzzle built from a need requires it.
        return ItemClassification.progression
    if name == data.RUNNING_ITEM_NAME:
        # Nothing asks for it: the island is only slower to cross without it.
        return ItemClassification.useful
    if name in ITEM_NAME_GROUPS["Abilities"]:
        # Only with `lock_abilities`: Legs leads out of the starting zone, puzzles ask for the rest.
        return ItemClassification.progression
    if name in TRAP_ITEM_NAMES:
        return ItemClassification.trap
    if name == data.CONFETTI_ITEM_NAME:
        # Drawn with the bonuses, but only a show: filler, so a junk-only check can hold it.
        return ItemClassification.filler
    if name in BONUS_ITEM_NAMES:
        # A boost for a while: nice to get, never needed.
        return ItemClassification.useful
    # Radio Music only starts a piece of music; no rule requires it.
    return ItemClassification.filler


RADIO_ITEM_NAME = "Radio"
"""The portable radio gadget; the world's own radios are taken off the island."""


def party_asked_for(world: BigWalkWorld) -> bool:
    return "big_party" in world.objectives


def create_item(world: BigWalkWorld, name: str) -> BigWalkItem:
    classification = classification_for(name, bool(world.options.require_arch_doors))
    # `big_party` needs a radio and a music, so they become progression.
    if party_asked_for(world) and (name == RADIO_ITEM_NAME or name in RADIO_ITEM_NAMES):
        classification = ItemClassification.progression
    return BigWalkItem(name, classification, ITEM_NAME_TO_ID[name], world.player)


def trap_weights(world: BigWalkWorld) -> dict[str, int]:
    """Trap name -> weight from `trap_weights`, hard traps only with `hard_traps`, none at 0."""
    weights = world.options.trap_weights.value
    hard = bool(world.options.hard_traps)
    return {
        trap.item_name: weights.get(trap.item_name, 0)
        for trap in data.TRAPS
        if (hard or not trap.hard) and weights.get(trap.item_name, 0) > 0
    }


def pick_trap(world: BigWalkWorld) -> str | None:
    weights = trap_weights(world)
    if not weights:
        return None
    return world.random.choices(list(weights), weights=list(weights.values()))[0]


def pick_bonus(world: BigWalkWorld) -> str | None:
    weights = {name: weight for name, weight in world.options.bonus_weights.value.items() if weight > 0}
    if not weights:
        return None
    return world.random.choices(list(weights), weights=list(weights.values()))[0]


def get_random_filler_item_name(world: BigWalkWorld) -> str:
    if world.random.randint(1, 100) <= world.options.trap_fill_percentage:
        trap = pick_trap(world)
        if trap is not None:
            return trap
    if world.random.randint(1, 100) <= world.options.bonus_fill_percentage:
        bonus = pick_bonus(world)
        if bonus is not None:
            return bonus
    return pick_island_object(world)


def pick_island_object(world: BigWalkWorld) -> str:
    """An island object under its `island_object_limits` count, else the least-made kept one."""
    # Made on first use: an item link's group world never runs generate_early.
    counts = world.__dict__.setdefault("island_objects_made", Counter())
    limits = island_object_limits(world)
    objects = [n for n in FILLER_ITEM_NAMES if n not in GUARANTEED_GEAR and n not in FLARE_GUN_NAMES]
    open_ = [n for n in objects if counts[n] < limits.get(n, 0)]
    kept = [n for n in objects if limits.get(n, 0) > 0]
    if open_:
        name = world.random.choice(open_)
    elif kept:
        name = min(kept, key=lambda n: (counts[n], world.random.random()))
    else:
        bonus = pick_bonus(world)
        if bonus is not None:
            return bonus
        name = min(objects, key=lambda n: (counts[n], world.random.random()))
    counts[name] += 1
    return name


def abilities_in_pool(world: BigWalkWorld) -> tuple[data.Ability, ...]:
    """The ability items `lock_abilities` puts in the pool: each arm alone, or both together."""
    mode = world.options.lock_abilities
    if mode == bigwalk_options.LockAbilities.option_handed:
        chosen = tuple(a for a in data.ABILITIES if a.handed)
    elif mode == bigwalk_options.LockAbilities.option_ambidextrous:
        chosen = tuple(a for a in data.ABILITIES if a.ambidextrous)
    else:
        return ()
    if not world.options.lock_running:
        chosen = tuple(a for a in chosen if a.item_name != data.RUNNING_ITEM_NAME)
    return chosen


def packs_give_themselves(world: BigWalkWorld) -> bool:
    return world.options.pack_sanity == bigwalk_options.PackSanity.option_vanilla


def pack_item_name(pickup: data.Pickup) -> str:
    """A pack's item, from the end of its location's name."""
    if pickup.location_name.endswith("Backpack"):
        return "Backpack"
    if pickup.location_name.endswith("Belt"):
        return "Belt"
    return data.GOURD_CARTON_ITEM_NAME


def create_all_items(world: BigWalkWorld) -> None:
    gourds = world.gourd_count
    # `linear_puzzles`: not the purple puzzles, whose gourd could be needed to open the chairlift.
    if world.options.linear_puzzles:
        for puzzle in [p for p in checks.puzzles(world) if p.prop_name not in data.CHAIRLIFT_PUZZLES][:gourds]:
            world.get_location(puzzle.location_name).place_locked_item(world.create_item(data.GOURD_ITEM_NAME))
            gourds -= 1
    pool: list[Item] = [world.create_item(data.GOURD_ITEM_NAME) for _ in range(gourds)]

    # `pack_sanity: vanilla`: each pack's check holds the pack itself, and the pool holds none.
    if packs_give_themselves(world):
        for pickup in data.PICKUPS:
            world.get_location(pickup.location_name).place_locked_item(world.create_item(pack_item_name(pickup)))

    for tower in world.towers:
        if tower is data.YELLOW_TWIST and world.options.tunnel_sanity:
            # `tunnel_sanity`: the doors one by one instead, below.
            continue
        if tower.item_name == data.MAP_ROOM_ITEM_NAME and world.options.start_with_map_room_open:
            # Handed over at the start, like the drawbridge.
            world.push_precollected(world.create_item(tower.item_name))
            continue
        if world.options.linear_keys and not (
                tower.item_name == data.DRAWBRIDGE_ITEM_NAME and world.options.start_with_drawbridge_open):
            # `linear_keys`: the door sits on its key's deposit, as in the game.
            world.get_location(tower.location_name).place_locked_item(world.create_item(tower.item_name))
            continue
        if tower.item_name == data.DRAWBRIDGE_ITEM_NAME and world.options.start_with_drawbridge_open:
            # Only the door is handed over: the key and its deposit check stay in play.
            world.push_precollected(world.create_item(tower.item_name))
            continue
        pool.append(world.create_item(tower.item_name))

    if world.options.tunnel_sanity:
        pool += [world.create_item(door.item_name) for door in data.TUNNEL_DOORS]

    # Keys are never handed over (each is six checks); `linear_towers` puts one in its monument.
    for tower in world.towers:
        if monuments.key_in_its_monument(world, tower):
            world.get_location(monuments.key_location_name(world, tower)).place_locked_item(
                world.create_item(data.key_item_name(tower)))
        else:
            pool.append(world.create_item(data.key_item_name(tower)))

    # The items below displace filler: the count at the end balances the pool.
    if world.options.shuffle_radio_music:
        pool += [world.create_item(station.item_name) for station in data.RADIO_STATIONS]

    pool += [world.create_item(door.item_name) for door in world.locked_arch_doors]

    if world.options.teleport_buttons == bigwalk_options.TeleportButtons.option_items:
        pool += [world.create_item(name) for name in TELEPORT_ITEM_NAMES]

    if world.options.gauntlet_mode == bigwalk_options.GauntletMode.option_locked_stages:
        if world.options.gauntlet_stage_items == bigwalk_options.GauntletStageItems.option_progressive:
            pool += [world.create_item(data.PROGRESSIVE_GAUNTLET_ITEM_NAME) for _ in data.GAUNTLET_STAGES]
        else:
            pool += [world.create_item(stage.item_name) for stage in data.GAUNTLET_STAGES]

    if world.options.tower_mode == bigwalk_options.TowerMode.option_by_item:
        # One per monument after the first, plus the first when it does not start open.
        count = len(world.towers) - (1 if world.options.first_tower_open else 0)
        pool += [world.create_item(data.PROGRESSIVE_MONUMENT_ITEM_NAME) for _ in range(count)]

    # Needs the player starts with are handed over and never enter the pool.
    if world.options.lock_puzzle_needs:
        for need in data.PUZZLE_NEEDS:
            if need.key in world.options.start_with_puzzle_needs.value:
                world.push_precollected(world.create_item(need.item_name))
            else:
                pool.append(world.create_item(need.item_name))

    for lock in checks.zone_locks(world):
        if lock.key in world.options.start_with_zone_locks.value:
            world.push_precollected(world.create_item(lock.item_name))
        else:
            pool.append(world.create_item(lock.item_name))

    for ability in abilities_in_pool(world):
        locked = data.ABILITY_LOCK_KEYS.get(ability.key) not in world.options.start_with_abilities.value
        for _ in range(ability.copies):
            if locked:
                pool.append(world.create_item(ability.item_name))
            else:
                world.push_precollected(world.create_item(ability.item_name))

    # `big_party` needs a radio: one is always added, not taken from the filler headroom.
    if party_asked_for(world):
        pool.append(world.create_item(RADIO_ITEM_NAME))

    unfilled = len(world.multiworld.get_unfilled_locations(world.player))
    filler_needed = unfilled - len(pool)
    # Universal Tracker regenerates a seed for its logic: an error here would leave no tracker.
    if filler_needed < 0 and getattr(world.multiworld, "re_gen_passthrough", None):
        filler_needed = 0
    if filler_needed < 0:
        raise OptionError(
            f"Big Walk: {world.player_name}'s options need {len(pool)} items for only {unfilled} "
            "locations. lock_puzzle_needs adds up to 17 items; raise gourd_sanity, turn radio_sanity "
            "on, list more start_with_puzzle_needs, or turn lock_puzzle_needs off to make room.")
    # A few bags are guaranteed, from the filler headroom only: a tight pool loses them first.
    for name, option in GUARANTEED_GEAR.items():
        take = 0 if packs_give_themselves(world) else min(getattr(world.options, option).value, filler_needed)
        pool += [world.create_item(name) for _ in range(take)]
        filler_needed -= take
    # Flare guns also come from the headroom, as many as `island_object_limits` allows.
    limits = island_object_limits(world)
    flare_guns = [*(data.FLARE_GUN_ITEM_NAMES if world.options.flare_gun_sanity else ()),
                  data.RAINBOW_FLARE_GUN_ITEM_NAME]
    for name in flare_guns:
        take = min(limits.get(name, 0), max(0, filler_needed))
        pool += [world.create_item(name) for _ in range(take)]
        filler_needed -= take
    pool += [world.create_filler() for _ in range(filler_needed)]

    world.multiworld.itempool += generation.set_local_items_aside(world, pool)


def ability_item_names(world: BigWalkWorld) -> list[str]:
    """Ability items the logic counts, once per copy; Running is only useful, so not counted."""
    return [ability.item_name for ability in abilities_in_pool(world) for _ in range(ability.copies)
            if ability.item_name != data.RUNNING_ITEM_NAME]


def body_target(world: BigWalkWorld) -> int:
    """`body_required` percent of the ability items, rounded up, at least one."""
    count = len(ability_item_names(world))
    return max(1, -(-count * world.options.body_required.value // 100)) if count else 0
