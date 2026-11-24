"""The chairlift and the tunnels are doors: no key may be placed past the door it opens."""

from .. import data, regions
from .bases import BigWalkTestBase


class TestGatedRegions(BigWalkTestBase):
    options: dict = {}
    run_default_tests = False

    def test_the_doors_exist_and_cost_their_key(self) -> None:
        # Guards the mechanism itself, whatever the membership is.
        for entrance_name, item_name in (
            (regions.CHAIRLIFT_ENTRANCE, data.GREEN_CUP.item_name),
            (regions.TUNNEL_ENTRANCE, data.YELLOW_TWIST.item_name),
            (regions.ENDING_ENTRANCE, data.BLACK_MONOLITH.item_name),
        ):
            entrance = self.multiworld.get_entrance(entrance_name, self.player)
            self.assertFalse(entrance.can_reach(self.multiworld.state), entrance_name)
            self.collect_by_name(item_name)
            self.assertTrue(entrance.can_reach(self.multiworld.state), entrance_name)

    def test_nothing_past_the_chairlift_is_reachable_without_the_key(self) -> None:
        for location_name in data.chairlift_locations():
            self.assertFalse(self.can_reach_location(location_name), location_name)

        self.collect_by_name(data.GREEN_CUP.item_name)
        self.collect_by_name(data.YELLOW_TWIST.item_name)

        for location_name in data.chairlift_locations():
            self.assertTrue(self.can_reach_location(location_name), location_name)

    def test_the_purple_puzzles_also_need_the_tunnels(self) -> None:
        purple = {puzzle.location_name for puzzle in data.PUZZLES if puzzle.prop_name in data.CHAIRLIFT_PUZZLES}
        self.collect_by_name(data.GREEN_CUP.item_name)

        for location_name in data.chairlift_locations():
            reachable = self.can_reach_location(location_name)
            self.assertEqual(reachable, location_name not in purple, location_name)

        self.collect_by_name(data.YELLOW_TWIST.item_name)

        for location_name in purple:
            self.assertTrue(self.can_reach_location(location_name), location_name)

    def test_nothing_past_the_tunnels_is_reachable_without_the_key(self) -> None:
        for location_name in data.tunnel_locations():
            self.assertFalse(self.can_reach_location(location_name), location_name)

        self.collect_by_name(data.YELLOW_TWIST.item_name)

        for location_name in data.tunnel_locations():
            self.assertTrue(self.can_reach_location(location_name), location_name)

    def test_a_gated_location_is_gated_once(self) -> None:
        # A name in both lists would be placed twice, the second call winning silently.
        overlap = set(data.chairlift_locations()) & set(data.tunnel_locations())
        self.assertFalse(overlap, overlap)

    def test_every_gated_name_is_a_real_location(self) -> None:
        # A typo would gate nothing, and look like a correct list when the option is off.
        from ..locations import LOCATION_NAME_TO_ID

        for location_name in data.chairlift_locations() + data.tunnel_locations():
            self.assertIn(location_name, LOCATION_NAME_TO_ID, location_name)
