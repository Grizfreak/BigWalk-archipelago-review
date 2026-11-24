from argparse import Namespace
from typing import Any

from BaseClasses import CollectionState, MultiWorld
from test.bases import WorldTestBase
from worlds.AutoWorld import call_all

from .. import data
from ..world import BigWalkWorld

UT_GEN_STEPS = ("generate_early", "create_regions", "create_items", "set_rules")
"""The steps Universal Tracker runs, in order; it never fills, the server places items."""


def build_like_universal_tracker(
    options: dict[str, Any],
    slot_data: dict[str, Any] | None = None,
) -> MultiWorld:
    """Builds a world the way Universal Tracker does; without slot_data, its first pass."""
    multiworld = MultiWorld(1)
    multiworld.game[1] = BigWalkWorld.game
    multiworld.player_name = {1: "Tracker"}
    multiworld.set_seed(0)

    # UT's flags: one says the world is built inside the tracker, one carries slot_data.
    multiworld.generation_is_fake = True
    if slot_data is not None:
        multiworld.re_gen_passthrough = {BigWalkWorld.game: slot_data}

    args = Namespace()
    for name, option in BigWalkWorld.options_dataclass.type_hints.items():
        setattr(args, name, {1: option.from_any(options.get(name, option.default))})
    multiworld.set_options(args)
    multiworld.state = CollectionState(multiworld)

    for step in UT_GEN_STEPS:
        call_all(multiworld, step)
    return multiworld


def world_shape(multiworld: MultiWorld) -> dict[str, Any]:
    """What a tracker can get wrong about a slot: locations, their regions, rule numbers."""
    world = multiworld.worlds[1]
    return {
        "regions": {
            region.name: sorted(location.name for location in region.locations)
            for region in multiworld.get_regions(1)
        },
        "gourd_count": world.gourd_count,
        "deposit_amounts": world.deposit_amounts,
        "deposit_goal": world.deposit_goal,
        "towers": [tower.prop_name for tower in world.towers],
        "locked_arch_doors": [door.item_name for door in world.locked_arch_doors],
        "progression": sorted(item.name for item in multiworld.itempool if item.player == 1 and item.advancement),
    }


class BigWalkTestBase(WorldTestBase):
    game = "Big Walk"
    world: BigWalkWorld

    def __init_subclass__(cls, **kwargs: Any) -> None:
        """Turns `require_arch_doors` and `lock_puzzle_needs` off unless the class sets them."""
        super().__init_subclass__(**kwargs)
        cls.options = {"require_arch_doors": False, "lock_puzzle_needs": False, **getattr(cls, "options", {})}

    _collected_gourds = 0

    def collect_gourds(self, count: int) -> None:
        """Collects `count` more gourds; collect_by_name would take the whole stack at once."""
        gourds = self.get_items_by_name(data.GOURD_ITEM_NAME)
        end = self._collected_gourds + count
        assert end <= len(gourds), f"only {len(gourds)} gourds in this pool, asked for {end}"
        self.collect(gourds[self._collected_gourds:end])
        self._collected_gourds = end
