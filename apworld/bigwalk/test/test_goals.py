from .. import data
from .bases import BigWalkTestBase


class TestBigGoodbyeGoal(BigWalkTestBase):
    """The default goal: everything past the Black Monolith Key."""

    options = {"goal": "big_goodbye"}

    def test_victory_needs_the_black_monolith_key(self) -> None:
        self.collect_all_but(data.BLACK_MONOLITH.item_name)
        self.assertFalse(self.can_reach_location("Victory"))

        self.collect_by_name(data.BLACK_MONOLITH.item_name)
        self.assertTrue(self.can_reach_location("Victory"))


class TestBigWallGoal(BigWalkTestBase):
    options = {"goal": "big_wall"}
    run_default_tests = False

    def test_victory_needs_the_black_monolith_key(self) -> None:
        self.collect_all_but(data.BLACK_MONOLITH.item_name)
        self.assertFalse(self.can_reach_location("Victory"))


class TestBigCollectionGoal(BigWalkTestBase):
    options = {"goal": "big_collection", "gourds_required": 20}
    run_default_tests = False

    def test_victory_needs_gourds_not_keys(self) -> None:
        # A deposit goal never opens the ending, so no big key may matter to it.
        for tower in data.TOWERS:
            self.collect_by_name(tower.item_name)
        self.assertFalse(self.can_reach_location("Victory"))

        self.collect_gourds(20)
        self.assertTrue(self.can_reach_location("Victory"))


class TestBigCollectionGoalAtEverySlot(BigWalkTestBase):
    """The largest deposit goal the option allows is every slot on the island."""

    options = {"goal": "big_collection", "gourds_required": data.MAX_MONUMENT_SLOTS}
    run_default_tests = False

    def test_every_gourd_is_needed_and_enough(self) -> None:
        self.collect_gourds(data.MAX_MONUMENT_SLOTS - 1)
        self.assertFalse(self.can_reach_location("Victory"))
        self.collect_gourds(1)
        self.assertTrue(self.can_reach_location("Victory"))


class TestBigGameGoal(BigWalkTestBase):
    """The ending behind the Spawn Secret Door, and only its item opens it."""

    options = {"goal": "big_game"}
    run_default_tests = False

    def test_victory_needs_the_green_dome(self) -> None:
        self.collect_all_but(data.GREEN_DOME.item_name)
        self.assertFalse(self.can_reach_location("Victory"))

        self.collect_by_name(data.GREEN_DOME.item_name)
        self.assertTrue(self.can_reach_location("Victory"))

    def test_victory_does_not_need_the_black_monolith_key(self) -> None:
        # In vanilla this zone is sealed until the game is finished once; the mod lifts that.
        self.collect_by_name(data.GREEN_DOME.item_name)
        self.assertTrue(self.can_reach_location("Victory"))
