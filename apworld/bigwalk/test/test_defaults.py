"""A seed with every option at its default: it generates, it can be won, and its main rules hold."""

from test.bases import WorldTestBase

from .. import data, regions
from .bases import NeedsChecks

LEFT = data.LEFT_ARCH_DOOR.item_name
RIGHT = data.RIGHT_ARCH_DOOR.item_name


class TestTheRealDefaults(NeedsChecks, WorldTestBase):
    # Not BigWalkTestBase: that one turns some defaults off for the other tests.
    game = "Big Walk"

    def test_the_puzzles_parts_are_items(self) -> None:
        self.assertTrue(self.world.options.lock_puzzle_needs)
        self.assertTrue(self.world.options.require_arch_doors)

    def test_a_puzzle_needs_each_of_its_parts(self) -> None:
        pool = {item.name for item in self.multiworld.itempool}
        checked = 0
        for puzzle in data.PUZZLES:
            for need in data.puzzle_needs(puzzle):
                # The part the player starts with is asked for by nothing.
                if need.item_name in pool:
                    self.assert_needs(puzzle.location_name, need.item_name)
                    checked += 1
        self.assertGreater(checked, 40)

    def test_the_towers_need_the_door_before_them(self) -> None:
        for prop, door in (("bigKeyBlueZone", LEFT), ("bigKeyGreenZone", RIGHT)):
            self.assert_needs(data.tower_reached_location_name(data.tower_by_prop(prop)), door)

    def test_the_first_arch_door_is_a_way_out_without_the_drawbridge(self) -> None:
        location = self.multiworld.get_location("Hub Fireworks", self.player)
        self.assertEqual(location.parent_region.name, regions.OVERWORLD)
        self.assertTrue(self.reached_without(location.name, data.DRAWBRIDGE_ITEM_NAME))

    def test_the_goal_needs_the_big_wall_door(self) -> None:
        self.assertTrue(self.multiworld.can_beat_game(self.state_without()))
        self.assertFalse(self.multiworld.can_beat_game(self.state_without("Big Wall Door")))
