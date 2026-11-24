"""Tower order, monument slots and monument keys of a seed."""

from __future__ import annotations

from typing import TYPE_CHECKING

from Options import OptionError

from . import data
from . import options as bigwalk_options

if TYPE_CHECKING:
    from .world import BigWalkWorld


def tower_reach_hints(world: BigWalkWorld) -> dict[str, list[int]]:
    """Location ids to hint by tower once it is reached."""
    hints: dict[str, list[int]] = {}
    for tower in data.TOWERS:
        ids: list[int] = []
        if world.options.hint_discovered_key_locations:
            if world.options.cut_key_sanity != bigwalk_options.CutKeySanity.option_off:
                ids += [data.cut_location_id(tower, i) for i in range(tower.segments)]
            ids.append(data.key_deposit_location_id(tower))
        if world.options.hint_discovered_deposit_boxes and world.tower_order:
            ids += _deposit_ids_of(world, tower)
        if ids:
            hints[tower.prop_name] = ids
    return hints


def slots_before(world: BigWalkWorld, position: int) -> int:
    """Slots of the first `position` monuments, in the seed's order of towers."""
    return sum(tower_slots(world, data.tower_by_prop(prop)) for prop in world.tower_order[:position])


def tower_count_needing(world: BigWalkWorld, amount: int) -> int:
    """How many towers of the order the `amount`-th gourd placed reaches into."""
    total = 0
    for index, prop in enumerate(world.tower_order, start=1):
        total += tower_slots(world, data.tower_by_prop(prop))
        if amount <= total:
            return index
    return len(world.tower_order)


def _deposit_ids_of(world: BigWalkWorld, tower: data.Tower) -> list[int]:
    """The gourd deposit checks in this tower's monument, in the seed's order of towers."""
    start = slots_before(world, tower_index(world, tower.prop_name))
    return [data.deposit_location_id(amount) for amount in world.deposit_amounts
            if start < amount <= start + tower_slots(world, tower)]


def tower_slots(world: BigWalkWorld, tower: data.Tower) -> int:
    """Slots the seed's gourds can fill; the Green Dome gives up any shortfall."""
    missing = max(0, data.MAX_MONUMENT_SLOTS - world.gourd_count)
    if tower.prop_name == "bigKeyOverflow":
        return max(0, tower.slots - missing)
    return tower.slots


def draw_the_tower_order(world: BigWalkWorld) -> None:
    """The order the monuments open in; the seed's own while Universal Tracker re-generates."""
    world.tower_order = ()
    if world.options.tower_mode == bigwalk_options.TowerMode.option_all_open:
        if "big_towers" in world.objectives:
            raise OptionError(
                f"Big Walk ({world.player_name}): the Objectives hold big_towers, which needs Tower Mode "
                f"in_order or by_item.")
        return

    if not world.options.first_tower_open and world.options.tower_mode != bigwalk_options.TowerMode.option_by_item:
        raise OptionError(
            f"Big Walk ({world.player_name}): first_tower_open is off, which needs Tower Mode by_item: in_order has "
            f"nothing to open the first monument.")

    passthrough = getattr(world.multiworld, "re_gen_passthrough", None)
    slot_data = passthrough.get(world.game) if passthrough else None
    known = tuple(slot_data.get("tower_order", ())) if slot_data else ()
    props = [tower.prop_name for tower in world.towers]
    if sorted(known) == sorted(props):
        world.tower_order = known
        return

    order = list(props)
    world.random.shuffle(order)
    world.tower_order = tuple(order)


def tower_index(world: BigWalkWorld, prop_name: str) -> int:
    return world.tower_order.index(prop_name)


def tutorial_monument_first(world: BigWalkWorld) -> bool:
    """Whether the tutorial's monument takes the first gourds."""
    return not world.tower_order or world.tower_order[0] == data.TOWERS[0].prop_name


def key_in_its_monument(world: BigWalkWorld, tower: data.Tower) -> bool:
    """Not the Drawbridge Key when its monument would lie past the drawbridge it opens."""
    if not world.options.linear_towers or tower_slots(world, tower) <= 0:
        return False
    return not (tower is data.TOWERS[0] and world.options.linear_arch_doors and world.options.linear_keys
                and not world.options.start_with_drawbridge_open and not tutorial_monument_first(world))


def key_location_name(world: BigWalkWorld, tower: data.Tower) -> str:
    """Where `linear_towers` puts this tower's key: its monument full, or its monument key."""
    if world.options.linear_towers == bigwalk_options.LinearTowers.option_one_deposit_box:
        return data.monument_key_location_name(tower)
    return data.monument_full_location_name(tower)


def key_gourds(world: BigWalkWorld, tower: data.Tower) -> int:
    """How many gourds this tower's monument holds when its key comes (`linear_towers`)."""
    return world.monument_key_gourds.get(tower.prop_name, tower_slots(world, tower))


def monuments_full(world: BigWalkWorld) -> list[data.Tower]:
    """The towers whose monument full is a check."""
    if not (world.options.monument_sanity
            or world.options.linear_towers == bigwalk_options.LinearTowers.option_all_deposit_boxes):
        return []
    return [tower for tower in data.TOWERS if tower_slots(world, tower) > 0]


def monument_keys(world: BigWalkWorld) -> list[data.Tower]:
    """The towers whose monument key is a location (`linear_towers: one_deposit_box`)."""
    if world.options.linear_towers != bigwalk_options.LinearTowers.option_one_deposit_box:
        return []
    return [tower for tower in data.TOWERS if key_in_its_monument(world, tower)]


def draw_the_monument_keys(world: BigWalkWorld) -> None:
    """For each tower, which gourd of its monument frees its key (`one_deposit_box`)."""
    world.monument_key_gourds = {}
    if world.options.linear_towers != bigwalk_options.LinearTowers.option_one_deposit_box:
        return
    passthrough = getattr(world.multiworld, "re_gen_passthrough", None)
    slot_data = passthrough.get(world.game) if passthrough else None
    if slot_data and "monument_key_gourds" in slot_data:
        world.monument_key_gourds = dict(zip(slot_data["monument_key_keys"], slot_data["monument_key_gourds"]))
        return
    for tower in data.TOWERS:
        if tower_slots(world, tower) > 0:
            world.monument_key_gourds[tower.prop_name] = world.random.randint(1, tower_slots(world, tower))
